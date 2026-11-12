using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Sme.BuildingBlocks.Data;
using Sme.BuildingBlocks.Tenancy;

namespace Sme.Identity.Branches;

/// <summary>All access runs as the current tenant, so row-level security scopes every statement.</summary>
public sealed class BranchRepository(TenantConnectionFactory connections, ITenantContext tenant)
{
    private const string Columns = "id, tenant_id, name, hours, created_at";

    public async Task<Branch> CreateAsync(string name, Dictionary<string, List<OpenInterval>> hours, CancellationToken ct)
    {
        await using var connection = await connections.OpenForCurrentTenantAsync(ct);
        await using var cmd = new NpgsqlCommand(
            $"INSERT INTO identity.branches (tenant_id, name, hours) VALUES (@tenant, @name, @hours) RETURNING {Columns}", connection);
        cmd.Parameters.AddWithValue("tenant", tenant.TenantId!.Value);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.Add(new NpgsqlParameter("hours", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(hours, OpeningHours.Json) });
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return Read(reader);
    }

    public async Task<IReadOnlyList<Branch>> ListAsync(CancellationToken ct)
    {
        await using var connection = await connections.OpenForCurrentTenantAsync(ct);
        await using var cmd = new NpgsqlCommand($"SELECT {Columns} FROM identity.branches ORDER BY created_at, name", connection);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var branches = new List<Branch>();
        while (await reader.ReadAsync(ct)) branches.Add(Read(reader));
        return branches;
    }

    public async Task<Branch?> GetAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await connections.OpenForCurrentTenantAsync(ct);
        await using var cmd = new NpgsqlCommand($"SELECT {Columns} FROM identity.branches WHERE id = @id", connection);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    private static Branch Read(NpgsqlDataReader r) => new(
        r.GetGuid(0),
        r.GetGuid(1),
        r.GetString(2),
        // jsonb stores keys in its own order; Normalize puts the days back in week order.
        OpeningHours.Normalize(JsonSerializer.Deserialize<Dictionary<string, List<OpenInterval>>>(r.GetString(3), OpeningHours.Json)),
        r.GetFieldValue<DateTime>(4));
}
