namespace VendingStock.Api.Contracts;
public sealed record LookupRequest(string Name,int SortOrder=0);
public sealed record ProductRequest(string Name,long SpecId,long BrandId,long CategoryId,decimal Price,int? Weight,string? ImageUrl);
public sealed record WarehouseRequest(string Name,string Address,string Manager,string ManagerPhone);
public sealed record WarehouseStockRequest(long WarehouseId,long ProductId,int Quantity,string[]? Tags);
public sealed record WarehouseStockUpdateRequest(int Quantity,string[]? Tags);
public sealed record PurchaseOrderItemRequest(long ProductId,int Quantity);
public sealed record PurchaseOrderRequest(long WarehouseId,DateTime ImportTime,string DeliveryPerson,string Receiver,IReadOnlyCollection<PurchaseOrderItemRequest> Items);

public sealed record VendingMachineRequest(string MachineCode,string Address,DateTime OnlineTime,byte Status,string Manager,string ManagerPhone);
public sealed record MachineProductRequest(long ProductId,int StockQuantity,int AllocationQuantity,decimal SellPrice);
public sealed record MachineProductUpdateRequest(int StockQuantity,int AllocationQuantity,decimal SellPrice);
public sealed record StaffRequest(string Name,string Phone,string? Address,byte Role);
public sealed record VehicleRequest(string PlateNumber,string VehicleType);
public sealed record DeliveryOrderItemRequest(long ProductId,long SourceWarehouseId,int Quantity);
public sealed record DeliveryOrderRequest(long MachineId,long RestockerId,long DeliveryPersonId,long? VehicleId,IReadOnlyCollection<DeliveryOrderItemRequest> Items);
public sealed record DeliveryOrderUpdateRequest(long RestockerId,long DeliveryPersonId,long? VehicleId,IReadOnlyCollection<DeliveryOrderItemRequest> Items);
public sealed record CancelDeliveryOrderRequest(string Reason);

public sealed record DailySalesRequest(long MachineId, DateOnly SalesDate, decimal TotalAmount);
public sealed record SysConfigRequest(string ConfigKey, string ConfigValue, string? Description);
