using FlowSynx.Application.Configuration.Database;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Audit;
using FlowSynx.Domain.Plugin;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.Persistence.Core.Sqlite.Contexts;
using FlowSynx.Persistence.Core.Sqlite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Persistence.Core.Sqlite.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlitePersistenceLayer(
        this IServiceCollection services, DatabaseConnection databaseConnection)
    {
        services
            .AddScoped<IAuditService, AuditService>()
            .AddScoped<IDatabaseInitializer, PosgreSqlDatabaseInitializer>()
            .AddScoped<IPluginConfigurationService, PluginConfigurationService>()
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

        return services;
    }

    public static IServiceCollection AddSqliteDurableWorkflowQueueService(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowExecutionQueue, WorkflowExecutionQueueServcie>();
        return services;
    }
}