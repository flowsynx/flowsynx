using FlowSynx.Application;
using FlowSynx.Extensions;
using FlowSynx.Hubs;
using FlowSynx.Infrastructure.Common;
using FlowSynx.Infrastructure.Serializations.Json;
using Serilog;
using FlowSynx.Infrastructure.Messaging;
using FlowSynx.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.AddSystemLogging();

try
{
    // Handle version flag early exit
    if (args.HandleVersionFlag())
        return;

    ConfigureConfiguration(builder);
    ConfigureServices(builder, args);

    var app = builder.Build();
    ConfigureMiddleware(app);
    await ConfigureFlowSynxApplication(app);

    await app.RunAsync();
}
catch (Exception ex)
{
    await HandleStartupExceptionAsync(builder, ex);
}
finally
{
    // Ensure Serilog flushes
    Log.CloseAndFlush();
}

#region helpers
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
}

static void ConfigureServices(WebApplicationBuilder builder, string[] args)
{
    var services = builder.Services;
    var config = builder.Configuration;
    var env = builder.Environment;

    services.AddDataProtection()
            .SetApplicationName("FlowSynx");

    services
        .AddMemoryCache()
        .AddFlowSynxCancellationTokenSource()
        .AddHttpContextAccessor()
        .AddFlowSynxUserService()
        .AddFlowSynxSystemClock()
        .AddJsonSerialization()
        .AddEndpointsApiExplorer()
        .AddHttpClient()
        .AddHttpJsonOptions()
        .AddFlowSynxDispatcher()
        .AddFlowSynxTenantService()
        .AddFlowSynxLoggingServices()
        .AddFlowSynxPersistence()
        .AddFlowSynxDataProtection()
        .AddFlowSynxSecretManagement()
        .AddFlowSynxSecurity()
        .AddFlowSynxServer()
        .AddFlowSynxVersion()
        .AddFlowSynxApplication()
        .AddFlowSynxEventPublisher()
        .AddFlowSynxHealthChecker()
        .AddFlowSynxApiDocumentation();

    if (!env.IsDevelopment())
        builder.Services.ParseFlowSynxArguments(args);

    builder.ConfigureFlowSynxHttpServer();
}

static void ConfigureMiddleware(WebApplication app)
{
    // GLOBAL EXCEPTION HANDLING — MUST BE FIRST
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // Centralized exception -> HTTP mapping
        app.UseFlowSynxCustomException();
    }

    // Security
    app.UseFlowSynxHttps();
    app.UseFlowSynxCustomHeaders();

    // Tenant resolution (early)
    app.UseFlowSynxTenants();
    //app.UseFlowSynxTenantLogging();

    // Routing (needed before auth)
    app.UseRouting();

    // Cross-cutting tenant concerns
    app.UseFlowSynxTenantCors();
    app.UseFlowSynxTenantRateLimiting();

    // Auth
    app.UseAuthentication();
    app.UseAuthorization();

    // Observability
    app.UseFlowSynxApiDocumentation();
    app.UseFlowSynxHealthCheck();
}

static async Task ConfigureFlowSynxApplication(WebApplication app)
{
    await app.EnsureApplicationDatabaseCreated();

    app.MapHub<WorkflowsHub>("/hubs/workflowExecutions");
    app.MapEndpoints();
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