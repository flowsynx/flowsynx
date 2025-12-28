using FlowSynx.Application.Configuration.Core.Database;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Plugin;
using FlowSynx.Domain.Repositories;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Persistence.Postgres.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Persistence.Postgres.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresPersistenceLayer(
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
                options.UseNpgsql(databaseConnection.ConnectionString);
            });

        using (var scope = services.BuildServiceProvider().CreateScope())
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationContext>>();
            using var context = dbFactory.CreateDbContext();
            context.Database.EnsureCreated();
        }

        return services;
    }

    public static IServiceCollection AddPostgreDurableWorkflowQueueService(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowExecutionQueue, WorkflowExecutionQueueServcie>();
        return services;
    }
}