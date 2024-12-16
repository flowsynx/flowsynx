using FlowSynx.Commands;
using FlowSynx.Core.Extensions;
using FlowSynx.Environment;
using FlowSynx.Extensions;
using FlowSynx.Services;

namespace FlowSynx.ApplicationBuilders;

public class ApiApplicationBuilder : IApiApplicationBuilder
{
    public async Task RunAsync(RootCommandOptions rootCommandOptions)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigHttpServer(EnvironmentVariables.FlowSynxHttpPort);

        builder.Services
               .AddEndpointsApiExplorer()
               .AddHttpJsonOptions()
               .AddLoggingService(rootCommandOptions.EnableLog, rootCommandOptions.LogLevel, rootCommandOptions.LogFile)
               .AddLocation()
               .AddVersion()
               .AddFlowSynxCore()
               .AddFlowSynxConnectors()
               .AddFlowSynxConfiguration(rootCommandOptions.ConfigFile);

        builder.Services.AddSingleton<IJobQueue, JobQueue>();
        builder.Services.AddHostedService<JobQueueService>();

        if (rootCommandOptions.EnableHealthCheck)
            builder.Services.AddHealthChecker();

        if (rootCommandOptions.OpenApi)
            builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        if (rootCommandOptions.OpenApi)
            app.UseOpenApi();
        
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