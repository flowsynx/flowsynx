namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record SecretConfiguration
{
    public bool Enabled { get; init; } = false;
    public InfisicalSecretConfiguration Infisical { get; init; } = new();

    public static SecretConfiguration Create()
    {
        return new SecretConfiguration
        {
            Enabled = false,
            Infisical = InfisicalSecretConfiguration.Create()
        };
    }
}