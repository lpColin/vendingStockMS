using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingStock.Api.Common;
using VendingStock.Api.Contracts;
using VendingStock.Api.Domain;
using VendingStock.Api.Infrastructure;
using VendingStock.Api.Services;
namespace VendingStock.Api.Controllers;
[ApiController][Route("api/v1/delivery-order")]
public sealed class DeliveryOrdersController(VendingStockDbContext db,DeliveryService service):ControllerBase
{
 [HttpGet("list")] public async Task<ApiResponse> List([FromQuery]PageQuery q,[FromQuery]DeliveryOrderStatus? status,[FromQuery]long? machineId,CancellationToken ct){var query=db.DeliveryOrders.AsNoTracking().Include(x=>x.Machine).AsQueryable();if(status.HasValue)query=query.Where(x=>x.Status==status);if(machineId.HasValue)query=query.Where(x=>x.MachineId==machineId);var total=await query.CountAsync(ct);var list=await query.OrderByDescending(x=>x.Id).Skip(q.Skip).Take(q.Take).Select(x=>new{x.Id,x.OrderNo,x.MachineId,MachineCode=x.Machine.MachineCode,x.Status,x.RestockerId,x.DeliveryPersonId,x.VehicleId,x.CompletedTime,x.DeliveryStartTime,x.DeliveryEndTime}).ToListAsync(ct);return ApiResponse.Ok(new PagedResult<object>(list.Cast<object>().ToArray(),total,q.Page,q.Take));}
 [HttpPost] public async Task<ApiResponse> Create(DeliveryOrderRequest r,CancellationToken ct){await Validate(r.MachineId,r.RestockerId,r.DeliveryPersonId,r.VehicleId,r.Items,ct);var order=new DeliveryOrder{OrderNo=await OrderNo(ct),MachineId=r.MachineId,RestockerId=r.RestockerId,DeliveryPersonId=r.DeliveryPersonId,VehicleId=r.VehicleId,Status=DeliveryOrderStatus.Pending,Items=r.Items.Select(x=>new DeliveryOrderItem{ProductId=x.ProductId,SourceWarehouseId=x.SourceWarehouseId,Quantity=x.Quantity}).ToArray()};db.DeliveryOrders.Add(order);await db.SaveChangesAsync(ct);return ApiResponse.Ok(new{id=order.Id,order.OrderNo});}
 [HttpPost("{id:long}/complete-restock")] public async Task<ApiResponse> CompleteRestock(long id,CancellationToken ct){await service.CompleteRestockAsync(id,ct);return ApiResponse.Ok(null,"配货完成");}
 [HttpPost("{id:long}/start-delivery")] public async Task<ApiResponse> Start(long id,CancellationToken ct){await service.StartDeliveryAsync(id,ct);return ApiResponse.Ok(null,"已开始送货");}
 [HttpPost("{id:long}/complete-delivery")] public async Task<ApiResponse> Complete(long id,CancellationToken ct){await service.CompleteDeliveryAsync(id,ct);return ApiResponse.Ok(null,"送货完成，机器库存已更新");}
 [HttpPost("{id:long}/cancel")] public async Task<ApiResponse> Cancel(long id,CancelDeliveryOrderRequest r,CancellationToken ct){if(string.IsNullOrWhiteSpace(r.Reason))throw new BusinessException("取消原因不能为空");await service.CancelAsync(id,r.Reason.Trim(),ct);return ApiResponse.Ok(null,"已取消");}
 private async Task Validate(long machine,long restocker,long delivery,long? vehicle,IReadOnlyCollection<DeliveryOrderItemRequest> items,CancellationToken ct){if(items.Count==0||items.Any(x=>x.Quantity<=0)||items.Select(x=>x.ProductId).Distinct().Count()!=items.Count)throw new BusinessException("配货明细不合法");if(!await db.VendingMachines.AnyAsync(x=>x.Id==machine,ct)||!await db.StaffMembers.AnyAsync(x=>x.Id==restocker&&x.Role==StaffRole.Restocker,ct)||!await db.StaffMembers.AnyAsync(x=>x.Id==delivery&&x.Role==StaffRole.DeliveryPerson,ct)||vehicle.HasValue&&!await db.Vehicles.AnyAsync(x=>x.Id==vehicle,ct)||await db.Products.CountAsync(x=>items.Select(i=>i.ProductId).Contains(x.Id),ct)!=items.Count||await db.Warehouses.CountAsync(x=>items.Select(i=>i.SourceWarehouseId).Contains(x.Id),ct)!=items.Select(x=>x.SourceWarehouseId).Distinct().Count())throw new BusinessException("关联机器、人员、车辆、商品或仓库不存在");}
 private async Task<string> OrderNo(CancellationToken ct){var p="DO"+DateTime.Now.ToString("yyyyMMdd");var last=await db.DeliveryOrders.Where(x=>x.OrderNo.StartsWith(p)).OrderByDescending(x=>x.OrderNo).Select(x=>x.OrderNo).FirstOrDefaultAsync(ct);return p+((last is null?1:int.Parse(last[^3..])+1)).ToString("D3");}
}
