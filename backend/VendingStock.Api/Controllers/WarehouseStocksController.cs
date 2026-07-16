using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingStock.Api.Common;
using VendingStock.Api.Contracts;
using VendingStock.Api.Infrastructure;
using VendingStock.Api.Services;
namespace VendingStock.Api.Controllers;
[ApiController][Route("api/v1/warehouse-stock")]
public sealed class WarehouseStocksController(VendingStockDbContext db,InventoryService inventory):ControllerBase
{
 [HttpGet("list")] public async Task<ApiResponse> List([FromQuery]PageQuery q,[FromQuery]long? warehouseId,[FromQuery]long? categoryId,[FromQuery]long? brandId,[FromQuery]string? tag,CancellationToken ct){var query=db.WarehouseStocks.AsNoTracking().Include(x=>x.Warehouse).Include(x=>x.Product).ThenInclude(x=>x.Spec).Include(x=>x.Product).ThenInclude(x=>x.Brand).Include(x=>x.Product).ThenInclude(x=>x.Category).AsQueryable();if(warehouseId.HasValue)query=query.Where(x=>x.WarehouseId==warehouseId);if(categoryId.HasValue)query=query.Where(x=>x.Product.CategoryId==categoryId);if(brandId.HasValue)query=query.Where(x=>x.Product.BrandId==brandId);if(!string.IsNullOrWhiteSpace(tag))query=query.Where(x=>x.Tags!=null&&x.Tags.Contains(tag));var total=await query.CountAsync(ct);var list=await query.OrderByDescending(x=>x.Id).Skip(q.Skip).Take(q.Take).ToListAsync(ct);return ApiResponse.Ok(new PagedResult<object>(list.Select(x=>(object)new{x.Id,x.WarehouseId,WarehouseName=x.Warehouse.Name,x.ProductId,ProductName=x.Product.Name,SpecName=x.Product.Spec.Name,BrandName=x.Product.Brand.Name,x.Quantity,Tags=ToTags(x.Tags)}).ToArray(),total,q.Page,q.Take));}
 [HttpPost] public async Task<ApiResponse> Create(WarehouseStockRequest r,CancellationToken ct){await VerifyReference(r.WarehouseId,r.ProductId,ct);if(await db.WarehouseStocks.AnyAsync(x=>x.WarehouseId==r.WarehouseId&&x.ProductId==r.ProductId,ct))throw new BusinessException("同一仓库中该商品库存已存在");await inventory.AdjustWarehouseStockAsync(r.WarehouseId,r.ProductId,r.Quantity,JsonSerializer.Serialize(r.Tags??[]),"手动新增库存",ct);return ApiResponse.Ok();}
 [HttpPut("{id:long}")] public async Task<ApiResponse> Update(long id,WarehouseStockUpdateRequest r,CancellationToken ct){var stock=await db.WarehouseStocks.FindAsync([id],ct)??throw new BusinessException("库存记录不存在",4040);await inventory.AdjustWarehouseStockAsync(stock.WarehouseId,stock.ProductId,r.Quantity,JsonSerializer.Serialize(r.Tags??[]),"手动调整库存",ct);return ApiResponse.Ok();}
 [HttpDelete("{id:long}")] public async Task<ApiResponse> Delete(long id,CancellationToken ct){var stock=await db.WarehouseStocks.FindAsync([id],ct)??throw new BusinessException("库存记录不存在",4040);db.WarehouseStocks.Remove(stock);await db.SaveChangesAsync(ct);return ApiResponse.Ok();}
 private async Task VerifyReference(long warehouseId,long productId,CancellationToken ct){if(!await db.Warehouses.AnyAsync(x=>x.Id==warehouseId,ct)||!await db.Products.AnyAsync(x=>x.Id==productId,ct))throw new BusinessException("仓库或商品不存在");} private static string[] ToTags(string? value){try{return string.IsNullOrWhiteSpace(value)?[]:JsonSerializer.Deserialize<string[]>(value)??[];}catch{return [];}}
}
