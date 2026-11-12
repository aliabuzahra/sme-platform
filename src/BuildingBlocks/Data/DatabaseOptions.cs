namespace Sme.BuildingBlocks.Data;

/// <summary>
/// Where a service's data lives. Bound from the <c>Database</c> configuration section.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Npgsql connection string for the login role that owns the service's schema.</summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// NOLOGIN role the service switches to for tenant work (<c>SET ROLE</c>). It must not own the
    /// tables and must not have BYPASSRLS, so row-level security applies (ADR-0007).
    /// </summary>
    public string ApplicationRole { get; set; } = "sme_app";

    /// <summary>Schema the service owns (ADR-0008). Its migrations and tables live here.</summary>
    public string Schema { get; set; } = "";
}
