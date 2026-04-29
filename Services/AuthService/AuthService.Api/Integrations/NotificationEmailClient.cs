using System.Net.Http.Json;

namespace AuthService.API.Integrations;

public sealed class NotificationEmailClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationEmailClient> _logger;

    public NotificationEmailClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NotificationEmailClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string message,
        Guid? recipientUserId = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = _configuration["NotificationService:SendEmailPath"];
        if (string.IsNullOrWhiteSpace(endpoint))
            endpoint = "/api/v1/notifications/email";

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                endpoint,
                new
                {
                    type = "auth.password_reset",
                    recipientUserId,
                    toEmail,
                    subject,
                    message
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return true;

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "NotificationService email call failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode,
                responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotificationService email call failed with exception.");
        }

        return false;
    }
}
