using System.Net.Http.Json;

namespace AuthService.API.Integrations;

public sealed class SupportStaffProvisioningClient
{
    private static readonly HashSet<string> StaffProvisionRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "support",
        "dispatcher",
        "staff",
        "student_services"
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupportStaffProvisioningClient> _logger;

    public SupportStaffProvisioningClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SupportStaffProvisioningClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SupportProvisionResult> ProvisionForRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        if (!RequiresProvisioning(roleName))
            return SupportProvisionResult.Skipped();

        if (!Guid.TryParse(userId, out var memberId))
        {
            return SupportProvisionResult.Failed(
                $"User id '{userId}' is not a valid GUID for support staff provisioning.");
        }

        var provisioningKey = _configuration["SupportService:ProvisioningKey"];
        if (string.IsNullOrWhiteSpace(provisioningKey))
        {
            return SupportProvisionResult.Failed(
                "SupportService:ProvisioningKey is not configured.");
        }

        var endpoint = _configuration["SupportService:ProvisioningPath"];
        if (string.IsNullOrWhiteSpace(endpoint))
            endpoint = "/api/SupportStaffStatuses/internal/provision";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("X-Internal-Key", provisioningKey);
        request.Content = JsonContent.Create(new
        {
            memberId,
            status = "Offline"
        });

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return SupportProvisionResult.Success();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Support staff provisioning failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode,
                responseBody);

            return SupportProvisionResult.Failed(
                $"Support staff provisioning failed with status {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Support staff provisioning request failed.");
            return SupportProvisionResult.Failed("Support staff provisioning request failed.");
        }
    }

    private static bool RequiresProvisioning(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return false;

        return StaffProvisionRoles.Contains(roleName)
               || roleName.EndsWith("_staff", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class SupportProvisionResult
    {
        public bool IsSuccess { get; init; }
        public bool IsSkipped { get; init; }
        public string? ErrorMessage { get; init; }

        public static SupportProvisionResult Success() => new() { IsSuccess = true };
        public static SupportProvisionResult Skipped() => new() { IsSuccess = true, IsSkipped = true };
        public static SupportProvisionResult Failed(string errorMessage) => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
