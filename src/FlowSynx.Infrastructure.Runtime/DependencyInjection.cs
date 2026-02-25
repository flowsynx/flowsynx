using FlowSynx.Application.Core.Services;
using FlowSynx.Infrastructure.Runtime.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Runtime;

public static class DependencyInjection
{
    public static IServiceCollection AddRuntimeServices(this IServiceCollection services)
    {
        services.AddScoped<IJsonProcessingService, JsonProcessingService>();
        services.AddScoped<IActivityExecutorFactory, ActivityExecutorFactory>();
        services.AddScoped<IActivityCompatibilityService, ActivityCompatibilityService>();
        services.AddSingleton<IRuntimeEnvironmentProvider, RuntimeEnvironmentProvider>();
        services.AddScoped<IWorkflowApplicationExecutionService, WorkflowApplicationExecutionService>();
        services.AddScoped<IWorkflowApplicationManagementService, WorkflowApplicationManagementService>();
        services.AddSingleton<ICircuitBreakerManager, InMemoryCircuitBreakerManager>();
        return services;
    }
}