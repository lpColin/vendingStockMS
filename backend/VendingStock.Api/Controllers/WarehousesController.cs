using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingStock.Api.Common;
using VendingStock.Api.Contracts;
using VendingStock.Api.Domain;
using VendingStock.Api.Infrastructure;
namespace VendingStock.Api.Controllers;
[ApiController][Route("api/v1/warehouse")]
public sealed class WarehousesController(VendingStockDbContext db):ControllerBase
{
 [HttpGet("list")] public async Task<ApiResponse> List([FromQuery]PageQuery q,[FromQuery]string? keyword,CancellationToken ct){var query=db.Warehouses.AsNoTracking().AsQueryable();if(!string.IsNullOrWhiteSpace(keyword))query=query.Where(x=>x.Name.Contains(keyword)||x.Address.Contains(keyword));var total=await query.CountAsync(ct);var list=await query.OrderByDescending(x=>x.Id).Skip(q.Skip).Take(q.Take).Select(x=>new{x.Id,x.Name,x.Address,x.Manager,x.ManagerPhone}).ToListAsync(ct);return ApiResponse.Ok(new PagedResult<object>(list.Cast<object>().ToArray(),total,q.Page,q.Take));}
 [HttpPost] public async Task<ApiResponse> Create(WarehouseRequest r,CancellationToken ct){Validate(r);var x=new Warehouse{Name=r.Name.Trim(),Address=r.Address.Trim(),Manager=r.Manager.Trim(),ManagerPhone=r.ManagerPhone.Trim()};db.Warehouses.Add(x);await db.SaveChangesAsync(ct);return ApiResponse.Ok(new{id=x.Id});}
 [HttpPut("{id:long}")] public async Task<ApiResponse> Update(long id,WarehouseRequest r,CancellationToken ct){Validate(r);var x=await db.Warehouses.FindAsync([id],ct)??throw new BusinessException("仓库不存在",4040);x.Name=r.Name.Trim();x.Address=r.Address.Trim();x.Manager=r.Manager.Trim();x.ManagerPhone=r.ManagerPhone.Trim();await db.SaveChangesAsync(ct);return ApiResponse.Ok();}
 [HttpDelete("{id:long}")] public async Task<ApiResponse> Delete(long id,CancellationToken ct){var x=await db.Warehouses.FindAsync([id],ct)??throw new BusinessException("仓库不存在",4040);db.Warehouses.Remove(x);await db.SaveChangesAsync(ct);return ApiResponse.Ok();}
 private static void Validate(WarehouseRequest r){if(string.IsNullOrWhiteSpace(r.Name)||string.IsNullOrWhiteSpace(r.Address)||string.IsNullOrWhiteSpace(r.Manager)||string.IsNullOrWhiteSpace(r.ManagerPhone))throw new BusinessException("仓库信息不能为空");}
}
