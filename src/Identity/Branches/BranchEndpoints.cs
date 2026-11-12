using Npgsql;
using Sme.BuildingBlocks.Tenancy;

namespace Sme.Identity.Branches;

public static class BranchEndpoints
{
    public static IEndpointRouteBuilder MapBranchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/branches").AddEndpointFilter(RequireTenant);

        // Owner adds a location with its own hours (UC-008).
        group.MapPost("/", async (CreateBranchRequest request, BranchRepository branches, CancellationToken ct) =>
        {
            var errors = OpeningHours.Validate(request.Hours);
            var name = request.Name?.Trim() ?? "";
            if (name.Length is < 1 or > 120) errors["name"] = ["Name must be 1 to 120 characters."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            try
            {
                var branch = await branches.CreateAsync(name, OpeningHours.Normalize(request.Hours), ct);
                return Results.Created($"/branches/{branch.Id}", branch);
            }
            catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return Results.Conflict(new { error = $"A branch named '{name}' already exists." });
            }
        });

        group.MapGet("/", async (BranchRepository branches, CancellationToken ct) => Results.Ok(await branches.ListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, BranchRepository branches, CancellationToken ct) =>
            await branches.GetAsync(id, ct) is { } branch ? Results.Ok(branch) : Results.NotFound());

        return app;
    }

    private static async ValueTask<object?> RequireTenant(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var tenant = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
        if (tenant.TenantId is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: $"{TenantHeaderMiddleware.HeaderName} header is required.");
        }
        return await next(context);
    }
}
