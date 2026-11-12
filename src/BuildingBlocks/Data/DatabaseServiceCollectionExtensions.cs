using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Sme.BuildingBlocks.Data;

public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers the data source, the tenant-aware connection factory and the migrator for a
    /// service that owns <paramref name="schema"/>. Reads the <c>Database</c> section.
    /// </summary>
    public static IServiceCollection AddServiceDatabase(this IServiceCollection services, IConfiguration configuration, string schema, Assembly migrationsAssembly)
    {
        var options = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        options.Schema = schema;
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException("Database:ConnectionString is not configured.");
        }

        services.AddSingleton(options);
        services.AddSingleton(_ => NpgsqlDataSource.Create(options.ConnectionString));
        services.AddSingleton(sp => new SqlMigrator(sp.GetRequiredService<NpgsqlDataSource>(), schema, migrationsAssembly));
        services.AddScoped<TenantConnectionFactory>();
        return services;
    }
}
