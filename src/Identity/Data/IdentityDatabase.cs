using Npgsql;

namespace Sme.Identity.Data;

/// <summary>
/// Creates the Identity service's own schema (ADR-0005: one schema per service) and the
/// row-level security that isolates tenants. Every statement can run again safely.
/// </summary>
public static class IdentityDatabase
{
    /// <summary>The role every request runs as. It owns nothing, so row-level security applies to it.</summary>
    public const string AppRole = "identity_app";

    private const long MigrationLockKey = 7360001;

    private static readonly string[] Statements =
    [
        "CREATE SCHEMA IF NOT EXISTS identity",

        """
        CREATE TABLE IF NOT EXISTS identity.tenants (
            id         uuid PRIMARY KEY,
            name       text NOT NULL CHECK (char_length(name) BETWEEN 2 AND 120),
            created_at timestamptz NOT NULL DEFAULT now()
        )
        """,

        // FORCE makes the policy apply to the table owner too, not only to other roles.
        "ALTER TABLE identity.tenants ENABLE ROW LEVEL SECURITY",
        "ALTER TABLE identity.tenants FORCE ROW LEVEL SECURITY",

        // A tenant row is visible, and can be inserted, only when it is the current tenant.
        // With no tenant set the comparison is NULL, so nothing is visible.
        "DROP POLICY IF EXISTS tenant_isolation ON identity.tenants",
        """
        CREATE POLICY tenant_isolation ON identity.tenants
            USING (id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
            WITH CHECK (id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
        """,

        $"GRANT USAGE ON SCHEMA identity TO {AppRole}",
        $"GRANT SELECT, INSERT ON identity.tenants TO {AppRole}",
    ];

    public static async Task MigrateAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Two instances starting together must not both create the same objects.
        await ExecuteAsync(connection, transaction, $"SELECT pg_advisory_xact_lock({MigrationLockKey})", cancellationToken);

        await using (var roleExists = new NpgsqlCommand($"SELECT 1 FROM pg_roles WHERE rolname = '{AppRole}'", connection, transaction))
        {
            if (await roleExists.ExecuteScalarAsync(cancellationToken) is null)
            {
                await ExecuteAsync(connection, transaction, $"CREATE ROLE {AppRole} NOLOGIN", cancellationToken);
            }
        }

        foreach (var statement in Statements)
        {
            await ExecuteAsync(connection, transaction, statement, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
