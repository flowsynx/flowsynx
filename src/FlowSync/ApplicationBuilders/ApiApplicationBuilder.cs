using FlowSync.Commands;
using FlowSync.Core.Common.Services;
using FlowSync.Core.Extensions;
using FlowSync.Extensions;
using FlowSync.Infrastructure.Extensions;

namespace FlowSync.ApplicationBuilders;

public class ApiApplicationBuilder : IApiApplicationBuilder
{
    private readonly IDefaultEndpoint _defaultEndpoint;

    public ApiApplicationBuilder(IDefaultEndpoint defaultEndpoint)
    {
        _defaultEndpoint = defaultEndpoint;
    }

    public async Task RunAsync(RootCommandOptions rootCommandOptions)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigHttpServer(_defaultEndpoint.GetDefaultHttpEndpoint());

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddLoggingService(rootCommandOptions.EnableLog, rootCommandOptions.AppLogLevel);
        builder.Services.AddLocation();
        builder.Services.AddVersion();
        builder.Services.AddFlowSyncApplication();
        builder.Services.AddFlowSyncInfrastructure();
        builder.Services.AddFlowSyncPluginsManager();
        builder.Services.AddFlowSyncConfigurationManager(rootCommandOptions.Config);
        
        if (rootCommandOptions.EnableHealthCheck)
            builder.Services.AddHealthChecker();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCustomHeaders();

        app.UseExceptionHandler(exceptionHandlerApp
            => exceptionHandlerApp.Run(async context
                => await Results.Problem().ExecuteAsync(context)));

        app.UseCustomException();

        app.UseRouting();

        if (rootCommandOptions.EnableHealthCheck)
            app.UseHealthCheck();

        app.MapEndpoints();

        await app.RunAsync();
    }
}