using FlowSynx.Application.Configuration;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Audit;
using FlowSynx.Domain.Plugin;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.Infrastructure.Workflow;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Persistence.Postgres.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Persistence.Postgres.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresPersistenceLayer(
        this IServiceCollection services, IConfiguration configuration)
    {
        var databaseConfiguration = new DatabaseConfiguration();
        configuration.GetSection("Db").Bind(databaseConfiguration);
        services.AddSingleton(databaseConfiguration);

        var connectionString = GetConnectionString(databaseConfiguration);

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
                options.UseNpgsql(connectionString);
            });

        return services;
    }

    public static IServiceCollection AddDurableWorkflowQueueService(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowExecutionQueue, WorkflowExecutionQueueServcie>();
        return services;
    }

    private static string GetConnectionString(DatabaseConfiguration config)
    {
        if (!string.IsNullOrWhiteSpace(config.ConnectionString))
            return config.ConnectionString;

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = config.Host,
            Port = config.Port ?? 5432,
            Database = config.Name,
            Username = config.UserName,
            Password = config.Password
        };

        if (config.AdditionalOptions != null)
        {
            foreach (var kv in config.AdditionalOptions)
            {
                builder[kv.Key] = kv.Value;
            }
        }

        return builder.ToString();
    }
}