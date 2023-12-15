using FlowSync.Core.Common.Services;

namespace FlowSync.Services;

public class DefaultEndpoint : IDefaultEndpoint
{
    private readonly IEnvironmentVariablesManager _environmentVariablesManager;

    public DefaultEndpoint(IEnvironmentVariablesManager environmentVariablesManager)
    {
        _environmentVariablesManager = environmentVariablesManager;
    }

    public int GetDefaultHttpEndpoint()
    {
        var flowSyncPort = _environmentVariablesManager.Get("FLOWSYNC_HTTP_PORT");
        var parsedPort = int.TryParse(flowSyncPort, out var result);
        return result > 0 ? result : 5860;
    }
}