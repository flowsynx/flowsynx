﻿using MediatR;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FlowSynx.Configuration;
using FlowSynx.Core.Behaviors;
using FlowSynx.Core.Parers.Namespace;
using FlowSynx.Core.Parers.Specifications;
using FlowSynx.Environment;
using FlowSynx.IO;
using FlowSynx.Parsers;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Extensions;
using FlowSynx.Connectors.Storage.Amazon.S3;
using FlowSynx.Connectors.Storage.LocalFileSystem;
using FlowSynx.Core.Parers.Contex;
using FlowSynx.Connectors.Storage.Azure.Blobs;
using FlowSynx.Connectors.Storage.Azure.Files;
using FlowSynx.Connectors.Storage.Google.Cloud;
using FlowSynx.Connectors.Storage.Google.Drive;
using FlowSynx.Connectors.Storage.Memory;
using FlowSynx.Connectors.Stream.Csv;
using FlowSynx.Data.Extensions;
using FlowSynx.Connectors.Stream.Json;

namespace FlowSynx.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowSynxCore(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(config => {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        services
            .AddEnvironmentManager()
            .AddEndpoint()
            .AddOperatingSystemInfo()
            .AddSystemClock()
            .AddSerialization()
            .AddFileSystem()
            .AddParsers()
            .AddCompressions()
            .AddDatFilter()
            .AddCache<string, Connector>()
            .AddScoped<IContextParser, ContextParser>()
            .AddScoped<INamespaceParser, NamespaceParser>()
            .AddScoped<ISpecificationsParser, SpecificationsParser>();

        return services;
    }
    
    public static IServiceCollection AddFlowSynxConfiguration(this IServiceCollection services, string configurationPath)
    {
        services
            .AddSingleton(new ConfigurationPath { Path = configurationPath })
            .AddConfiguration();
        return services;
    }
    
    public static IServiceCollection AddFlowSynxConnectors(this IServiceCollection services)
    {
        services
            .RegisterConnectors()
            .AddConnectorsManager();
        return services;
    }

    public static IServiceCollection RegisterConnectors(this IServiceCollection services)
    {
        services.AddScoped<Connector, LocalFileSystemStorage>();
        services.AddScoped<Connector, MemoryStorage>();
        services.AddScoped<Connector, AzureFileStorage>();
        services.AddScoped<Connector, AzureBlobStorage>();
        services.AddScoped<Connector, AmazonS3Storage>();
        services.AddScoped<Connector, GoogleCloudStorage>();
        services.AddScoped<Connector, GoogleDriveStorage>();
        services.AddScoped<Connector, CsvStream>();
        services.AddScoped<Connector, JsonStream>();
        return services;
    }
}