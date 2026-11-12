namespace Sme.Identity.Tenants;

public sealed record Tenant(Guid Id, string Name, string Country, string Status, DateTimeOffset CreatedAt);

/// <summary>The owner's phone becomes the first user of the tenant, so they can sign in (UC-007).</summary>
public sealed record RegisterTenantRequest(string Name, string? Country, string OwnerPhone);
