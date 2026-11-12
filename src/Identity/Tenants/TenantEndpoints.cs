using Sme.BuildingBlocks.Tenancy;
using Sme.Identity.Auth;

namespace Sme.Identity.Tenants;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tenants");

        // Registration: a business owner creates their isolated account (UC-006).
        group.MapPost("/", async (RegisterTenantRequest request, TenantRepository tenants, CancellationToken ct) =>
        {
            var name = request.Name?.Trim() ?? "";
            if (name.Length is < 1 or > 120)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Name must be 1 to 120 characters."] });
            }
            var country = (request.Country ?? "JO").Trim().ToUpperInvariant();
            if (country.Length != 2 || !country.All(char.IsAsciiLetterUpper))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["country"] = ["Country must be a two-letter code."] });
            }
            if (!PhoneNumber.TryNormalize(request.OwnerPhone, out var ownerPhone))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["ownerPhone"] = ["Enter the owner's phone in international form, e.g. +962791234567."] });
            }
            var tenant = await tenants.RegisterAsync(name, country, ownerPhone, ct);
            return Results.Created($"/tenants/{tenant.Id}", tenant);
        });

        // Read: only the caller's own tenant is visible; any other id is a 404, never a 403 that leaks existence.
        group.MapGet("/{id:guid}", async (Guid id, ITenantContext tenantContext, TenantRepository tenants, CancellationToken ct) =>
        {
            if (tenantContext.TenantId is null)
            {
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: $"{TenantHeaderMiddleware.HeaderName} header is required.");
            }
            var tenant = await tenants.GetAsync(id, ct);
            return tenant is null ? Results.NotFound() : Results.Ok(tenant);
        });

        return app;
    }
}
