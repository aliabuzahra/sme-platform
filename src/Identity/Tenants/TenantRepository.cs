using Npgsql;
using Sme.BuildingBlocks.Data;

namespace Sme.Identity.Tenants;

public sealed class TenantRepository(TenantConnectionFactory connections)
{
    /// <summary>Registers a tenant. Runs as the platform: there is no tenant yet to act for.</summary>
    public async Task<Tenant> RegisterAsync(string name, string country, CancellationToken ct)
    {
        await using var connection = await connections.OpenPlatformAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO identity.tenants (name, country) VALUES (@name, @country) " +
            "RETURNING id, name, country, status, created_at", connection);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("country", country);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return Read(reader);
    }

    /// <summary>Reads a tenant as the current tenant. Row-level security hides every other tenant.</summary>
    public async Task<Tenant?> GetAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await connections.OpenForCurrentTenantAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, country, status, created_at FROM identity.tenants WHERE id = @id", connection);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    private static Tenant Read(NpgsqlDataReader r) => new(
        r.GetGuid(0),
        r.GetString(1),
        r.GetString(2),
        r.GetString(3),
        r.GetFieldValue<DateTime>(4));
}
