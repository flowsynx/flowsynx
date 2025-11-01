namespace FlowSynx.Infrastructure.Secrets.Infisical;

internal sealed class InfisicalConfiguration
{
    public string? HostUri { get; set; }
    public string ProjectId { get; set; } = string.Empty;
    public string EnvironmentSlug { get; set; } = string.Empty;
    public string SecretPath { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}