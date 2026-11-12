using System.Reflection;
using Npgsql;

namespace Sme.BuildingBlocks.Data;

/// <summary>
/// Applies a service's SQL migrations in file-name order, once each. Scripts are embedded resources
/// ending in <c>.sql</c> under the service's <c>Migrations</c> folder, named <c>NNNN_description.sql</c>.
/// Applied names are recorded in <c>&lt;schema&gt;.schema_migrations</c>. Each script runs in its own
/// transaction with the login (owner) role.
/// </summary>
public sealed class SqlMigrator
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly string _schema;
    private readonly Assembly _assembly;

    public SqlMigrator(NpgsqlDataSource dataSource, string schema, Assembly migrationsAssembly)
    {
        _dataSource = dataSource;
        _schema = TenantConnectionFactory.ValidIdentifier(schema);
        _assembly = migrationsAssembly;
    }

    public IReadOnlyList<string> ScriptNames() =>
        _assembly.GetManifestResourceNames()
            .Where(n => n.Contains(".Migrations.", StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.Ordinal))
            .Select(n => n[(n.IndexOf(".Migrations.", StringComparison.Ordinal) + ".Migrations.".Length)..])
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

    /// <summary>Applies pending scripts. Returns the names that were applied in this call.</summary>
    public async Task<IReadOnlyList<string>> MigrateAsync(CancellationToken ct = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);

        await Execute(connection, $"CREATE SCHEMA IF NOT EXISTS \"{_schema}\";", ct);
        await Execute(connection,
            $"CREATE TABLE IF NOT EXISTS \"{_schema}\".schema_migrations (" +
            "name text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());", ct);

        var applied = new HashSet<string>(StringComparer.Ordinal);
        await using (var cmd = new NpgsqlCommand($"SELECT name FROM \"{_schema}\".schema_migrations", connection))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct)) applied.Add(reader.GetString(0));
        }

        var appliedNow = new List<string>();
        foreach (var name in ScriptNames())
        {
            if (applied.Contains(name)) continue;
            var sql = ReadScript(name);
            await using var tx = await connection.BeginTransactionAsync(ct);
            await Execute(connection, sql, ct, tx);
            await using (var record = new NpgsqlCommand($"INSERT INTO \"{_schema}\".schema_migrations (name) VALUES (@name)", connection, tx))
            {
                record.Parameters.AddWithValue("name", name);
                await record.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct);
            appliedNow.Add(name);
        }
        return appliedNow;
    }

    private string ReadScript(string name)
    {
        var resource = _assembly.GetManifestResourceNames().Single(n => n.EndsWith(".Migrations." + name, StringComparison.Ordinal));
        using var stream = _assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task Execute(NpgsqlConnection connection, string sql, CancellationToken ct, NpgsqlTransaction? tx = null)
    {
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
