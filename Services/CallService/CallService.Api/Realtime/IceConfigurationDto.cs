namespace CallService.Api.Realtime;

public sealed record IceServerDto(
    string[] Urls,
    string? Username,
    string? Credential);

public sealed record IceConfigurationDto(
    DateTime GeneratedAtUtc,
    DateTime ExpiresAtUtc,
    IceServerDto[] IceServers);
