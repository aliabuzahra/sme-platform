using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sme.BuildingBlocks.Data;
using Sme.BuildingBlocks.Tenancy;
using Sme.Identity.Tenants;

namespace Sme.Identity.Tests;

public sealed class TenantTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    private HttpClient Client(Guid? tenant = null)
    {
        var client = factory.CreateClient();
        if (tenant is { } id) client.DefaultRequestHeaders.Add(TenantHeaderMiddleware.HeaderName, id.ToString());
        return client;
    }

    private async Task<Tenant> Register(string name)
    {
        var response = await Client().PostAsJsonAsync("/tenants/", new RegisterTenantRequest(name, null, "+962790000001"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Tenant>())!;
    }

    [Fact]
    public async Task A_tenant_can_be_created_and_read()
    {
        var created = await Register("Amman Bakery");

        Assert.Equal("Amman Bakery", created.Name);
        Assert.Equal("JO", created.Country);
        Assert.Equal("active", created.Status);

        var read = await Client(created.Id).GetFromJsonAsync<Tenant>($"/tenants/{created.Id}");
        Assert.Equal(created, read);
    }

    [Fact]
    public async Task Another_tenants_row_is_not_found_through_the_api()
    {
        var mine = await Register("Mine");
        var theirs = await Register("Theirs");

        var response = await Client(mine.Id).GetAsync($"/tenants/{theirs.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Row_level_security_blocks_reading_another_tenants_rows_in_sql()
    {
        var mine = await Register("Mine SQL");
        await Register("Theirs SQL");

        using var scope = factory.Services.CreateScope();
        var connections = scope.ServiceProvider.GetRequiredService<TenantConnectionFactory>();

        await using var asMine = await connections.OpenForTenantAsync(mine.Id);
        var visibleIds = await Ids(asMine);
        Assert.Equal([mine.Id], visibleIds);

        // The application role with no tenant set sees nothing at all.
        await using var noTenant = await connections.OpenPlatformAsync();
        await using (var sw = new NpgsqlCommand("SET ROLE sme_app", noTenant)) await sw.ExecuteNonQueryAsync();
        Assert.Empty(await Ids(noTenant));

        // And it cannot smuggle a row in for another tenant either.
        await using var insert = new NpgsqlCommand(
            "INSERT INTO identity.tenants (id, name) VALUES (gen_random_uuid(), 'smuggled')", asMine);
        var ex = await Assert.ThrowsAsync<PostgresException>(() => insert.ExecuteNonQueryAsync());
        Assert.Equal("42501", ex.SqlState); // insufficient_privilege: the row-level policy rejected it
    }

    [Fact]
    public async Task Reading_without_a_tenant_is_unauthorized()
    {
        var tenant = await Register("No header");

        var response = await Client().GetAsync($"/tenants/{tenant.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_malformed_tenant_header_is_a_bad_request()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TenantHeaderMiddleware.HeaderName, "not-a-uuid");

        var response = await client.GetAsync($"/tenants/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", null, "+962790000002")]
    [InlineData("   ", null, "+962790000002")]
    [InlineData("Fine", "Jordan", "+962790000002")]
    [InlineData("Fine", null, "12345")]
    public async Task Invalid_registrations_are_rejected(string name, string? country, string ownerPhone)
    {
        var response = await Client().PostAsJsonAsync("/tenants/", new RegisterTenantRequest(name, country, ownerPhone));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Tests_run_against_their_own_database()
    {
        var options = factory.Services.GetRequiredService<DatabaseOptions>();
        Assert.Contains(factory.DatabaseName, options.ConnectionString);
    }

    [Fact]
    public async Task Migrations_apply_once()
    {
        var migrator = factory.Services.GetRequiredService<SqlMigrator>();

        Assert.Contains("0001_tenants.sql", migrator.ScriptNames());
        Assert.Empty(await migrator.MigrateAsync());
    }

    private static async Task<List<Guid>> Ids(NpgsqlConnection connection)
    {
        await using var cmd = new NpgsqlCommand("SELECT id FROM identity.tenants ORDER BY created_at", connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        var ids = new List<Guid>();
        while (await reader.ReadAsync()) ids.Add(reader.GetGuid(0));
        return ids;
    }
}
