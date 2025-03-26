using ConnectivityTestingLab.Application.Configurations;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Persistence.Postgres.Seeder;
using FlowSynx.Persistence.Postgres.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Persistence.Postgres.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresPersistenceLayer(this IServiceCollection services, 
        IConfiguration configuration)
    {
        var databaseConfiguration = new DatabaseConfiguration();
        configuration.GetSection("Db").Bind(databaseConfiguration);
        services.AddSingleton(databaseConfiguration);

        var connectionString = $"Host={databaseConfiguration.Host};Port={databaseConfiguration.Port};Database={databaseConfiguration.Name};Username={databaseConfiguration.UserName};Password={databaseConfiguration.Password};";

        services
            .AddScoped<IAuditService, AuditService>()
            .AddScoped<IApplicationDataSeeder, ApplicationDataSeeder>()
            .AddScoped<IPluginConfigurationService, PluginConfigurationService>()
            .AddScoped<IWorkflowService, WorkflowService>()
            .AddScoped<IWorkflowExecutionService, WorkflowExecutionService>()
            .AddScoped<IWorkflowTaskExecutionService, WorkflowTaskExecutionService>()
            .AddScoped<IWorkflowTriggerService, WorkflowTriggerService>()
            .AddScoped<ITransactionService, TransactionService>()
            .AddDbContextFactory<ApplicationContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });
        return services;
    }
}