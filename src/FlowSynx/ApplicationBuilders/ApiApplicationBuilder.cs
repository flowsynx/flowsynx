using FlowSynx.Commands;
using FlowSynx.Core.Extensions;
using FlowSynx.Environment;
using FlowSynx.Extensions;

namespace FlowSynx.ApplicationBuilders;

public class ApiApplicationBuilder : IApiApplicationBuilder
{
    private readonly IEndpoint _endpoint;

    public ApiApplicationBuilder(IEndpoint endpoint)
    {
        _endpoint = endpoint;
    }

    public async Task RunAsync(RootCommandOptions rootCommandOptions)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigHttpServer(_endpoint.GetDefaultHttpPort());

        builder.Services
            .AddEndpointsApiExplorer()
            .AddLoggingService(rootCommandOptions.EnableLog, rootCommandOptions.AppLogLevel)
            .AddLocation()
            .AddVersion()
            .AddFlowSynxCore()
            .AddFlowSynxPlugins()
            .AddFlowSynxConfiguration(rootCommandOptions.Config);
        
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