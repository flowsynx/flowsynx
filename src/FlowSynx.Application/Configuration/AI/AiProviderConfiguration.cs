namespace FlowSynx.Application.Configuration.AI;

public class AiProviderConfiguration : Dictionary<string, string>
{
    public AiProviderConfiguration() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}