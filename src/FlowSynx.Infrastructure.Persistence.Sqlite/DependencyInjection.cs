using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Infrastructure.Persistence.Abstractions;
using FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;
using FlowSynx.Infrastructure.Persistence.Sqlite.Services;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.Persistence.Sqlite.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Persistence.Sqlite;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlitePersistenceLayer(
        this IServiceCollection services, DatabaseConnection databaseConnection)
    {
        services
            .AddScoped<IAuditTrailRepository, AuditTrailRepository>()
            .AddScoped<IChromosomeRepository, ChromosomeRepository>()
            .AddScoped<IGeneBlueprintRepository, GeneBlueprintRepository>()
            .AddScoped<IGenomeRepository, GenomeRepository>()
            .AddScoped<ITenantRepository, TenantRepository>()
            .AddScoped<IValidateDatabaseConnection, ValidateDatabaseConnection>()
            .AddScoped<IDatabaseInitializer, SqliteDatabaseInitializer>()
            .AddScoped<ITenantSecretConfigRepository, TenantSecretConfigRepository>()
            .AddDbContextFactory<SqliteApplicationContext>(options =>
            {
                options.UseSqlite(databaseConnection.ConnectionString);
            });

        return services;
    }
}