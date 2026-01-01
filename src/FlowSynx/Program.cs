using FlowSynx.Application.Extensions;
using FlowSynx.Extensions;
using FlowSynx.Hubs;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Infrastructure.Secrets;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
    await ConfigureApplication(app);

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

    //builder.Services.AddSecretService(builder.Configuration);

    //using var scope = builder.Services.BuildServiceProvider().CreateScope();
    //var secretFactory = scope.ServiceProvider.GetRequiredService<ISecretFactory>();
    //var secretProvider = secretFactory.GetDefaultProvider();

    //builder.Configuration.AddSecrets(secretProvider);
}

static void ConfigureServices(WebApplicationBuilder builder, string[] args)
{
    //builder.AddLoggers();

    var services = builder.Services;
    var config = builder.Configuration;
    var env = builder.Environment;

    services
        .AddMemoryCache()
        .AddCancellationTokenSource()
        .AddHttpContextAccessor()
        .AddSystemClock()
        .AddJsonSerialization()
        .AddEndpointsApiExplorer()
        .AddHttpClient()
        .AddHttpJsonOptions()
        //.AddEncryptionService()
        .AddTenantService();

    builder.Services.AddScoped<ITenantLoggerFactory, SerilogTenantLoggerFactory>();
    builder.Services.AddScoped<ILoggerProvider, TenantLoggerProvider>();

    builder.Services.AddLogging(logging =>
    {
        logging.ClearProviders();
    });

    services.AddPersistence()
        .AddSecurity()
        .AddServer()
        .AddVersion()
        .AddApplication()
        .AddUserService()
        .AddEventPublisher()
        .AddHealthChecker()
        .AddApiDocumentation();

    if (!env.IsDevelopment())
        builder.Services.ParseArguments(args);

    builder.ConfigureHttpServer();
}

static void ConfigureMiddleware(WebApplication app)
{
    app.UseTenantLogging();

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
    app.UseRouting();

    app.UseTenantCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseTenantRateLimiting();

    app.UseApiDocumentation();
    app.UseCustomException();
    app.UseHealthCheck();
}

static async Task ConfigureApplication(WebApplication app)
{
    await app.EnsureApplicationDatabaseCreated();

    app.MapHub<WorkflowsHub>("/hubs/workflowExecutions");
    app.MapEndpoints("Fixed");
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