using FlowSynx.Application.Configuration;
using FlowSynx.Application.Extensions;
using FlowSynx.Extensions;
using FlowSynx.Hubs;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.Configuration;
using FlowSynx.Infrastructure.Workflow.Triggers.HttpBased;
using FlowSynx.Persistence.Postgres.Extensions;
using FlowSynx.Persistence.SQLite.Extensions;
using FlowSynx.Services;
using Microsoft.Extensions.Logging;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

try
{
    if (args.HandleVersionFlag())
        return;

    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables();

    var customConfigPath = builder.Configuration["config"];
    if (!string.IsNullOrEmpty(customConfigPath))
    {
        builder.Configuration.Sources.Clear(); // Optional: clear defaults
        builder.Configuration.AddJsonFile(customConfigPath, optional: false, reloadOnChange: false);
    }

    var requestedConfigurationSource = ConfigurationSourceSelector.Resolve(args, builder.Configuration);
    var activeConfigurationSource = requestedConfigurationSource;
    var infisicalFallbackUsed = false;

    if (requestedConfigurationSource == ConfigurationSourceOption.Infisical)
    {
        var infisicalConfiguration = new InfisicalConfiguration();
        builder.Configuration.GetSection("Infisical").Bind(infisicalConfiguration);

        if (!infisicalConfiguration.Enabled)
        {
            activeConfigurationSource = ConfigurationSourceOption.AppSettings;
        }
        else
        {
            try
            {
                builder.Configuration.AddInfisical(infisicalConfiguration);
            }
            catch (Exception ex) when (infisicalConfiguration.FallbackToAppSettings)
            {
                activeConfigurationSource = ConfigurationSourceOption.AppSettings;
                infisicalFallbackUsed = true;
                Console.Error.WriteLine("Warning: Failed to load configuration from Infisical. Falling back to appsettings.json.");
                Console.Error.WriteLine($"Reason: {ex.GetType().Name}");
            }
        }
    }

    builder.Configuration[ConfigurationSourceSelector.ActiveSourceConfigurationKey] = activeConfigurationSource.ToString();
    builder.Configuration[ConfigurationSourceSelector.InfisicalFallbackKey] = infisicalFallbackUsed.ToString();

    IConfiguration config = builder.Configuration;

    builder.Services
           .AddCancellationTokenSource()
           .AddHttpContextAccessor()
           .AddJsonSerialization()
           .AddSqLiteLoggerLayer()
           .AddLoggingService(config)
           .AddJsonLocalization(config)
           .AddEndpointsApiExplorer()
           .AddHttpClient()
           .AddHttpJsonOptions()
           .AddPostgresPersistenceLayer(config)
           .AddWorkflowQueueService(config)
           .AddEndpoint(config)
           .AddPluginsPath()
           .AddVersion()
           .AddApplication()
           .AddEncryptionService(config)
           .AddInfrastructure()
           .AddInfrastructurePluginManager(config)
           .AddUserService()
           .AddRateLimiting(config)
           .AddResultStorageService(config)
           .AddEventPublisher();

    if (!builder.Environment.IsDevelopment())
        builder.Services.ParseArguments(args);

    builder.Services
           .AddSecurity(config)
           .AddHealthChecker(config)
           .AddOpenApi(config)
           .AddHostedService<WorkflowExecutionWorker>()
           .AddHostedService<TriggerProcessingService>();

    builder.ConfigureHttpServer();
    builder.Services.AddConfiguredCors(config);

    var app = builder.Build();

    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    var activeSource = app.Configuration[ConfigurationSourceSelector.ActiveSourceConfigurationKey]
                       ?? ConfigurationSourceOption.AppSettings.ToString();
    startupLogger.LogInformation("Configuration source in use: {Source}", activeSource);

    if (bool.TryParse(app.Configuration[ConfigurationSourceSelector.InfisicalFallbackKey], out var fallback) && fallback)
    {
        startupLogger.LogWarning("Infisical configuration was requested but appsettings.json was used as a fallback.");
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
            exceptionHandlerApp.Run(async context =>
                await Results.Problem().ExecuteAsync(context)));
    }

    app.UseHttps();
    app.UseCustomHeaders();
    app.UseConfiguredCors();
    app.UseRateLimiter();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseOpenApi();
    app.UseCustomException();

    app.EnsureApplicationDatabaseCreated();

    app.UseHealthCheck();

    app.MapHub<WorkflowsHub>("/hubs/workflowExecutions");
    app.MapEndpoints("Fixed");

    var listener = app.Services.GetRequiredService<IWorkflowHttpListener>();
    app.MapHttpTriggersWorkflowRoutes(listener);

    await app.RunAsync();
}
catch (Exception ex)
{
    using var scope = builder.Services.BuildServiceProvider().CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    if (logger != null)
        logger.LogError(ex.Message);
    else
        await Console.Error.WriteLineAsync(ex.Message);

    // If the console closes immediately, the output may not be visible.
    // So, added await Task.Delay(500) here;
    await Task.Delay(500);
}
