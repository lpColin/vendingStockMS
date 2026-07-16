using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingStock.Api.Common;
using VendingStock.Api.Contracts;
using VendingStock.Api.Domain;
using VendingStock.Api.Infrastructure;
namespace VendingStock.Api.Controllers;
[ApiController][Route("api/v1/staff")]
public sealed class StaffController(VendingStockDbContext db) : ControllerBase
{
 [HttpGet("list")] public async Task<ApiResponse> List([FromQuery]PageQuery q,[FromQuery]StaffRole? role,CancellationToken ct){var query=db.StaffMembers.AsNoTracking().AsQueryable();if(role.HasValue)query=query.Where(x=>x.Role==role);var total=await query.CountAsync(ct);var items=await query.OrderByDescending(x=>x.Id).Skip(q.Skip).Take(q.Take).Select(x=>new{x.Id,x.Name,x.Phone,x.Address,x.Role}).ToListAsync(ct);return ApiResponse.Ok(new PagedResult<object>(items.Cast<object>().ToArray(),total,q.Page,q.Take));}
 [HttpPost] public async Task<ApiResponse> Create(StaffRequest r,CancellationToken ct){Validate(r);var entity=new Staff{Name=r.Name.Trim(),Phone=r.Phone.Trim(),Address=r.Address?.Trim(),Role=(StaffRole)r.Role};db.StaffMembers.Add(entity);await db.SaveChangesAsync(ct);return ApiResponse.Ok(new{id=entity.Id});}
 [HttpPut("{id:long}")] public async Task<ApiResponse> Update(long id,StaffRequest r,CancellationToken ct){Validate(r);var entity=await db.StaffMembers.FindAsync([id],ct)??throw new BusinessException("人员不存在",4040);entity.Name=r.Name.Trim();entity.Phone=r.Phone.Trim();entity.Address=r.Address?.Trim();entity.Role=(StaffRole)r.Role;await db.SaveChangesAsync(ct);return ApiResponse.Ok();}
 [HttpDelete("{id:long}")] public async Task<ApiResponse> Delete(long id,CancellationToken ct){var entity=await db.StaffMembers.FindAsync([id],ct)??throw new BusinessException("人员不存在",4040);db.StaffMembers.Remove(entity);await db.SaveChangesAsync(ct);return ApiResponse.Ok();}
 private static void Validate(StaffRequest r){if(string.IsNullOrWhiteSpace(r.Name)||string.IsNullOrWhiteSpace(r.Phone)||(r.Role!=1&&r.Role!=2))throw new BusinessException("人员参数不合法");}
}
[ApiController][Route("api/v1/vehicle")]
public sealed class VehiclesController(VendingStockDbContext db):ControllerBase
{
 [HttpGet("list")] public async Task<ApiResponse> List([FromQuery]PageQuery q,CancellationToken ct){var total=await db.Vehicles.CountAsync(ct);var items=await db.Vehicles.AsNoTracking().OrderByDescending(x=>x.Id).Skip(q.Skip).Take(q.Take).Select(x=>new{x.Id,x.PlateNumber,x.VehicleType}).ToListAsync(ct);return ApiResponse.Ok(new PagedResult<object>(items.Cast<object>().ToArray(),total,q.Page,q.Take));}
 [HttpPost] public async Task<ApiResponse> Create(VehicleRequest r,CancellationToken ct){Validate(r);if(await db.Vehicles.AnyAsync(x=>x.PlateNumber==r.PlateNumber.Trim(),ct))throw new BusinessException("车牌已存在");var entity=new Vehicle{PlateNumber=r.PlateNumber.Trim(),VehicleType=r.VehicleType.Trim()};db.Vehicles.Add(entity);await db.SaveChangesAsync(ct);return ApiResponse.Ok(new{id=entity.Id});}
 [HttpPut("{id:long}")] public async Task<ApiResponse> Update(long id,VehicleRequest r,CancellationToken ct){Validate(r);var entity=await db.Vehicles.FindAsync([id],ct)??throw new BusinessException("车辆不存在",4040);if(await db.Vehicles.AnyAsync(x=>x.Id!=id&&x.PlateNumber==r.PlateNumber.Trim(),ct))throw new BusinessException("车牌已存在");entity.PlateNumber=r.PlateNumber.Trim();entity.VehicleType=r.VehicleType.Trim();await db.SaveChangesAsync(ct);return ApiResponse.Ok();}
 [HttpDelete("{id:long}")] public async Task<ApiResponse> Delete(long id,CancellationToken ct){var entity=await db.Vehicles.FindAsync([id],ct)??throw new BusinessException("车辆不存在",4040);db.Vehicles.Remove(entity);await db.SaveChangesAsync(ct);return ApiResponse.Ok();}
 private static void Validate(VehicleRequest r){if(string.IsNullOrWhiteSpace(r.PlateNumber)||string.IsNullOrWhiteSpace(r.VehicleType))throw new BusinessException("车辆参数不合法");}
}
