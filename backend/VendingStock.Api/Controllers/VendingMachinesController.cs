using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VendingStock.Api.Common;
using VendingStock.Api.Contracts;
using VendingStock.Api.Domain;
using VendingStock.Api.Infrastructure;
namespace VendingStock.Api.Controllers;
[ApiController][Route("api/v1/vending-machine")]
public sealed class VendingMachinesController(VendingStockDbContext db):ControllerBase
{
 [HttpGet("list")] public async Task<ApiResponse> List([FromQuery]PageQuery q,[FromQuery]VendingMachineStatus? status,[FromQuery]string? keyword,CancellationToken ct){var query=db.VendingMachines.AsNoTracking().AsQueryable();if(status.HasValue)query=query.Where(x=>x.Status==status);if(!string.IsNullOrWhiteSpace(keyword))query=query.Where(x=>x.MachineCode.Contains(keyword)||x.Address.Contains(keyword));var total=await query.CountAsync(ct);var list=await query.OrderByDescending(x=>x.Id).Skip(q.Skip).Take(q.Take).Select(x=>new{x.Id,x.MachineCode,x.Address,x.OnlineTime,x.Status,x.Manager,x.ManagerPhone}).ToListAsync(ct);return ApiResponse.Ok(new PagedResult<object>(list.Cast<object>().ToArray(),total,q.Page,q.Take));}
 [HttpGet("{id:long}")] public async Task<ApiResponse> Get(long id,CancellationToken ct){var machine=await db.VendingMachines.AsNoTracking().Include(x=>x.Products).ThenInclude(x=>x.Product).ThenInclude(x=>x.Spec).SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new BusinessException("贩卖机不存在",4040);var total=machine.Products.Sum(x=>x.AllocationQuantity);return ApiResponse.Ok(new{machine.Id,machine.MachineCode,machine.Address,machine.OnlineTime,machine.Status,machine.Manager,machine.ManagerPhone,StockRate=total==0?0:Math.Round(machine.Products.Sum(x=>x.StockQuantity)*100m/total,2),Products=machine.Products.Select(x=>new{x.Id,x.ProductId,ProductName=x.Product.Name,SpecName=x.Product.Spec.Name,x.StockQuantity,x.AllocationQuantity,x.SellPrice})});}
 [HttpPost("import-external")] public async Task<ApiResponse> ImportExternal(ExternalMachineImportRequest request,CancellationToken ct)
 {
  if(string.IsNullOrWhiteSpace(request.Payload)) throw new BusinessException("请粘贴机器数据 JSON");
  JsonDocument document;
  try { document=JsonDocument.Parse(request.Payload); } catch(JsonException) { throw new BusinessException("机器数据不是有效的 JSON 格式"); }
  using(document)
  {
   var root=document.RootElement;
   var rows=root.ValueKind==JsonValueKind.Array ? root : root.TryGetProperty("body",out var body) && body.ValueKind==JsonValueKind.Array ? body : throw new BusinessException("未找到 body 数组，请粘贴接口完整响应或机器数组");
   var sources=rows.EnumerateArray().Select(item => new { MachineCode=ReadString(item,"vmCode"), Address=ReadString(item,"nodeName") }).Where(x=>!string.IsNullOrWhiteSpace(x.MachineCode)&&!string.IsNullOrWhiteSpace(x.Address)).GroupBy(x=>x.MachineCode!,StringComparer.Ordinal).Select(x=>x.Last()).ToArray();
   if(sources.Length==0) throw new BusinessException("未读取到有效机器数据（需要 vmCode 和 nodeName）");
   // Avoid EF Core 9 translating an array Contains expression on .NET 10.
   var existing=await db.VendingMachines.ToDictionaryAsync(x=>x.MachineCode,ct);
   var created=0; var updated=0;
   foreach(var source in sources)
   {
    if(existing.TryGetValue(source.MachineCode!,out var machine)) { if(machine.Address!=source.Address) { machine.Address=source.Address!; updated++; } continue; }
    db.VendingMachines.Add(new VendingMachine { MachineCode=source.MachineCode!, Address=source.Address!, OnlineTime=DateTime.Now, Status=VendingMachineStatus.Normal, Manager="待完善", ManagerPhone="待完善" }); created++;
   }
   await db.SaveChangesAsync(ct);
   return ApiResponse.Ok(new { TotalCount=sources.Length, CreatedCount=created, UpdatedCount=updated },"机器数据导入完成");
  }
 }
 private static string? ReadString(JsonElement item,string propertyName)=>item.TryGetProperty(propertyName,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString()?.Trim():null;
 [HttpPost] public async Task<ApiResponse> Create(VendingMachineRequest r,CancellationToken ct){Validate(r);if(await db.VendingMachines.AnyAsync(x=>x.MachineCode==r.MachineCode.Trim(),ct))throw new BusinessException("机器编码已存在");var x=new VendingMachine{MachineCode=r.MachineCode.Trim(),Address=r.Address.Trim(),OnlineTime=r.OnlineTime,Status=(VendingMachineStatus)r.Status,Manager=r.Manager.Trim(),ManagerPhone=r.ManagerPhone.Trim()};db.VendingMachines.Add(x);await db.SaveChangesAsync(ct);return ApiResponse.Ok(new{id=x.Id});}
 [HttpPut("{id:long}")] public async Task<ApiResponse> Update(long id,VendingMachineRequest r,CancellationToken ct){Validate(r);var x=await db.VendingMachines.FindAsync([id],ct)??throw new BusinessException("贩卖机不存在",4040);if(await db.VendingMachines.AnyAsync(m=>m.Id!=id&&m.MachineCode==r.MachineCode.Trim(),ct))throw new BusinessException("机器编码已存在");x.MachineCode=r.MachineCode.Trim();x.Address=r.Address.Trim();x.OnlineTime=r.OnlineTime;x.Status=(VendingMachineStatus)r.Status;x.Manager=r.Manager.Trim();x.ManagerPhone=r.ManagerPhone.Trim();await db.SaveChangesAsync(ct);return ApiResponse.Ok();}
 [HttpDelete("{id:long}")] public async Task<ApiResponse> Delete(long id,CancellationToken ct){var x=await db.VendingMachines.FindAsync([id],ct)??throw new BusinessException("贩卖机不存在",4040);db.VendingMachines.Remove(x);await db.SaveChangesAsync(ct);return ApiResponse.Ok();}
 private static void Validate(VendingMachineRequest r){if(string.IsNullOrWhiteSpace(r.MachineCode)||string.IsNullOrWhiteSpace(r.Address)||string.IsNullOrWhiteSpace(r.Manager)||string.IsNullOrWhiteSpace(r.ManagerPhone)||r.Status>2)throw new BusinessException("贩卖机参数不合法");}
}
