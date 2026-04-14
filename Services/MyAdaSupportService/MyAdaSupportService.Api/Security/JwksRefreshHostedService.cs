namespace MyAdaSupportService.Api.Security;

public sealed class JwksRefreshHostedService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await JwksProvider.RefreshAsync();
            await Task.Delay(RefreshInterval, stoppingToken);
        }
    }
}
