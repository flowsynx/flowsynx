using FlowSynx.Application.Configuration;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigHttpServer(this WebApplicationBuilder builder)
    {
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var endpointConfiguration = scope.ServiceProvider.GetRequiredService<EndpointConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            builder.WebHost.ConfigureKestrel((_, kestrelOptions) =>
            {
                var httpPort = endpointConfiguration.Http;
                kestrelOptions.ListenAnyIP(httpPort ?? 6262);
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