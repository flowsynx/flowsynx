using FlowSynx.Extensions;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Application.Extensions;
using FlowSynx.Persistence.SQLite.Extensions;
using FlowSynx.Persistence.Postgres.Extensions;
using FlowSynx.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
IConfiguration config = builder.Configuration;

builder.Services
       .AddCancellationTokenSource()
       .AddHttpContextAccessor()
       .AddEndpointsApiExplorer()
       .AddHttpClient()
       .AddHttpJsonOptions()
       .AddJsonSerialization()
       .AddSQLiteLoggerLayer()
       .AddPostgresPersistenceLayer(config)
       .AddSQLiteLoggerLayer()
       .AddLoggingService(config)
       .AddEndpoint(config)
       .AddLocation()
       .AddVersion()
       .AddCore()
       .AddInfrastructure()
       .AddFlowSynxPlugins()
       .AddUserService();

builder.Services.ParseArguments(args);

builder.Services
       .AddSecurity(config)
       .AddHealthChecker(config)
       .AddOpenApi(config)
       .AddHostedService<TriggerProcessingService>();

builder.ConfigHttpServer();

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
   .EnsureLogDatabaseCreated()
   .EnsureApplicationDatabaseCreated()
   .UseApplicationDataSeeder()
   .UseHealthCheck();

app.MapEndpoints();

await app.RunAsync();