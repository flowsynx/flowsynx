namespace FlowSynx.Infrastructure.Secrets.AzureKeyVault;

internal sealed class AzureKeyVaultConfiguration
{
    public string VaultUri { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}