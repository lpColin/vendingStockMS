using VendingStock.Api.Integrations;
namespace VendingStock.Api.Services;
public sealed class DailyGoodsSyncHostedService(IServiceScopeFactory scopeFactory, ILogger<DailyGoodsSyncHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun(DateTime.Now); await Task.Delay(delay, stoppingToken);
            try { using var scope = scopeFactory.CreateScope(); await scope.ServiceProvider.GetRequiredService<GoodsSyncService>().SyncAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Daily goods sync failed."); }
        }
    }
    internal static TimeSpan DelayUntilNextRun(DateTime now) { var next = now.Date.AddHours(4); if (now >= next) next = next.AddDays(1); return next - now; }
}
