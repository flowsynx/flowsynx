using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Extensions;

public static class WebHostBuilderExtensions
{
    public static IWebHostBuilder ConfigHttpServer(this IWebHostBuilder webHost, int port, ILogger logger)
    {
        try
        {
            webHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(port);
            });
            webHost.UseKestrel(option =>
            {
                option.AddServerHeader = false;
                option.Limits.MaxRequestBufferSize = null;
            });
            return webHost;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationConfigureServer, ex.Message);
            logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}