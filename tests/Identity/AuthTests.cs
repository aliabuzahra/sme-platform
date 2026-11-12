using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Sme.BuildingBlocks.Data;
using Sme.Identity.Auth;
using Sme.Identity.Branches;
using Sme.Identity.Tenants;

namespace Sme.Identity.Tests;

public sealed class AuthTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    private static int _phoneSeed = 100;
    private static string NextPhone() => $"+9627900{Interlocked.Increment(ref _phoneSeed):D5}";

    private FakeOtpSender Sender => factory.Services.GetRequiredService<FakeOtpSender>();

    private async Task<Tenant> Register(string name, string ownerPhone)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/tenants/", new RegisterTenantRequest(name, null, ownerPhone));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Tenant>())!;
    }

    private async Task<string> RequestCode(string phone)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/auth/otp/request", new OtpRequest(phone));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return Sender.LastCodeFor(phone) ?? throw new Xunit.Sdk.XunitException($"No code was sent to {phone}.");
    }

    private Task<HttpResponseMessage> Verify(string phone, string code, Guid? tenantId = null) =>
        factory.CreateClient().PostAsJsonAsync("/auth/otp/verify", new OtpVerifyRequest(phone, code, tenantId));

    [Fact]
    public async Task Sign_in_works_end_to_end_with_the_fake_sender()
    {
        var phone = NextPhone();
        var tenant = await Register("Owner's shop", phone);
        var code = await RequestCode(phone);

        var response = await Verify(phone, code);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var signIn = (await response.Content.ReadFromJsonAsync<SignInResponse>())!;
        Assert.Equal(tenant.Id, signIn.Tenant.Id);
        Assert.Equal("owner", signIn.Role);
        Assert.Equal(["core"], signIn.Modules);
        Assert.True(signIn.ExpiresAt > DateTimeOffset.UtcNow.AddHours(11));

        // The token carries tenant and module claims, and services read the tenant from it.
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signIn.AccessToken);
        var me = await client.GetFromJsonAsync<MeResponse>("/auth/me");
        Assert.Equal(tenant.Id.ToString(), me!.TenantId);
        Assert.Equal(phone, me.Phone);
        Assert.Equal(["core"], me.Modules);

        var created = await client.PostAsJsonAsync("/branches/", new CreateBranchRequest("Main", null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(tenant.Id, (await created.Content.ReadFromJsonAsync<Branch>())!.TenantId);

        var mine = await client.GetFromJsonAsync<Tenant>($"/tenants/{tenant.Id}");
        Assert.Equal(tenant.Id, mine!.Id);
    }

    [Fact]
    public async Task Wrong_codes_are_rejected_and_burn_the_code_after_too_many_attempts()
    {
        var phone = NextPhone();
        await Register("Strict shop", phone);
        var code = await RequestCode(phone);
        var wrong = code == "000000" ? "111111" : "000000";

        for (var i = 0; i < 5; i++)
        {
            var attempt = await Verify(phone, wrong);
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
        }
        // Even the right code is refused now.
        var burnt = await Verify(phone, code);
        Assert.Equal(HttpStatusCode.Unauthorized, burnt.StatusCode);
        Assert.Contains("Too many", await burnt.Content.ReadAsStringAsync());

        // A fresh code works again.
        var fresh = await RequestCode(phone);
        Assert.Equal(HttpStatusCode.OK, (await Verify(phone, fresh)).StatusCode);
    }

    [Fact]
    public async Task Expired_codes_are_rejected()
    {
        var phone = NextPhone();
        await Register("Slow shop", phone);
        var code = await RequestCode(phone);

        // Age the code in the database instead of waiting five minutes.
        using var scope = factory.Services.CreateScope();
        await using var connection = await scope.ServiceProvider.GetRequiredService<TenantConnectionFactory>().OpenPlatformAsync();
        await using (var age = new Npgsql.NpgsqlCommand("UPDATE identity.otp_codes SET expires_at = now() - interval '1 second' WHERE phone = @phone", connection))
        {
            age.Parameters.AddWithValue("phone", phone);
            await age.ExecuteNonQueryAsync();
        }

        var response = await Verify(phone, code);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("expired", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_code_can_be_used_once_and_a_new_request_retires_the_old_code()
    {
        var phone = NextPhone();
        await Register("Once shop", phone);
        var first = await RequestCode(phone);
        var second = await RequestCode(phone);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Verify(phone, first)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Verify(phone, second)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Verify(phone, second)).StatusCode);
    }

    [Fact]
    public async Task Unknown_phones_get_the_same_answer_but_no_code()
    {
        var phone = NextPhone();
        var response = await factory.CreateClient().PostAsJsonAsync("/auth/otp/request", new OtpRequest(phone));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Null(Sender.LastCodeFor(phone));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Verify(phone, "123456")).StatusCode);
    }

    [Fact]
    public async Task A_phone_on_two_businesses_must_choose_one()
    {
        var phone = NextPhone();
        var first = await Register("First business", phone);
        var second = await Register("Second business", phone);

        var ambiguous = await Verify(phone, await RequestCode(phone));
        Assert.Equal(HttpStatusCode.Conflict, ambiguous.StatusCode);
        Assert.Contains(second.Name, await ambiguous.Content.ReadAsStringAsync());

        var chosen = await Verify(phone, await RequestCode(phone), second.Id);
        Assert.Equal(HttpStatusCode.OK, chosen.StatusCode);
        var signIn = (await chosen.Content.ReadFromJsonAsync<SignInResponse>())!;
        Assert.Equal(second.Id, signIn.Tenant.Id);
        Assert.NotEqual(first.Id, signIn.Tenant.Id);
    }

    [Fact]
    public async Task Requests_are_throttled_per_phone()
    {
        var phone = NextPhone();
        await Register("Busy shop", phone);
        var codes = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            await factory.CreateClient().PostAsJsonAsync("/auth/otp/request", new OtpRequest(phone));
            codes.Add(Sender.LastCodeFor(phone)!);
        }
        // The fourth request inside the window sent nothing new.
        Assert.Equal(codes[2], codes[3]);
    }

    [Fact]
    public async Task Tampered_and_missing_tokens_are_unauthorized()
    {
        var phone = NextPhone();
        await Register("Token shop", phone);
        var signIn = (await (await Verify(phone, await RequestCode(phone))).Content.ReadFromJsonAsync<SignInResponse>())!;

        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/me")).StatusCode);

        var tampered = signIn.AccessToken[..^4] + "AAAA";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/me")).StatusCode);
    }

    private sealed record MeResponse(string UserId, string TenantId, string Phone, string Role, string[] Modules);
}
