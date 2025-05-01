using FlowSynx.Extensions;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Application.Extensions;
using FlowSynx.Persistence.SQLite.Extensions;
using FlowSynx.Persistence.Postgres.Extensions;
using FlowSynx.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

try
{
    var customConfigPath = builder.Configuration["config"];
    if (!string.IsNullOrEmpty(customConfigPath))
    {
        builder.Configuration.Sources.Clear(); // Optional: clear defaults
        builder.Configuration.AddJsonFile(customConfigPath, optional: false, reloadOnChange: false);
    }

    builder.Configuration.AddEnvironmentVariables();
    IConfiguration config = builder.Configuration;

    builder.Services
           .AddCancellationTokenSource()
           .AddHttpContextAccessor()
           .AddSQLiteLoggerLayer()
           .AddLoggingService(config)
           .AddEndpointsApiExplorer()
           .AddHttpClient()
           .AddHttpJsonOptions()
           .AddJsonSerialization()
           .AddPostgresPersistenceLayer(config)
           .AddEndpoint(config)
           .AddPluginsPath()
           .AddVersion()
           .AddCore()
           .AddInfrastructure()
           .AddInfrastructurePluginManager(config)
           .AddUserService();

    //builder.Services.ParseArguments(args);

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
       .EnsureApplicationDatabaseCreated()
       .UseApplicationDataSeeder()
       .UseHealthCheck();

    app.MapEndpoints();

    await app.RunAsync();
}
catch (Exception ex)
{
    using var scope = builder.Services.BuildServiceProvider().CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    if (logger != null)
        logger.LogError(ex.Message);
    else
        Console.Error.WriteLine(ex.Message);

    // If the console closes immediately, the output may not be visible.
    // So, added await Task.Delay(500) here;
    await Task.Delay(500);
}