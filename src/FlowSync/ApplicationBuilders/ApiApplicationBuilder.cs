using FlowSync.Commands;
using FlowSync.Core.Extensions;
using FlowSync.Endpoints.List;
using FlowSync.Extensions;
using FlowSync.Infrastructure.Extensions;
using FlowSync.Persistence.Json.Extensions;
using FluentValidation;
using IValidator = FlowSync.Core.Services.IValidator;

namespace FlowSync.ApplicationBuilders;

public class ApiApplicationBuilder : IApiApplicationBuilder
{
    public async Task RunAsync(CommandOptions commandOptions)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigHttpServer(commandOptions.Port);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddLoggingService(commandOptions.EnableLog, commandOptions.AppLogLevel);
        builder.Services.AddLocation();
        builder.Services.AddFlowSyncApplication();
        builder.Services.AddFlowSyncInfrastructure();
        builder.Services.AddFlowSyncPersistence(commandOptions.Config);
        builder.Services.AddValidatorsFromAssemblyContaining<IValidator>();
        builder.Services.AddHealthChecker();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseHealthCheck();

        app.UseCustomException();
        app.UseCustomHeaders();
        app.MapList();

        await app.RunAsync();
    }
}