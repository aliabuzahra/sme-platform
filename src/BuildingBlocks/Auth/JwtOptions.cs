namespace Sme.BuildingBlocks.Auth;

/// <summary>
/// How platform access tokens are signed and checked. Bound from the <c>Jwt</c> section.
/// Every service validates with the same key until UC-011 moves validation to the Gateway.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 key, at least 32 bytes. Development has a placeholder; other environments must set it.</summary>
    public string SigningKey { get; set; } = "";

    public string Issuer { get; set; } = "sme-identity";

    public string Audience { get; set; } = "sme-platform";

    public int LifetimeMinutes { get; set; } = 12 * 60;
}
