using FlowSynx.Application;
using FlowSynx.Extensions;
using FlowSynx.Hubs;
using FlowSynx.Infrastructure.Common;
using FlowSynx.Infrastructure.Serializations.Json;
using Serilog;
using FlowSynx.Infrastructure.Messaging;
using FlowSynx.Infrastructure.Secrets;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

try
{
    // Handle version flag early exit
    if (args.HandleVersionFlag())
        return;

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

    services
        .AddMemoryCache()
        .AddCancellationTokenSource()
        .AddHttpContextAccessor()
        .AddUserService()
        .AddSystemClock()
        .AddJsonSerialization()
        .AddEndpointsApiExplorer()
        .AddHttpClient()
        .AddHttpJsonOptions()
        .AddDispatcher()
        .AddTenantService()
        .AddLoggingServices()
        .AddPersistence()
        .AddSecrets()
        .AddSecurity()
        .AddServer()
        .AddVersion()
        .AddApplication()
        .AddEventPublisher()
        .AddHealthChecker()
        .AddApiDocumentation();

    if (!env.IsDevelopment())
        builder.Services.ParseArguments(args);

    builder.ConfigureHttpServer();
}

static void ConfigureMiddleware(WebApplication app)
{
    // Exception handling must be first
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

    // Security basics
    app.UseHttps();
    app.UseCustomHeaders();

    // Resolve tenant as early as possible
    app.UseTenants();
    app.UseTenantLogging();

    // Routing (needed before auth)
    app.UseRouting();

    // Tenant-specific cross-cutting concerns
    app.UseTenantCors();
    app.UseTenantRateLimiting();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Observability & platform concerns
    app.UseApiDocumentation();
    app.UseHealthCheck();

    // Global exception mapping (domain -> HTTP)
    app.UseCustomException();

}

static async Task ConfigureApplication(WebApplication app)
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