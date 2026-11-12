namespace Sme.BuildingBlocks.Auth;

/// <summary>Claim names every service agrees on. Issued by Identity (UC-007), checked by the Gateway (UC-011).</summary>
public static class PlatformClaims
{
    /// <summary>The tenant the token acts for.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>The signed-in phone number in E.164 form.</summary>
    public const string Phone = "phone";

    /// <summary>The user's role inside the tenant: <c>owner</c> or <c>staff</c>.</summary>
    public const string Role = "role";

    /// <summary>JSON array of module names the tenant is entitled to. One claim value per module.</summary>
    public const string Modules = "modules";
}
