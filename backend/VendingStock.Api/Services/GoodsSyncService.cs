using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using VendingStock.Api.Common;
using VendingStock.Api.Domain;
using VendingStock.Api.Infrastructure;
using VendingStock.Api.Integrations;
namespace VendingStock.Api.Services;
public sealed class GoodsSyncService(VendingStockDbContext db, IHttpClientFactory httpClientFactory, ILogger<GoodsSyncService> logger)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public async Task<GoodsSyncResult> SyncAsync(CancellationToken ct)
    {
        if (!await Gate.WaitAsync(0, ct)) throw new BusinessException("商品同步任务正在执行，请稍后重试", 4090);
        var log = new SyncTaskLog { TaskType = "GOODS_SYNC", Status = SyncTaskStatus.Running, StartedAt = DateTime.Now };
        db.SyncTaskLogs.Add(log); await db.SaveChangesAsync(ct);
        try
        {
            var client = httpClientFactory.CreateClient("UboxGoods");
            var response = await client.GetFromJsonAsync<UboxGoodsResponse>(UboxGoodsEndpoint.GoodsPath, ct) ?? throw new BusinessException("优宝商品接口未返回数据", 4005);
            if (response.Code != 1 || response.Data is null) throw new BusinessException($"优宝商品接口返回失败：{response.Message ?? "未知错误"}", 4005);
            var sourceItems = response.Data.Where(x => x.SkuId > 0 && !string.IsNullOrWhiteSpace(x.SkuName)).GroupBy(x => x.SkuId).Select(x => x.Last()).ToArray();
            log.TotalCount = sourceItems.Length;
            var skuIds = sourceItems.Select(x => x.SkuId).ToArray();
            var existing = await db.ProductRaws.Where(x => skuIds.Contains(x.SkuId)).ToDictionaryAsync(x => x.SkuId, ct);
            foreach (var source in sourceItems) { if (!existing.TryGetValue(source.SkuId, out var entity)) { entity = new ProductRaw { SkuId = source.SkuId }; db.ProductRaws.Add(entity); } Map(source, entity); log.SuccessCount++; }
            log.Status = SyncTaskStatus.Success; log.FinishedAt = DateTime.Now; await db.SaveChangesAsync(ct);
            return new GoodsSyncResult(log.Id, log.TotalCount, log.SuccessCount, log.FailedCount, log.FinishedAt.Value);
        }
        catch (Exception exception)
        {
            log.Status = SyncTaskStatus.Failed; log.FinishedAt = DateTime.Now; log.ErrorMessage = exception is BusinessException ? exception.Message : "商品同步执行失败"; log.FailedCount = Math.Max(1, log.TotalCount - log.SuccessCount); await db.SaveChangesAsync(CancellationToken.None); logger.LogError(exception, "Goods synchronization failed. LogId: {LogId}", log.Id); throw;
        }
        finally { Gate.Release(); }
    }
    private static void Map(UboxGoodsItem source, ProductRaw target) { target.SkuName = source.SkuName!.Trim(); target.BaseUnit = source.BaseUnit; target.BoxUnit = source.BoxUnit; target.BoxQuantity = source.BoxQuantity; target.SpecStr = source.SpecStr; target.ImageUrl = source.ImageUrl; target.BoxPrice = source.BoxPrice; target.UnitPrice = source.UnitPrice; target.Hot = source.Hot != 0; target.PackageNum = source.PackageNum; target.IsRecentlyPurchased = source.IsRecentlyPurchased != 0; target.Stock = source.Stock; target.StockR = source.StockR; target.StockP = source.StockP; target.StockA = source.StockA; target.StockB = source.StockB; target.StockW = source.StockW; target.BrandIdRaw = source.BrandIdRaw; target.BrandName = source.BrandName; target.CategoryCode1 = source.CategoryCode1; target.CategoryName1 = source.CategoryName1; target.CategoryCode2 = source.CategoryCode2; target.CategoryName2 = source.CategoryName2; target.Height = source.Height; target.Length = source.Length; target.Width = source.Width; }
}
public sealed record GoodsSyncResult(long LogId, int TotalCount, int SuccessCount, int FailedCount, DateTime FinishedAt);
