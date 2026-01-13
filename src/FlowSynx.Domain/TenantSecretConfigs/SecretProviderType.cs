namespace FlowSynx.Domain.TenantSecretConfigs;

public enum SecretProviderType
{
    BuiltIn = 0,
    AzureKeyVault = 1,
    AwsSecretsManager = 2,
    HashiCorpVault = 3,
    Infisical = 4,
}