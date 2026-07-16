using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VendingStock.Api.Infrastructure;

public sealed class VendingStockDbContextFactory : IDesignTimeDbContextFactory<VendingStockDbContext>
{
    public VendingStockDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("VENDING_STOCK_MYSQL")
            ?? "Server=127.0.0.1;Port=3306;Database=vending_stock;User=vending_app;Password=design-time-only;";
        var options = new DbContextOptionsBuilder<VendingStockDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4)))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new VendingStockDbContext(options);
    }
}
