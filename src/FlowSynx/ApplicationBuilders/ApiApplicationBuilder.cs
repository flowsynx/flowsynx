using FlowSynx.Commands;
using FlowSynx.Core.Extensions;
using FlowSynx.Environment;
using FlowSynx.Extensions;

namespace FlowSynx.ApplicationBuilders;

public class ApiApplicationBuilder : IApiApplicationBuilder
{
    public async Task RunAsync(RootCommandOptions rootCommandOptions)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigHttpServer(EnvironmentVariables.FlowSynxHttpPort);

        builder.Services
               .AddEndpointsApiExplorer()
               .AddLoggingService(rootCommandOptions.EnableLog, rootCommandOptions.LogLevel, rootCommandOptions.LogFile)
               .AddLocation()
               .AddVersion()
               .AddFlowSynxCore()
               .AddFlowSynxPlugins()
               .AddFlowSynxConfiguration(rootCommandOptions.ConfigFile);
        
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