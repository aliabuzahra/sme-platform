namespace Sme.Identity.Tenants;

public sealed record Tenant(Guid Id, string Name, string Country, string Status, DateTimeOffset CreatedAt);

public sealed record RegisterTenantRequest(string Name, string? Country);
