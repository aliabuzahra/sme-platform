using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Sme.Identity.Tests;

/// <summary>
/// Hosts the Identity service against a database created for this test run and dropped afterwards.
/// Needs a reachable Postgres. The admin connection comes from <c>SME_TEST_ADMIN_CONNECTION</c>,
/// defaulting to the Compose placeholders in README.md.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string AdminConnection =
        Environment.GetEnvironmentVariable("SME_TEST_ADMIN_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=postgres;Username=sme;Password=change-me";

    public string DatabaseName { get; } = "sme_test_" + Guid.NewGuid().ToString("N")[..12];

    public string ConnectionString =>
        new NpgsqlConnectionStringBuilder(AdminConnection) { Database = DatabaseName }.ConnectionString;

    public async Task InitializeAsync()
    {
        await using var admin = new NpgsqlConnection(AdminConnection);
        await admin.OpenAsync();
        await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{DatabaseName}\"", admin);
        await cmd.ExecuteNonQueryAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(AdminConnection);
        await admin.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{DatabaseName}\" WITH (FORCE)", admin);
        await cmd.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = ConnectionString,
            }));
    }
}
