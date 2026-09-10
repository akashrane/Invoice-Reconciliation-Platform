namespace ReconFlow.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public required string Key { get; init; }
    public string Issuer { get; init; } = "ReconFlow";
    public string Audience { get; init; } = "ReconFlow.UI";
    public int ExpiryMinutes { get; init; } = 480;
}
