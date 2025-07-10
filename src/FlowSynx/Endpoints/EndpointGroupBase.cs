namespace FlowSynx.Endpoints;

public abstract class EndpointGroupBase
{
    public abstract void Map(WebApplication app, string rateLimitPolicyName);
}