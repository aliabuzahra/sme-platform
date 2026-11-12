using Npgsql;
using Sme.BuildingBlocks.Tenancy;

namespace Sme.BuildingBlocks.Data;

/// <summary>
/// Opens Postgres connections in one of two modes (ADR-0007):
/// <list type="bullet">
/// <item><b>Platform</b>: the owning login role, no tenant. Only for operations that have no tenant
/// yet, such as registering one. Row-level security does not filter the owner's own tables.</item>
/// <item><b>Tenant</b>: <c>SET ROLE</c> to the application role and <c>app.tenant_id</c> set for the
/// session, so every policy filters to that tenant. All request handling uses this mode.</item>
/// </list>
/// Npgsql resets role and settings when a connection goes back to the pool (DISCARD ALL), so one
/// request's tenant never leaks into the next.
/// </summary>
public sealed class TenantConnectionFactory
{
    public const string TenantSetting = "app.tenant_id";

    private readonly NpgsqlDataSource _dataSource;
    private readonly string _applicationRole;
    private readonly ITenantContext _tenant;

    public TenantConnectionFactory(NpgsqlDataSource dataSource, DatabaseOptions options, ITenantContext tenant)
    {
        _dataSource = dataSource;
        _applicationRole = ValidIdentifier(options.ApplicationRole);
        _tenant = tenant;
    }

    public ValueTask<NpgsqlConnection> OpenPlatformAsync(CancellationToken ct = default) =>
        _dataSource.OpenConnectionAsync(ct);

    /// <summary>Opens a connection scoped to the current request's tenant.</summary>
    public ValueTask<NpgsqlConnection> OpenForCurrentTenantAsync(CancellationToken ct = default) =>
        OpenForTenantAsync(_tenant.TenantId ?? throw new InvalidOperationException("No tenant in the current request."), ct);

    public async ValueTask<NpgsqlConnection> OpenForTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var connection = await _dataSource.OpenConnectionAsync(ct);
        try
        {
            // The role name is validated as an identifier; the tenant id goes through a parameter.
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SET ROLE \"{_applicationRole}\"; SELECT set_config('{TenantSetting}', @tenant, false);";
            cmd.Parameters.AddWithValue("tenant", tenantId.ToString());
            await cmd.ExecuteNonQueryAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    internal static string ValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name) || !name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_'))
        {
            throw new ArgumentException($"'{name}' is not a plain lowercase SQL identifier.", nameof(name));
        }
        return name;
    }
}
