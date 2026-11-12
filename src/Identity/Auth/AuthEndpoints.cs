using System.Security.Claims;
using Sme.BuildingBlocks.Auth;

namespace Sme.Identity.Auth;

public sealed record OtpRequest(string Phone);
public sealed record OtpVerifyRequest(string Phone, string Code, Guid? TenantId);
public sealed record TenantChoice(Guid Id, string Name);
public sealed record SignInResponse(string AccessToken, DateTimeOffset ExpiresAt, TenantChoice Tenant, Guid UserId, string Role, string[] Modules);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        // Step 1: a code goes to the phone. The answer is the same whether or not the phone is known,
        // so this endpoint cannot be used to list customers' numbers.
        group.MapPost("/otp/request", async (OtpRequest request, UserRepository users, OtpService otp, CancellationToken ct) =>
        {
            if (!PhoneNumber.TryNormalize(request.Phone, out var phone))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["phone"] = ["Enter a phone number in international form, e.g. +962791234567."] });
            }
            if ((await users.FindByPhoneAsync(phone, ct)).Count > 0)
            {
                await otp.RequestAsync(phone, ct);
            }
            return Results.Accepted(value: new { phone, expiresInSeconds = otp.LifetimeSeconds });
        });

        // Step 2: the code comes back and a token goes out. A phone on several tenants must say which one.
        group.MapPost("/otp/verify", async (OtpVerifyRequest request, UserRepository users, OtpService otp, TokenIssuer tokens, CancellationToken ct) =>
        {
            if (!PhoneNumber.TryNormalize(request.Phone, out var phone) || string.IsNullOrWhiteSpace(request.Code))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["phone"] = ["Phone and code are required."] });
            }

            var memberships = await users.FindByPhoneAsync(phone, ct);
            var chosen = request.TenantId is { } tenantId ? memberships.FirstOrDefault(m => m.TenantId == tenantId) : null;
            if (chosen is null && memberships.Count == 1 && request.TenantId is null) chosen = memberships[0];

            // Codes are checked even when the phone is unknown, so timing does not reveal membership.
            var outcome = await otp.VerifyAsync(phone, request.Code.Trim(), ct);
            if (outcome != OtpVerifyOutcome.Ok || memberships.Count == 0)
            {
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: outcome switch
                {
                    OtpVerifyOutcome.Expired => "The code has expired. Request a new one.",
                    OtpVerifyOutcome.TooManyAttempts => "Too many wrong codes. Request a new one.",
                    _ => "Wrong phone number or code.",
                });
            }
            if (chosen is null)
            {
                // The code is spent, which is deliberate: choosing a tenant means requesting a fresh code.
                return Results.Conflict(new
                {
                    error = "This phone belongs to more than one business. Request a new code and pass tenantId.",
                    tenants = memberships.Select(m => new TenantChoice(m.TenantId, m.TenantName)),
                });
            }

            var token = tokens.Issue(chosen.UserId, chosen.TenantId, chosen.Phone, chosen.Role, chosen.Modules);
            return Results.Ok(new SignInResponse(token.AccessToken, token.ExpiresAt, new TenantChoice(chosen.TenantId, chosen.TenantName), chosen.UserId, chosen.Role, chosen.Modules));
        });

        // What the token says, for clients and for checking the Gateway later (UC-011).
        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            userId = user.FindFirstValue("sub"),
            tenantId = user.FindFirstValue(PlatformClaims.TenantId),
            phone = user.FindFirstValue(PlatformClaims.Phone),
            role = user.FindFirstValue(PlatformClaims.Role),
            modules = user.FindAll(PlatformClaims.Modules).Select(c => c.Value).ToArray(),
        })).RequireAuthorization();

        return app;
    }
}
