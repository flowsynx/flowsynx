using FlowSynx.Application.Core.Services;
using FlowSynx.Infrastructure.Runtime.Expression;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Runtime;

public static class DependencyInjection
{
    public static IServiceCollection AddRuntimeServices(this IServiceCollection services)
    {
        services.AddScoped<IJsonProcessingService, JsonProcessingService>();
        services.AddScoped<IGeneExecutorFactory, GeneExecutorFactory>();
        services.AddScoped<IGenomeExecutionService, GenomeExecutionService>();
        services.AddScoped<IGenomeManagementService, GenomeManagementService>();
        return services;
    }
}