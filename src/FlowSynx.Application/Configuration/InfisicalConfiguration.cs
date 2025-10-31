namespace FlowSynx.Application.Configuration;

/// <summary>
/// Represents configuration settings required to fetch secrets from Infisical.
/// </summary>
public sealed class InfisicalConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether Infisical integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Infisical host URI. Defaults to the public cloud endpoint when not specified.
    /// </summary>
    public string? HostUri { get; set; }

    /// <summary>
    /// Gets or sets the Infisical project identifier that contains configuration secrets.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the environment slug that maps to the desired Infisical environment.
    /// </summary>
    public string EnvironmentSlug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret path within the Infisical project. Defaults to the root path.
    /// </summary>
    public string SecretPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether FlowSynx should fall back to appsettings when Infisical access fails.
    /// </summary>
    public bool FallbackToAppSettings { get; set; } = true;

    /// <summary>
    /// Gets the machine identity credentials used to authenticate against Infisical.
    /// </summary>
    public InfisicalMachineIdentityConfiguration MachineIdentity { get; set; } = new();
}

/// <summary>
/// Represents the machine identity credentials used to authenticate against Infisical.
/// </summary>
public sealed class InfisicalMachineIdentityConfiguration
{
    /// <summary>
    /// Gets or sets the Infisical machine identity client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Infisical machine identity client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}
