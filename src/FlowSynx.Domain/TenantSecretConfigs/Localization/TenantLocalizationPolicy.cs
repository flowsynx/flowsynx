namespace FlowSynx.Domain.TenantSecretConfigs.Localization;

public sealed record TenantLocalizationPolicy
{
    public string Language { get; init; }

    public static TenantLocalizationPolicy Create()
    {
        return new TenantLocalizationPolicy
        {
            Language = "en"
        };
    }
}