using Npgsql;
using Sme.Identity.Data;

namespace Sme.Identity.Tenants;

public sealed record Tenant(Guid Id, string Name, DateTimeOffset CreatedAt);

/// <summary>Reads and writes tenants. Every query runs inside a tenant context, so row-level security always applies.</summary>
public sealed class TenantStore(NpgsqlDataSource dataSource)
{
    /// <summary>Registers a new tenant. The new tenant is its own context, which is what the insert policy requires.</summary>
    public async Task<Tenant> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var id = Guid.CreateVersion7();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantContext.ApplyAsync(connection, transaction, id, cancellationToken);

        DateTimeOffset createdAt;
        await using (var insert = new NpgsqlCommand("INSERT INTO identity.tenants (id, name) VALUES ($1, $2) RETURNING created_at", connection, transaction))
        {
            insert.Parameters.Add(new NpgsqlParameter { Value = id });
            insert.Parameters.Add(new NpgsqlParameter { Value = name });
            await using var reader = await insert.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            createdAt = reader.GetFieldValue<DateTimeOffset>(0);
        }

        await transaction.CommitAsync(cancellationToken);
        return new Tenant(id, name, createdAt);
    }

    /// <summary>Returns the tenant only if it is the caller's own; another tenant's row is invisible, not forbidden.</summary>
    public async Task<Tenant?> FindAsync(Guid callerTenantId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantContext.ApplyAsync(connection, transaction, callerTenantId, cancellationToken);

        Tenant? tenant = null;
        await using (var select = new NpgsqlCommand("SELECT id, name, created_at FROM identity.tenants WHERE id = $1", connection, transaction))
        {
            select.Parameters.Add(new NpgsqlParameter { Value = tenantId });
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                tenant = new Tenant(reader.GetGuid(0), reader.GetString(1), reader.GetFieldValue<DateTimeOffset>(2));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return tenant;
    }
}
