namespace FlowSynx.Application.Localizations;

/// <summary>
/// Provides a central access point to the configured localization provider.
/// </summary>
public static class Localization
{
    /// <summary>
    /// Gets or sets the <see cref="ILocalization"/> implementation used to resolve localized strings.
    /// </summary>
    public static ILocalization? Instance { get; set; }

    /// <summary>
    /// Resolves a localized string for the specified key by delegating to the configured <see cref="Instance"/>.
    /// </summary>
    /// <param name="key">The localization key to resolve.</param>
    /// <returns>The localized string associated with the provided key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Instance"/> has not been configured.</exception>
    public static string Get(string key)
    {
        var localization = Instance ?? throw new InvalidOperationException("Localization.Instance is not initialized.");
        return localization.Get(key);
    }

    /// <summary>
    /// Resolves a formatted localized string for the specified key using the provided <paramref name="args"/>.
    /// </summary>
    /// <param name="key">The localization key to resolve.</param>
    /// <param name="args">Optional arguments used to populate placeholders in the localized string.</param>
    /// <returns>The formatted localized string associated with the provided key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Instance"/> has not been configured.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public static string Get(string key, params object[] args)
    { 
        var localization = Instance ?? throw new InvalidOperationException("Localization.Instance is not initialized.");

        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        return localization.Get(key, args); 
    }
}
