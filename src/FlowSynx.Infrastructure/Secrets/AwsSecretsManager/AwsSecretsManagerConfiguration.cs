namespace FlowSynx.Infrastructure.Secrets.AwsSecretsManager;

internal sealed class AwsSecretsManagerConfiguration
{
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string SecretPrefix { get; set; } = string.Empty;
}