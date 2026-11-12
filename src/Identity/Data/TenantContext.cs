using Npgsql;

namespace Sme.Identity.Data;

/// <summary>
/// Puts a transaction in one tenant's context: it switches to the unprivileged application role
/// and sets <c>app.tenant_id</c>, which the row-level security policies read. Both settings are
/// local to the transaction, so a pooled connection never carries a tenant to the next request.
/// </summary>
public static class TenantContext
{
    public static async Task ApplyAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using (var role = new NpgsqlCommand($"SET LOCAL ROLE {IdentityDatabase.AppRole}", connection, transaction))
        {
            await role.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var tenant = new NpgsqlCommand("SELECT set_config('app.tenant_id', $1, true)", connection, transaction);
        tenant.Parameters.Add(new NpgsqlParameter { Value = tenantId.ToString() });
        await tenant.ExecuteNonQueryAsync(cancellationToken);
    }
}
