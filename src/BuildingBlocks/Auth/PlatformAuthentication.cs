using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sme.BuildingBlocks.Tenancy;

namespace Sme.BuildingBlocks.Auth;

public static class PlatformAuthentication
{
    /// <summary>
    /// Validates platform bearer tokens (issuer, audience, lifetime, signature) and exposes
    /// <see cref="JwtOptions"/> for services that also issue them.
    /// </summary>
    public static IServiceCollection AddPlatformJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 bytes.");
        }
        services.AddSingleton(options);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = PlatformClaims.Role,
                };
            });
        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// After authentication: a valid token's <c>tenant_id</c> claim becomes the request's tenant.
    /// A token always wins over the <c>X-Tenant-Id</c> stand-in header.
    /// </summary>
    public static IApplicationBuilder UseTenantFromToken(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var claim = context.User.FindFirst(PlatformClaims.TenantId)?.Value;
            if (claim is not null)
            {
                if (!Guid.TryParse(claim, out var tenantId))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
                context.RequestServices.GetRequiredService<TenantContext>().Set(tenantId);
            }
            await next(context);
        });
}
