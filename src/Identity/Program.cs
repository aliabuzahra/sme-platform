using Sme.BuildingBlocks.Data;
using Sme.BuildingBlocks.Tenancy;
using Sme.Identity.Branches;
using Sme.Identity.Tenants;

var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Configuration["Service:Name"]
    ?? throw new InvalidOperationException("Service:Name is not configured.");

builder.Services.AddServiceDatabase(builder.Configuration, schema: "identity", typeof(Program).Assembly);
builder.Services.AddTenantContext();
builder.Services.AddScoped<TenantRepository>();
builder.Services.AddScoped<BranchRepository>();

var app = builder.Build();

// Schema changes ship with the service and run before it takes traffic.
await app.Services.GetRequiredService<SqlMigrator>().MigrateAsync();

app.UseTenantHeader();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = serviceName,
}));

app.MapTenantEndpoints();
app.MapBranchEndpoints();

app.Run();

// Lets the integration tests host the service with WebApplicationFactory<Program>.
public partial class Program;
