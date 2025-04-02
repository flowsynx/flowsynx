using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Services;

namespace FlowSynx.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigHttpServer(this WebApplicationBuilder builder)
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var endpoint = scope.ServiceProvider.GetRequiredService<IEndpoint>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            builder.WebHost.ConfigureKestrel((context, kestrelOptions) =>
            {
                var httpPort = endpoint.HttpPort;
                kestrelOptions.ListenAnyIP(httpPort);
            });

            builder.WebHost.UseKestrel(option =>
            {
                option.AddServerHeader = false;
                option.Limits.MaxRequestBufferSize = null;
            });

            return builder;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationConfigureServer, ex.Message);
            logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}