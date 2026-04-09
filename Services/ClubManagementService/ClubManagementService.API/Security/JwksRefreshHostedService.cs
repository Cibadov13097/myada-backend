namespace ClubManagementService.API.Security
{
    public class JwksRefreshHostedService : BackgroundService
    {
        private readonly ILogger<JwksRefreshHostedService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        public JwksRefreshHostedService(ILogger<JwksRefreshHostedService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_interval, stoppingToken);

                try
                {
                    await JwksProvider.RefreshKeysAsync();
                    _logger.LogInformation("JWKS keys refreshed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh JWKS keys, will retry");
                }
            }
        }
    }
}
