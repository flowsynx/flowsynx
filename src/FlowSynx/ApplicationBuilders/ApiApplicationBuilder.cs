using FlowSynx.Application.Extensions;
using FlowSynx.Extensions;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Persistence.Postgres.Extensions;
using FlowSynx.Persistence.SQLite.Extensions;
using FlowSynx.Services;
using Microsoft.Extensions.Hosting;

namespace FlowSynx.ApplicationBuilders;

public class ApiApplicationBuilder : IApiApplicationBuilder
{
    public async Task RunAsync(ILogger logger, int port, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        IConfiguration config = builder.Configuration;

        builder.WebHost.ConfigHttpServer(port);

        builder.Services
               .AddHttpContextAccessor()
               .AddEndpointsApiExplorer()
               .AddHttpJsonOptions()
               .AddJsonSerialization()
               .AddPostgresPersistenceLayer(config)
               .AddSQLiteLoggerLayer()
               .AddLocation()
               .AddVersion()
               .AddCore()
               .AddInfrastructure()
               .AddFlowSynxPlugins()
               .AddUserService()
               .AddSecurity(config, logger)
               .AddLoggingService(config, cancellationToken)
               .AddHttpClient()
               .AddHealthChecker(config)
               .AddOpenApi(config)
               .AddHostedService<TriggerProcessingService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseOpenApi()
           .UseCustomHeaders()
           .UseExceptionHandler(exceptionHandlerApp => 
                                exceptionHandlerApp.Run(async context => 
                                    await Results.Problem().ExecuteAsync(context)))
           .UseCustomException()
           .UseRouting()
           .UseAuthentication()
           .UseAuthorization()
           .EnsureApplicationDatabaseCreated(logger)
           .UseApplicationDataSeeder()
           .UseHealthCheck();

        app.MapEndpoints();

        await app.RunAsync(cancellationToken);
    }
}