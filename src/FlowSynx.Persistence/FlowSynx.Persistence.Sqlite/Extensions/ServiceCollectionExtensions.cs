using FlowSynx.Application.Configuration.Core.Database;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Audit;
using FlowSynx.Domain.Plugin;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.Persistence.Sqlite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Persistence.Sqlite.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlitePersistenceLayer(
        this IServiceCollection services, DatabaseConnection databaseConnection)
    {
        services
            .AddScoped<IAuditService, AuditService>()
            .AddScoped<IDatabaseInitializer, PosgreSqlDatabaseInitializer>()
            .AddScoped<IPluginService, PluginService>()
            .AddScoped<IWorkflowService, WorkflowService>()
            .AddScoped<IWorkflowExecutionService, WorkflowExecutionService>()
            .AddScoped<IWorkflowTaskExecutionService, WorkflowTaskExecutionService>()
            .AddScoped<IWorkflowTriggerService, WorkflowTriggerService>()
            .AddScoped<IWorkflowApprovalService, WorkflowApprovalService>()
            .AddDbContextFactory<ApplicationContext>(options =>
            {
                options.UseSqlite(databaseConnection.ConnectionString);
            });

        // Ensure database is created
        using (var scope = services.BuildServiceProvider().CreateScope())
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationContext>>();
            using var context = dbFactory.CreateDbContext();
            context.Database.EnsureCreated();
        }

        return services;
    }

    public static IServiceCollection AddSqliteDurableWorkflowQueueService(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowExecutionQueue, WorkflowExecutionQueueServcie>();
        return services;
    }
}