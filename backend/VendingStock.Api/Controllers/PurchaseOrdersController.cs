using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingStock.Api.Common;
using VendingStock.Api.Contracts;
using VendingStock.Api.Domain;
using VendingStock.Api.Infrastructure;
using VendingStock.Api.Services;
namespace VendingStock.Api.Controllers;
[ApiController]
[Route("api/v1/purchase-order")]
public sealed class PurchaseOrdersController(VendingStockDbContext db, InventoryService inventory) : ControllerBase
{
    [HttpGet("list")]
    public async Task<ApiResponse> List([FromQuery] PageQuery q, [FromQuery] long? warehouseId, [FromQuery] PurchaseOrderStatus? status, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct)
    {
        var query = db.PurchaseOrders.AsNoTracking().Include(x => x.Warehouse).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(x => x.WarehouseId == warehouseId);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (startDate.HasValue) query = query.Where(x => x.ImportTime >= startDate);
        if (endDate.HasValue) query = query.Where(x => x.ImportTime < endDate.Value.Date.AddDays(1));
        var total = await query.CountAsync(ct);
        var list = await query.OrderByDescending(x => x.Id).Skip(q.Skip).Take(q.Take).Select(x => new { x.Id, x.OrderNo, x.WarehouseId, WarehouseName = x.Warehouse.Name, x.ImportTime, x.DeliveryPerson, x.Receiver, x.TotalQuantity, x.Status, StatusName = x.Status == PurchaseOrderStatus.Imported ? "已入库" : "待入库" }).ToListAsync(ct);
        return ApiResponse.Ok(new PagedResult<object>(list.Cast<object>().ToArray(), total, q.Page, q.Take));
    }
    [HttpGet("{id:long}")]
    public async Task<ApiResponse> Get(long id, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.AsNoTracking().Include(x => x.Warehouse).Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x.Spec).Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x.Brand).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException("进货单不存在", 4040);
        return ApiResponse.Ok(new { order.Id, order.OrderNo, order.WarehouseId, WarehouseName = order.Warehouse.Name, order.ImportTime, order.DeliveryPerson, order.Receiver, order.TotalQuantity, order.Status, Items = order.Items.Select(x => new { x.Id, x.ProductId, ProductName = x.Product.Name, SpecName = x.Product.Spec.Name, BrandName = x.Product.Brand.Name, x.Quantity }) });
    }
    [HttpPost]
    public async Task<ApiResponse> Create(PurchaseOrderRequest request, CancellationToken ct)
    {
        if (!await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, ct)) throw new BusinessException("仓库不存在");
        if (string.IsNullOrWhiteSpace(request.DeliveryPerson) || string.IsNullOrWhiteSpace(request.Receiver) || request.Items.Count == 0 || request.Items.Any(x => x.Quantity <= 0)) throw new BusinessException("进货单参数不合法");
        var productIds = request.Items.Select(x => x.ProductId).ToArray();
        if (productIds.Distinct().Count() != productIds.Length || await db.Products.CountAsync(x => productIds.Contains(x.Id), ct) != productIds.Length) throw new BusinessException("进货商品不存在或重复");
        var order = new PurchaseOrder { OrderNo = await NewOrderNo(ct), WarehouseId = request.WarehouseId, ImportTime = request.ImportTime, DeliveryPerson = request.DeliveryPerson.Trim(), Receiver = request.Receiver.Trim(), TotalQuantity = request.Items.Sum(x => x.Quantity), Status = PurchaseOrderStatus.Pending, Items = request.Items.Select(x => new PurchaseOrderItem { ProductId = x.ProductId, Quantity = x.Quantity }).ToArray() };
        db.PurchaseOrders.Add(order); await db.SaveChangesAsync(ct); return ApiResponse.Ok(new { id = order.Id, order.OrderNo });
    }
    [HttpPost("{id:long}/import")]
    public async Task<ApiResponse> Import(long id, CancellationToken ct) { await inventory.ImportPurchaseOrderAsync(id, ct); return ApiResponse.Ok(null, "入库成功"); }
    [HttpDelete("{id:long}")]
    public async Task<ApiResponse> Delete(long id, CancellationToken ct) { var order = await db.PurchaseOrders.FindAsync([id], ct) ?? throw new BusinessException("进货单不存在", 4040); if (order.Status == PurchaseOrderStatus.Imported) throw new BusinessException("已入库的进货单不可删除"); db.PurchaseOrders.Remove(order); await db.SaveChangesAsync(ct); return ApiResponse.Ok(); }
    private async Task<string> NewOrderNo(CancellationToken ct) { var prefix = "PO" + DateTime.Now.ToString("yyyyMMdd"); var last = await db.PurchaseOrders.Where(x => x.OrderNo.StartsWith(prefix)).OrderByDescending(x => x.OrderNo).Select(x => x.OrderNo).FirstOrDefaultAsync(ct); var sequence = last is null ? 1 : int.Parse(last.Substring(last.Length - 3)) + 1; return prefix + sequence.ToString("D3"); }
}
