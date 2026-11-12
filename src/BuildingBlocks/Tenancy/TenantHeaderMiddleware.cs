using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Sme.BuildingBlocks.Tenancy;

/// <summary>
/// Reads the tenant for the request from the <c>X-Tenant-Id</c> header.
/// </summary>
/// <remarks>
/// A development stand-in for the tenant claim in a sign-in token (UC-007). It is only honoured
/// when <c>Tenancy:AllowHeader</c> is true, which <c>appsettings.Development.json</c> sets, and a
/// request that already carries a token keeps the token's tenant. UC-011 removes it once the
/// Gateway is the only caller.
/// </remarks>
public static class TenantHeaderMiddleware
{
    public const string HeaderName = "X-Tenant-Id";
    public const string AllowHeaderSetting = "Tenancy:AllowHeader";

    public static IServiceCollection AddTenantContext(this IServiceCollection services)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        return services;
    }

    public static IApplicationBuilder UseTenantHeader(this IApplicationBuilder app, bool allow) =>
        !allow ? app : app.Use(async (context, next) =>
        {
            var tenant = context.RequestServices.GetRequiredService<TenantContext>();
            if (tenant.TenantId is null && context.Request.Headers.TryGetValue(HeaderName, out var raw))
            {
                if (!Guid.TryParse(raw.ToString(), out var tenantId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = $"{HeaderName} must be a UUID." });
                    return;
                }
                tenant.Set(tenantId);
            }
            await next(context);
        });
}
