namespace FlowSynx.Services;

public class DefaultEndpoint : IEndpoint
{
    private readonly ILogger<DefaultEndpoint> _logger;

    public DefaultEndpoint(ILogger<DefaultEndpoint> logger)
    {
        _logger = logger;
    }
    
    public int HttpPort() => 5860;
}