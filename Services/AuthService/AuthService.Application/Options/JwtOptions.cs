namespace AuthService.Application.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "myada-auth";
    public string Audience { get; set; } = "myada-api";
    public int TokenLifetimeHours { get; set; } = 2;
}
