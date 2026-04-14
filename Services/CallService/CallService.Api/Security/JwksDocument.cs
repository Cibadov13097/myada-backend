namespace CallService.Api.Security;

public sealed class JwksDocument
{
    public List<JwksKey> Keys { get; set; } = new();
}
