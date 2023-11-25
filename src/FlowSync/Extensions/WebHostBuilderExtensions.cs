using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace FlowSync.Extensions;

public static class WebHostBuilderExtensions
{
    public static IWebHostBuilder ConfigHttpServer(this IWebHostBuilder webHost, int port)
    {
        webHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                //listenOptions.UseHttps();
            });
        });
        webHost.UseKestrel(option => option.AddServerHeader = false);

        return webHost;
    }
}