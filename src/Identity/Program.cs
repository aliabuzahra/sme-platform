using Sme.BuildingBlocks.Auth;
using Sme.BuildingBlocks.Data;
using Sme.BuildingBlocks.Tenancy;
using Sme.Identity.Auth;
using Sme.Identity.Branches;
using Sme.Identity.Tenants;

var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Configuration["Service:Name"]
    ?? throw new InvalidOperationException("Service:Name is not configured.");

builder.Services.AddServiceDatabase(builder.Configuration, schema: "identity", typeof(Program).Assembly);
builder.Services.AddTenantContext();
builder.Services.AddPlatformJwt(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TenantRepository>();
builder.Services.AddScoped<BranchRepository>();

// Sign-in (UC-007). Only the fake sender exists until Notifications delivers SMS (UC-012).
var otpOptions = builder.Configuration.GetSection(OtpOptions.SectionName).Get<OtpOptions>() ?? new OtpOptions();
if (otpOptions.Sender != "fake")
{
    throw new InvalidOperationException($"Otp:Sender '{otpOptions.Sender}' is not available yet; only 'fake' is.");
}
builder.Services.AddSingleton(otpOptions);
builder.Services.AddSingleton<FakeOtpSender>();
builder.Services.AddSingleton<IOtpSender>(sp => sp.GetRequiredService<FakeOtpSender>());
builder.Services.AddSingleton<TokenIssuer>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<UserRepository>();

var app = builder.Build();

// Schema changes ship with the service and run before it takes traffic.
await app.Services.GetRequiredService<SqlMigrator>().MigrateAsync();

app.UseAuthentication();
app.UseTenantFromToken();
app.UseTenantHeader(allow: app.Configuration.GetValue<bool>(TenantHeaderMiddleware.AllowHeaderSetting));
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = serviceName,
}));

app.MapTenantEndpoints();
app.MapBranchEndpoints();
app.MapAuthEndpoints();

app.Run();

// Lets the integration tests host the service with WebApplicationFactory<Program>.
public partial class Program;
