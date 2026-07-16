using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingStock.Api.Common;
using VendingStock.Api.Infrastructure;
using VendingStock.Api.Services;
namespace VendingStock.Api.Controllers;
[ApiController][Route("api/v1/sync-task")]
public sealed class SyncTasksController(VendingStockDbContext db, GoodsSyncService goodsSyncService) : ControllerBase
{
    [HttpPost("goods/trigger")]
    public async Task<ApiResponse> TriggerGoods(CancellationToken ct)
    {
        var result = await goodsSyncService.SyncAsync(ct);
        return ApiResponse.Ok(result, "商品原始数据同步完成");
    }
    [HttpGet("log/list")]
    public async Task<ApiResponse> Logs([FromQuery] PageQuery page, [FromQuery] string? taskType, CancellationToken ct)
    {
        var query = db.SyncTaskLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(taskType)) query = query.Where(x => x.TaskType == taskType);
        var total = await query.CountAsync(ct);
        var list = await query.OrderByDescending(x => x.Id).Skip(page.Skip).Take(page.Take).Select(x => new { x.Id, x.TaskType, x.Status, x.StartedAt, x.FinishedAt, x.TotalCount, x.SuccessCount, x.FailedCount, x.ErrorMessage }).ToListAsync(ct);
        return ApiResponse.Ok(new PagedResult<object>(list.Cast<object>().ToArray(), total, page.Page, page.Take));
    }
}
[ApiController][Route("api/v1/product-raw")]
public sealed class ProductRawsController(VendingStockDbContext db) : ControllerBase
{
    [HttpGet("list")]
    public async Task<ApiResponse> List([FromQuery] PageQuery page, [FromQuery] string? keyword, [FromQuery] int? brandId, [FromQuery] int? categoryCode, CancellationToken ct)
    {
        var query = db.ProductRaws.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.SkuName.Contains(keyword));
        if (brandId.HasValue) query = query.Where(x => x.BrandIdRaw == brandId);
        if (categoryCode.HasValue) query = query.Where(x => x.CategoryCode1 == categoryCode || x.CategoryCode2 == categoryCode);
        var total = await query.CountAsync(ct);
        var list = await query.OrderBy(x => x.SkuName).Skip(page.Skip).Take(page.Take).Select(x => new { x.Id, x.SkuId, x.SkuName, x.SpecStr, x.ImageUrl, x.BoxPrice, x.UnitPrice, x.Stock, x.BrandIdRaw, x.BrandName, x.CategoryCode1, x.CategoryName1, x.CategoryCode2, x.CategoryName2, x.UpdatedAt }).ToListAsync(ct);
        return ApiResponse.Ok(new PagedResult<object>(list.Cast<object>().ToArray(), total, page.Page, page.Take));
    }
}
