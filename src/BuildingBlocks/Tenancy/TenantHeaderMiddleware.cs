using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Sme.BuildingBlocks.Tenancy;

/// <summary>
/// Reads the tenant for the request from the <c>X-Tenant-Id</c> header.
/// </summary>
/// <remarks>
/// A stand-in until UC-007 puts the tenant in the sign-in token and UC-011 makes the Gateway
/// the only caller. Services listen on localhost only (ADR-0006), so until then the header is
/// no weaker than the network they sit on. Remove it in UC-011.
/// </remarks>
public static class TenantHeaderMiddleware
{
    public const string HeaderName = "X-Tenant-Id";

    public static IServiceCollection AddTenantContext(this IServiceCollection services)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        return services;
    }

    public static IApplicationBuilder UseTenantHeader(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var raw))
            {
                if (!Guid.TryParse(raw.ToString(), out var tenantId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = $"{HeaderName} must be a UUID." });
                    return;
                }
                context.RequestServices.GetRequiredService<TenantContext>().Set(tenantId);
            }
            await next(context);
        });
}
