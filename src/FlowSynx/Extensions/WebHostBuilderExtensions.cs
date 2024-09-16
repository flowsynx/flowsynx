namespace FlowSynx.Extensions;

public static class WebHostBuilderExtensions
{
    public static IWebHostBuilder ConfigHttpServer(this IWebHostBuilder webHost, int port)
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
}