using Microsoft.EntityFrameworkCore;
using VendingStock.Api.Common;
using VendingStock.Api.Domain;
using VendingStock.Api.Infrastructure;
namespace VendingStock.Api.Services;
public sealed class InventoryService(VendingStockDbContext db)
{
 public async Task AdjustWarehouseStockAsync(long warehouseId,long productId,int newQuantity,string? tags,string remark,CancellationToken ct)
 { if(newQuantity<0) throw new BusinessException("库存数量不能小于 0"); var stock=await db.WarehouseStocks.SingleOrDefaultAsync(x=>x.WarehouseId==warehouseId&&x.ProductId==productId,ct); if(stock is null) { stock=new WarehouseStock{WarehouseId=warehouseId,ProductId=productId,Quantity=0,Tags=tags};db.WarehouseStocks.Add(stock); } var before=stock.Quantity; stock.Quantity=newQuantity; stock.Tags=tags; db.InventoryTransactions.Add(new InventoryTransaction{TransactionType=InventoryTransactionType.ManualAdjustment,WarehouseId=warehouseId,ProductId=productId,QuantityChange=newQuantity-before,QuantityBefore=before,QuantityAfter=newQuantity,Remark=remark}); await db.SaveChangesAsync(ct); }
 public async Task ImportPurchaseOrderAsync(long orderId,CancellationToken ct)
 { await using var tx=await db.Database.BeginTransactionAsync(ct); var order=await db.PurchaseOrders.Include(x=>x.Items).SingleOrDefaultAsync(x=>x.Id==orderId,ct) ?? throw new BusinessException("进货单不存在",4040); if(order.Status==PurchaseOrderStatus.Imported) throw new BusinessException("该进货单已入库，不可重复操作",4003); foreach(var item in order.Items){var stock=await db.WarehouseStocks.SingleOrDefaultAsync(x=>x.WarehouseId==order.WarehouseId&&x.ProductId==item.ProductId,ct);if(stock is null){stock=new WarehouseStock{WarehouseId=order.WarehouseId,ProductId=item.ProductId,Quantity=0};db.WarehouseStocks.Add(stock);}var before=stock.Quantity;stock.Quantity+=item.Quantity;db.InventoryTransactions.Add(new InventoryTransaction{TransactionType=InventoryTransactionType.PurchaseImport,WarehouseId=order.WarehouseId,ProductId=item.ProductId,QuantityChange=item.Quantity,QuantityBefore=before,QuantityAfter=stock.Quantity,RelatedOrderNo=order.OrderNo,Remark="进货单一键入库"});} order.Status=PurchaseOrderStatus.Imported; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
}
