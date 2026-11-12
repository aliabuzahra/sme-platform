namespace Sme.Identity.Tenants;

public sealed record RegisterTenantRequest(string? Name);

public static class TenantEndpoints
{
    /// <summary>
    /// Carries the caller's tenant until sign-in exists. UC-007 replaces it with the tenant claim
    /// of a verified token, and UC-011 makes the Gateway the only caller. It is not authentication.
    /// </summary>
    public const string TenantHeader = "X-Tenant-Id";

    public const int NameMinLength = 2;
    public const int NameMaxLength = 120;

    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tenants", async (RegisterTenantRequest request, TenantStore store, CancellationToken cancellationToken) =>
        {
            var name = request.Name?.Trim() ?? string.Empty;
            // Counted the way Postgres char_length counts, so the API and the table agree.
            var length = name.EnumerateRunes().Count();
            if (length < NameMinLength || length > NameMaxLength)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = [$"Name must be {NameMinLength} to {NameMaxLength} characters."],
                });
            }

            var tenant = await store.CreateAsync(name, cancellationToken);
            return Results.Created($"/tenants/{tenant.Id}", tenant);
        });

        app.MapGet("/tenants/{id:guid}", async (Guid id, HttpRequest http, TenantStore store, CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(http.Headers[TenantHeader].ToString(), out var callerTenantId))
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: $"The {TenantHeader} header is required.");
            }

            var tenant = await store.FindAsync(callerTenantId, id, cancellationToken);
            return tenant is null ? Results.NotFound() : Results.Ok(tenant);
        });

        return app;
    }
}
