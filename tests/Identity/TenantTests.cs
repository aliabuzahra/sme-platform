using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sme.Identity.Data;
using Sme.Identity.Tenants;

namespace Sme.Identity.Tests;

/// <summary>
/// UC-006. These run the real service against Postgres (start it with `docker compose up -d --wait`):
/// row-level security is a database feature, so a fake database would prove nothing.
/// </summary>
public sealed class TenantTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed record TenantDto(Guid Id, string Name, DateTimeOffset CreatedAt);

    private readonly HttpClient _client = factory.CreateClient();

    private async Task<TenantDto> RegisterAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/tenants", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
        Assert.NotNull(tenant);
        Assert.Equal($"/tenants/{tenant.Id}", response.Headers.Location?.OriginalString);
        return tenant;
    }

    private Task<HttpResponseMessage> GetAsync(Guid tenantId, Guid? asTenant)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/tenants/{tenantId}");
        if (asTenant is not null)
        {
            request.Headers.Add(TenantEndpoints.TenantHeader, asTenant.Value.ToString());
        }

        return _client.SendAsync(request);
    }

    [Fact]
    public async Task A_tenant_can_be_created_and_read()
    {
        var created = await RegisterAsync("  مخبز الواحة  ");
        Assert.Equal("مخبز الواحة", created.Name);
        Assert.NotEqual(Guid.Empty, created.Id);

        var response = await GetAsync(created.Id, asTenant: created.Id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var read = await response.Content.ReadFromJsonAsync<TenantDto>();
        Assert.Equal(created, read);
    }

    [Fact]
    public async Task A_tenant_cannot_read_another_tenant_through_the_api()
    {
        var first = await RegisterAsync("Shop A");
        var second = await RegisterAsync("Shop B");

        var response = await GetAsync(second.Id, asTenant: first.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reading_without_a_tenant_is_rejected()
    {
        var tenant = await RegisterAsync("Shop C");

        var response = await GetAsync(tenant.Id, asTenant: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" a ")]
    [InlineData(null)]
    public async Task A_name_that_is_missing_or_too_short_is_rejected(string? name)
    {
        var response = await _client.PostAsJsonAsync("/tenants", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_name_longer_than_the_limit_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/tenants", new { name = new string('x', TenantEndpoints.NameMaxLength + 1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>The acceptance test: the database itself hides and refuses another tenant's rows.</summary>
    [Fact]
    public async Task Row_level_security_blocks_another_tenants_rows()
    {
        var mine = await RegisterAsync("Mine");
        var theirs = await RegisterAsync("Theirs");
        var dataSource = factory.Services.GetRequiredService<NpgsqlDataSource>();

        await using var connection = await dataSource.OpenConnectionAsync();

        // In my context, an unfiltered query returns my row and only my row.
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await TenantContext.ApplyAsync(connection, transaction, mine.Id);
            var visible = new List<Guid>();
            await using (var all = new NpgsqlCommand("SELECT id FROM identity.tenants", connection, transaction))
            await using (var reader = await all.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    visible.Add(reader.GetGuid(0));
                }
            }

            Assert.Equal(new[] { mine.Id }, visible);
            Assert.DoesNotContain(theirs.Id, visible);
            await transaction.RollbackAsync();
        }

        // In my context, writing a row for a different tenant is refused by the policy.
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await TenantContext.ApplyAsync(connection, transaction, mine.Id);
            await using var insert = new NpgsqlCommand("INSERT INTO identity.tenants (id, name) VALUES ($1, 'Forged')", connection, transaction);
            insert.Parameters.Add(new NpgsqlParameter { Value = Guid.CreateVersion7() });
            var error = await Assert.ThrowsAsync<PostgresException>(() => insert.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
            await transaction.RollbackAsync();
        }

        // The application role with no tenant set sees nothing at all.
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await using (var role = new NpgsqlCommand($"SET LOCAL ROLE {IdentityDatabase.AppRole}", connection, transaction))
            {
                await role.ExecuteNonQueryAsync();
            }

            await using var count = new NpgsqlCommand("SELECT count(*) FROM identity.tenants", connection, transaction);
            Assert.Equal(0L, (long)(await count.ExecuteScalarAsync())!);
            await transaction.RollbackAsync();
        }

        // The tenant set inside a transaction is gone once it ends.
        await using var after = new NpgsqlCommand("SELECT coalesce(current_setting('app.tenant_id', true), '')", connection);
        Assert.Equal(string.Empty, (string)(await after.ExecuteScalarAsync())!);
    }
}
