using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Services;

public class DefaultEndpoint : IEndpoint
{
    private readonly ILogger<DefaultEndpoint> _logger;

    public DefaultEndpoint(ILogger<DefaultEndpoint> logger)
    {
        _logger = logger;
    }

    public int HttpPort()
    {
        try
        {
            return 5860;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationEndpoint, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}