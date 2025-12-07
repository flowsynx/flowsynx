using FlowSynx.Application.Extensions;
using FlowSynx.Extensions;
using FlowSynx.Hubs;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.Secrets;
using FlowSynx.Infrastructure.Workflow.Triggers.HttpBased;
using FlowSynx.Services;

var builder = WebApplication.CreateBuilder(args);

try
{
    // Handle version flag early exit
    if (args.HandleVersionFlag())
        return;

    FilterLogging(builder);
    ConfigureConfiguration(builder);
    ConfigureServices(builder, args);

    var app = builder.Build();
    ConfigureMiddleware(app);
    ConfigureApplication(app);

    await app.RunAsync();
}
catch (Exception ex)
{
    await HandleStartupExceptionAsync(builder, ex);
}

#region helpers
static void FilterLogging(WebApplicationBuilder builder)
{
    // Clear built-in/default logging providers so only configured providers remain
    builder.Logging.ClearProviders();
    builder.Logging.AddLoggingFilter();
}

static void ConfigureConfiguration(WebApplicationBuilder builder)
{
    var env = builder.Environment;

    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables();

    // Load custom config if specified
    var customConfigPath = builder.Configuration["config"];
    if (!string.IsNullOrWhiteSpace(customConfigPath))
    {
        builder.Configuration.AddJsonFile(customConfigPath, optional: false, reloadOnChange: false);
    }

    builder.Services.AddSecretService(builder.Configuration);

    using var scope = builder.Services.BuildServiceProvider().CreateScope();
    var secretFactory = scope.ServiceProvider.GetRequiredService<ISecretFactory>();
    var secretProvider = secretFactory.GetDefaultProvider();

    builder.Configuration.AddSecrets(secretProvider);
}

static void ConfigureServices(WebApplicationBuilder builder, string[] args)
{
    builder.AddLoggers();

    var services = builder.Services;
    var config = builder.Configuration;
    var env = builder.Environment;

    services
        .AddCancellationTokenSource()
        .AddHttpContextAccessor()
        .AddSystemClock()
        .AddJsonSerialization()
        .AddJsonLocalization(config)
        .AddEndpointsApiExplorer()
        .AddHttpClient()
        .AddHttpJsonOptions()
        .AddSecurity(config)
        .AddPersistence(config)
        .AddWorkflowQueueService(config)
        .AddEnsureWorkflowPluginsService(config)
        .AddServer(config)
        .AddPluginsPath()
        .AddVersion()
        .AddApplication()
        .AddInfrastructure()
        .AddInfrastructurePluginManager(config)
        .AddUserService()
        .AddRateLimiting(config)
        .AddResultStorageService(config)
        .AddEventPublisher()
        .AddHealthChecker(config)
        .AddOpenApi(config)
        .AddHostedService<WorkflowExecutionWorker>()
        .AddHostedService<TriggerProcessingService>()
        .AddConfiguredCors(config)
        .AddAiService(config)
        .AddNotificationsService(config);

    if (!env.IsDevelopment())
        builder.Services.ParseArguments(args);

    builder.ConfigureHttpServer();
}

static void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler(handlerApp =>
            handlerApp.Run(async context =>
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
    app.UseHealthCheck();
}

static void ConfigureApplication(WebApplication app)
{
    app.EnsureApplicationDatabaseCreated();

    app.MapHub<WorkflowsHub>("/hubs/workflowExecutions");
    app.MapEndpoints("Fixed");

    var listener = app.Services.GetRequiredService<IWorkflowHttpListener>();
    app.MapHttpTriggersWorkflowRoutes(listener);
}

static async Task HandleStartupExceptionAsync(WebApplicationBuilder builder, Exception ex)
{
    try
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Unhandled exception during startup");
    }
    catch
    {
        await Console.Error.WriteLineAsync($"Startup error: {ex.Message}");
    }

    // Prevent console from closing immediately
    await Task.Delay(500);
}
#endregion