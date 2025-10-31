namespace FlowSynx.Application.Configuration;

/// <summary>
/// Enumerates the available configuration providers recognised by FlowSynx.
/// </summary>
public enum ConfigurationSourceOption
{
    /// <summary>
    /// Use the local appsettings files as the primary configuration provider.
    /// </summary>
    AppSettings = 0,

    /// <summary>
    /// Load configuration values from Infisical.
    /// </summary>
    Infisical = 1
}
