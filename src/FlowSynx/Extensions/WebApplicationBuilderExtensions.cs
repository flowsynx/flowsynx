using FlowSynx.Application.Configuration;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using System.Text;

namespace FlowSynx.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureHttpServer(this WebApplicationBuilder builder)
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var endpointConfig = scope.ServiceProvider.GetRequiredService<EndpointConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            ConfigureKestrelEndpoints(builder, endpointConfig, logger);
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
        EndpointConfiguration config,
        ILogger logger)
    {
        builder.WebHost.ConfigureKestrel((_, options) =>
        {
            var httpPort = config.Http?.Port ?? 6262;
            int? httpsPort = null;

            if (config.Https?.Enabled == true)
            {
                httpsPort = config.Https.Port ?? 6263;

                if (httpsPort == httpPort)
                {
                    var message = $"HTTP and HTTPS ports cannot be the same: {httpPort}";
                    logger.LogCritical(message);
                    throw new InvalidOperationException(message);
                }
            }

            options.ListenAnyIP(httpPort);

            if (httpsPort.HasValue)
            {
                options.ListenAnyIP(httpsPort.Value, listenOptions =>
                {
                    var cert = config.Https!.Certificate;

                    if (cert != null)
                    {
                        if (!string.IsNullOrWhiteSpace(cert.Password))
                        {
                            listenOptions.UseHttps(cert.Path, cert.Password);
                        }
                        else
                        {
                            listenOptions.UseHttps(cert.Path);
                        }
                    }
                    else
                    {
                        listenOptions.UseHttps();
                    }
                });

                logger.LogInformation("Configuring HTTP and HTTPS endpoints: HTTP {HttpPort}, HTTPS {HttpsPort}", httpPort, httpsPort);
            }
            else
            {
                logger.LogInformation("Configuring HTTP endpoint only: HTTP {HttpPort}", httpPort);
            }
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
}