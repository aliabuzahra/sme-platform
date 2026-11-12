using System.Net;
using System.Net.Http.Json;
using Sme.BuildingBlocks.Tenancy;
using Sme.Identity.Branches;
using Sme.Identity.Tenants;

namespace Sme.Identity.Tests;

public sealed class BranchTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    private static readonly Dictionary<string, List<OpenInterval>> Weekdays = new()
    {
        ["sun"] = [new("09:00", "18:00")],
        ["mon"] = [new("09:00", "18:00")],
    };

    private static readonly Dictionary<string, List<OpenInterval>> Evenings = new()
    {
        ["thu"] = [new("16:00", "23:00")],
        ["fri"] = [new("13:00", "15:00"), new("16:00", "23:30")],
    };

    private HttpClient Client(Guid tenant)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TenantHeaderMiddleware.HeaderName, tenant.ToString());
        return client;
    }

    private async Task<Guid> NewTenant(string name)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/tenants/", new RegisterTenantRequest(name, null, "+962790000003"));
        return (await response.Content.ReadFromJsonAsync<Tenant>())!.Id;
    }

    [Fact]
    public async Task Owner_can_add_a_second_branch_with_its_own_hours()
    {
        var tenant = await NewTenant("Two shops");
        var client = Client(tenant);

        var first = await client.PostAsJsonAsync("/branches/", new CreateBranchRequest("Downtown", Weekdays));
        var second = await client.PostAsJsonAsync("/branches/", new CreateBranchRequest("Abdoun", Evenings));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var branches = await client.GetFromJsonAsync<List<Branch>>("/branches/");
        Assert.Equal(["Downtown", "Abdoun"], branches!.Select(b => b.Name));
        Assert.All(branches, b => Assert.Equal(tenant, b.TenantId));

        var abdoun = branches.Single(b => b.Name == "Abdoun");
        Assert.Equal(["thu", "fri"], abdoun.Hours.Keys);
        Assert.Equal(2, abdoun.Hours["fri"].Count);
        Assert.Equal(new OpenInterval("16:00", "23:30"), abdoun.Hours["fri"][1]);

        var downtown = await client.GetFromJsonAsync<Branch>($"/branches/{branches.Single(b => b.Name == "Downtown").Id}");
        Assert.Equal(Weekdays["mon"], downtown!.Hours["mon"]);
    }

    [Fact]
    public async Task Branches_are_invisible_to_other_tenants()
    {
        var mine = await NewTenant("Mine");
        var theirs = await NewTenant("Theirs");
        var created = await Client(mine).PostAsJsonAsync("/branches/", new CreateBranchRequest("Only mine", null));
        var branch = (await created.Content.ReadFromJsonAsync<Branch>())!;

        var list = await Client(theirs).GetFromJsonAsync<List<Branch>>("/branches/");
        var single = await Client(theirs).GetAsync($"/branches/{branch.Id}");

        Assert.Empty(list!);
        Assert.Equal(HttpStatusCode.NotFound, single.StatusCode);
    }

    [Fact]
    public async Task Invalid_hours_and_duplicate_names_are_rejected()
    {
        var tenant = await NewTenant("Strict");
        var client = Client(tenant);

        var bad = await client.PostAsJsonAsync("/branches/", new CreateBranchRequest("Bad hours",
            new() { ["mon"] = [new("18:00", "09:00")] }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        await client.PostAsJsonAsync("/branches/", new CreateBranchRequest("Main", null));
        var duplicate = await client.PostAsJsonAsync("/branches/", new CreateBranchRequest("Main", null));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Branch_endpoints_need_a_tenant()
    {
        var response = await factory.CreateClient().GetAsync("/branches/");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
