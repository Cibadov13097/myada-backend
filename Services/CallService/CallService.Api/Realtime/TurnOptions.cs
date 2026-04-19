namespace CallService.Api.Realtime;

public sealed class TurnOptions
{
    public bool Enabled { get; set; }
    public string[] Urls { get; set; } = Array.Empty<string>();
    public string SharedSecret { get; set; } = string.Empty;
    public int CredentialTtlSeconds { get; set; } = 3600;
}
