using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
using FlowSync.Core.Common.Behaviors;
using MediatR;
using FlowSync.Abstractions.Storage;
using FlowSync.Core.Storage;
using FlowSync.Core.Storage.Filter;
using FlowSync.Core.Parers.Date;
using FlowSync.Core.Parers.Size;
using FlowSync.Core.Parers.Sort;

namespace FlowSync.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowSyncApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(config => {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        services.AddScoped<IStorageFilter, StorageFilter>();
        services.AddScoped<IStorageService, StorageService>();

        return services;
    }
}