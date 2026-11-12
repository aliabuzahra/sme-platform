using Npgsql;
using Sme.Identity.Data;
using Sme.Identity.Tenants;

var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Configuration["Service:Name"]
    ?? throw new InvalidOperationException("Service:Name is not configured.");

// The connection is opened on first use, so /health answers even when Postgres is not configured.
builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.")));
builder.Services.AddSingleton<TenantStore>();

var app = builder.Build();

// Local development and tests create the identity schema themselves. Deployed environments run
// migrations as a separate step with an admin connection (UC-013), so this stays off there.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await IdentityDatabase.MigrateAsync(app.Services.GetRequiredService<NpgsqlDataSource>());
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = serviceName,
}));

app.MapTenantEndpoints();

app.Run();

// Lets the test project start the service in memory.
public partial class Program
{
}
