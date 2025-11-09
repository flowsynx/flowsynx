namespace FlowSynx.Infrastructure.AI.AzureOpenAi;

internal sealed class AzureOpenAiConfiguration
{
    public string Endpoint { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string Deployment { get; set; } = "gpt-4o-mini";
}