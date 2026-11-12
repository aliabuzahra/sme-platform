namespace Sme.BuildingBlocks.Tenancy;

/// <summary>
/// The tenant the current request acts for. Scoped per request. Null until something sets it.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
}

/// <summary>
/// Request-scoped holder for the current tenant (ADR-0007).
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void Set(Guid tenantId) => TenantId = tenantId;

    public Guid RequireTenantId() =>
        TenantId ?? throw new InvalidOperationException("No tenant in the current request.");
}
