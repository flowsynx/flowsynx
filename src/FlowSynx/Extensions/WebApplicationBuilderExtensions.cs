using FlowSynx.Application.Configuration.System.Logger;
using FlowSynx.Application.Configuration.System.Server;
using FlowSynx.Domain;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace FlowSynx.Extensions;

public static class WebApplicationBuilderExtensions
{
    private const int DefaultHttpPort = 6262;
    private const int DefaultHttpsPort = 6263;

    private static T BindSection<T>(this IConfiguration configuration, string sectionName) where T : new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);
        return section;
    }

    public static WebApplicationBuilder AddConfiguredLogging(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration.BindSection<LoggerConfiguration>("System:Logger");
        builder.Services.AddSingleton(config);

        if (config == null || !config.Enabled)
            return builder;

        builder.Services.AddSingleton<ILoggerProvider>(sp =>
        {
            var factory = new CompositeLoggingProviderFactory(sp);

            var loggingProviders = new List<ILoggerProvider>();

            if (config.DefaultProviders == null || config.Providers == null)
                return new CompositeLoggerProvider(loggingProviders);

            foreach (var entry in config.DefaultProviders)
            {
                var match = config.Providers
                    .FirstOrDefault(p => string.Equals(p.Key, entry, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(match.Key))
                    continue;

                var provider = factory.Create(match.Key, match.Value);
                if (provider != null)
                    loggingProviders.Add(provider);
            }

            return new CompositeLoggerProvider(loggingProviders);
        });

        return builder;
    }

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
        ILogger logger)
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
        ILogger logger,
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