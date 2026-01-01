using FlowSynx.PluginCore.Exceptions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using FlowSynx.Domain.Primitives;
using Serilog;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;
using FlowSynx.Infrastructure.Configuration.Server;

namespace FlowSynx.Extensions;

public static class TenantLogging
{
    public static Serilog.ILogger CreateTenantLogger(string tenantId)
    {
        var logPath = Path.Combine("logs", $"tenant-{tenantId}", "log-.txt");

        return new SerilogLoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("TenantId", tenantId)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {TenantId} {Message}{NewLine}{Exception}")
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();
    }
}

public static class WebApplicationBuilderExtensions
{
    private const int DefaultHttpPort = 6262;
    private const int DefaultHttpsPort = 6263;

    public static WebApplicationBuilder AddLoggers(this WebApplicationBuilder builder)
    {
        Log.Logger = new SerilogLoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "MultiTenantApp")
            .WriteTo.Console(outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {TenantId} {Message}{NewLine}{Exception}")
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();



        //var config = builder.Configuration.BindSection<LoggerConfiguration>("Logger");
        //builder.Services.AddSingleton(config);

        //// Register a composite provider that always includes Console and Database providers.
        //// If the user provided configuration entries for those providers, their settings are used.
        //builder.Services.AddSingleton<ILoggerProvider>(sp =>
        //{
        //    var factory = new CompositeLoggingProviderFactory(sp);
        //    var loggingProviders = new List<ILoggerProvider>();

        //    // 1) Ensure Console provider is present
        //    var consoleProvider = CreateProviderWithFallback(factory, config, "console");
        //    if (consoleProvider != null)
        //        loggingProviders.Add(consoleProvider);

        //    // 3) If configuration enables other providers and specifies a default order, add them
        //    if (config != null && config.Enabled && config.DefaultProviders != null && config.Providers != null)
        //    {
        //        foreach (var entry in config.DefaultProviders)
        //        {
        //            // Skip console because we already added it
        //            if (string.Equals(entry, "console", StringComparison.OrdinalIgnoreCase))
        //            {
        //                continue;
        //            }

        //            var match = config.Providers
        //                .FirstOrDefault(p => string.Equals(p.Key, entry, StringComparison.OrdinalIgnoreCase));

        //            if (string.IsNullOrEmpty(match.Key))
        //                continue;

        //            var provider = factory.Create(match.Key, match.Value);
        //            if (provider != null)
        //                loggingProviders.Add(provider);
        //        }
        //    }

        //    return new CompositeLoggerProvider(loggingProviders);
        //});

        return builder;
    }

    //private static ILoggerProvider? CreateProviderWithFallback(
    //    CompositeLoggingProviderFactory factory,
    //    LoggerConfiguration? config,
    //    string providerKey)
    //{
    //    if (factory == null)
    //        throw new ArgumentNullException(nameof(factory));

    //    // Try to find a matching configured provider entry
    //    if (config?.Providers != null)
    //    {
    //        var match = config.Providers.FirstOrDefault(p => string.Equals(p.Key, providerKey, StringComparison.OrdinalIgnoreCase));
    //        if (!string.IsNullOrEmpty(match.Key))
    //        {
    //            return factory.Create(match.Key, match.Value);
    //        }
    //    }

    //    // No configuration entry: create provider with default key and no settings
    //    return factory.Create(providerKey, null);
    //}

    public static WebApplicationBuilder ConfigureHttpServer(this WebApplicationBuilder builder)
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var serverConfig = scope.ServiceProvider.GetRequiredService<ServerConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            ConfigureKestrelEndpoints(builder, serverConfig, logger);
            ConfigureKestrelSettings(builder);

            return builder;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationConfigureServer, ex.Message);
            logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private static void ConfigureKestrelEndpoints(
        WebApplicationBuilder builder,
        ServerConfiguration config,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        builder.WebHost.ConfigureKestrel((_, options) =>
        {
            var httpPort = config.Http?.Port ?? DefaultHttpPort;
            int? httpsPort = GetHttpsPort(config, httpPort);

            options.ListenAnyIP(httpPort);

            if (!httpsPort.HasValue)
            {
                logger.LogInformation("Configuring HTTP endpoint only: HTTP {HttpPort}", httpPort);
                return;
            }

            ConfigureHttps(options, httpsPort.Value, config.Https, logger, httpPort);
        });
    }

    private static void ConfigureKestrelSettings(WebApplicationBuilder builder)
    {
        builder.WebHost.UseKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBufferSize = null;
        });
    }

    private static int? GetHttpsPort(
        ServerConfiguration config,
        int httpPort)
    {
        if (config.Https?.Enabled != true) return null;

        var httpsPort = config.Https.Port ?? DefaultHttpsPort;
        if (httpsPort == httpPort)
            throw new InvalidOperationException($"HTTP and HTTPS endpoint ports cannot be the same: {httpPort}");

        return httpsPort;
    }

    private static void ConfigureHttps(
        KestrelServerOptions options,
        int httpsPort,
        HttpsServerConfiguration? httpsConfig,
        Microsoft.Extensions.Logging.ILogger logger,
        int httpPort)
    {   
        options.ListenAnyIP(httpsPort, listenOptions =>
        {
            ConfigureListenOptions(listenOptions, httpsConfig);
        });

        logger.LogInformation("Configuring HTTP and HTTPS endpoints: HTTP {HttpPort}, HTTPS {HttpsPort}", httpPort, httpsPort);
    }

    private static void ConfigureListenOptions(
        ListenOptions listenOptions,
        HttpsServerConfiguration? httpsConfig)
    {
        var cert = httpsConfig?.Certificate;

        if (cert == null)
        {
            listenOptions.UseHttps();
            return;
        }

        ConfigureCertificate(listenOptions, cert);
    }

    private static void ConfigureCertificate(
        ListenOptions listenOptions,
        HttpsServerCertificateConfiguration cert)
    {
        if (cert == null)
            throw new ArgumentNullException(nameof(cert));

        if (string.IsNullOrWhiteSpace(cert.Password))
        {
            listenOptions.UseHttps(cert.Path);
            return;
        }

        listenOptions.UseHttps(cert.Path, cert.Password);
    }
}