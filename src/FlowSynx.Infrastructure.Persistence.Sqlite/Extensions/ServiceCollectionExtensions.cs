using FlowSynx.Application;
using FlowSynx.Infrastructure.Configuration.Core.Database;
using FlowSynx.Infrastructure.Persistence;
using FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;
using FlowSynx.Infrastructure.Persistence.Sqlite.Services;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.Persistence.Sqlite.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Persistence.Sqlite.Extensions;

public static class ServiceCollectionExtensions
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
            .AddScoped<IDatabaseInitializer, SqliteDatabaseInitializer>()
            .AddDbContextFactory<SqliteApplicationContext>(options =>
            {
                options.UseSqlite(databaseConnection.ConnectionString);
            });

        //// Ensure database is created
        //using (var scope = services.BuildServiceProvider().CreateScope())
        //{
        //    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SqliteApplicationContext>>();
        //    using var context = dbFactory.CreateDbContext();
        //    context.Database.EnsureCreated();
        //}

        return services;
    }
}