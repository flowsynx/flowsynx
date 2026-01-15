using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
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
            .AddScoped<IGeneRepository, GeneRepository>()
            .AddScoped<IGenomeRepository, GenomeRepository>()
            .AddScoped<ITenantRepository, TenantRepository>()
            .AddScoped<IExecutionRepository, ExecutionRepository>()
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