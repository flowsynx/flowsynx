namespace FlowSynx.Infrastructure.Secrets.HashiCorpVault;

internal sealed class HashiCorpVaultConfiguration
{
    public string VaultUri { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string MountPoint { get; set; } = "secret";
    public string SecretPath { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string SecretId { get; set; } = string.Empty;
    public string AuthMethod { get; set; } = "token";
}