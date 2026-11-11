var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Configuration["Service:Name"]
    ?? throw new InvalidOperationException("Service:Name is not configured.");

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = serviceName,
}));

app.Run();
