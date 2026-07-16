using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VendingStock.Api.Integrations;
using VendingStock.Api.Common;
using VendingStock.Api.Infrastructure;
using VendingStock.Api.Services;

Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("logs/vending-stock-.log", rollingInterval: RollingInterval.Day).CreateBootstrapLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console()
        .WriteTo.File("logs/vending-stock-.log", rollingInterval: RollingInterval.Day));

    var connectionString = builder.Configuration.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("ConnectionStrings:MySql is required.");
    builder.Services.AddDbContext<VendingStockDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)).UseSnakeCaseNamingConvention());
    builder.Services.AddScoped<InventoryService>();
    builder.Services.AddScoped<DeliveryService>();
    builder.Services.AddHttpClient("UboxGoods", client => { client.BaseAddress = new Uri(UboxGoodsEndpoint.BaseUrl); client.Timeout = TimeSpan.FromSeconds(30); });
    builder.Services.AddScoped<GoodsSyncService>();
    builder.Services.AddHostedService<DailyGoodsSyncHostedService>();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddProblemDetails();
    builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var status = exception is BusinessException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(exception?.Message ?? "服务器内部错误", exception is BusinessException business ? business.Code : 5000));
    }));
    app.UseSerilogRequestLogging();
    app.UseCors("Frontend");
    if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
    app.MapGet("/health", () => ApiResponse.Ok(new { status = "healthy" }));
    app.MapControllers();
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally { Log.CloseAndFlush(); }
