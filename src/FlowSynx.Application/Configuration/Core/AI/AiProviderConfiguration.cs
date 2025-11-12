namespace FlowSynx.Application.Configuration.Core.AI;

public class AiProviderConfiguration : Dictionary<string, string>
{
    public AiProviderConfiguration() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}