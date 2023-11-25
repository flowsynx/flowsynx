using Microsoft.Extensions.DependencyInjection;
using FlowSync.Core.Services;
using FlowSync.Core.FileSystem;
using FlowSync.Core.FileSystem.Filter;
using FlowSync.Core.FileSystem.Parse.Date;
using FlowSync.Core.FileSystem.Parse.RemotePath;
using FlowSync.Core.FileSystem.Parse.Size;
using FlowSync.Core.FileSystem.Parse.Sort;

namespace FlowSync.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowSyncApplication(this IServiceCollection services)
    {
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(IFeature).Assembly));
        services.AddScoped<IRemotePathParser, RemotePathParser>();
        services.AddScoped<IDateParser, DateParser>();
        services.AddScoped<ISizeParser, SizeParser>();
        services.AddScoped<ISortParser, SortParser>();
        services.AddScoped<IFilter, Filter>();
        services.AddScoped<IFileSystemService, FileSystemService>();
        return services;
    }
}