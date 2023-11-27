using Microsoft.Extensions.DependencyInjection;
using FlowSync.Core.FileSystem;
using FlowSync.Core.FileSystem.Filter;
using System.Reflection;
using FlowSync.Abstractions.Filter;
using FlowSync.Abstractions.Parers.Date;
using FlowSync.Abstractions.Parers.Size;
using FlowSync.Abstractions.Parers.Sort;
using FluentValidation;
using FlowSync.Core.Common.Behaviors;
using MediatR;
using FlowSync.Core.FileSystem.Parers.Date;
using FlowSync.Core.FileSystem.Parers.RemotePath;
using FlowSync.Core.FileSystem.Parers.Size;
using FlowSync.Core.FileSystem.Parers.Sort;

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

        services.AddScoped<IRemotePathParser, RemotePathParser>();
        services.AddScoped<IDateParser, DateParser>();
        services.AddScoped<ISizeParser, SizeParser>();
        services.AddScoped<ISortParser, SortParser>();
        services.AddScoped<IFileSystemFilter, FileSystemFilter>();
        services.AddScoped<IFileSystemService, FileSystemService>();

        return services;
    }
}