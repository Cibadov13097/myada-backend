namespace LocationService.Api.Security;

/// <summary>
/// Periodically refreshes JWKS signing keys from AuthService to support key rotation without restart.
/// </summary>
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
