namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record LocalizationConfiguration
{
    public string Language { get; init; }

    public static LocalizationConfiguration Create()
    {
        return new LocalizationConfiguration
        {
            Language = "en"
        };
    }
}