using System;
using FlowSynx.Application.Configuration;
using Microsoft.Extensions.Configuration;

namespace FlowSynx.Infrastructure.Configuration;

/// <summary>
/// Resolves the configuration source that FlowSynx should use at runtime.
/// </summary>
public static class ConfigurationSourceSelector
{
    /// <summary>
    /// Environment variable key used to determine the configuration source.
    /// </summary>
    public const string EnvironmentVariableKey = "FLOWSYNX_CONFIG_SOURCE";

    /// <summary>
    /// Configuration key that stores the requested source.
    /// </summary>
    public const string ConfigurationSectionKey = "Configuration:Source";

    /// <summary>
    /// Configuration key that stores the active source used after resolution.
    /// </summary>
    public const string ActiveSourceConfigurationKey = "Configuration:ActiveSource";

    /// <summary>
    /// Configuration key that flags a fallback to appsettings due to Infisical failure.
    /// </summary>
    public const string InfisicalFallbackKey = "Configuration:InfisicalFallback";

    /// <summary>
    /// Resolves the configuration source to use, favouring command-line arguments, then environment variables, then appsettings.
    /// </summary>
    /// <param name="args">Application command-line arguments.</param>
    /// <param name="configuration">Current configuration manager.</param>
    /// <returns>The resolved configuration source option.</returns>
    public static ConfigurationSourceOption Resolve(string[] args, IConfiguration configuration)
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        var resolvedFromArgs = ParseFromArguments(args);
        if (resolvedFromArgs.HasValue)
            return resolvedFromArgs.Value;

        var resolvedFromEnvironment = ParseOption(Environment.GetEnvironmentVariable(EnvironmentVariableKey));
        if (resolvedFromEnvironment.HasValue)
            return resolvedFromEnvironment.Value;

        var resolvedFromConfiguration = ParseOption(configuration[ConfigurationSectionKey]);
        if (resolvedFromConfiguration.HasValue)
            return resolvedFromConfiguration.Value;

        return ConfigurationSourceOption.AppSettings;
    }

    private static ConfigurationSourceOption? ParseFromArguments(string[] args)
    {
        if (args is null || args.Length == 0)
            return null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument.StartsWith("--config-source=", StringComparison.OrdinalIgnoreCase))
            {
                var value = argument[(argument.IndexOf('=') + 1)..];
                return ParseOption(value);
            }

            if (!string.Equals(argument, "--config-source", StringComparison.OrdinalIgnoreCase))
                continue;

            if (index + 1 >= args.Length)
                break;

            return ParseOption(args[index + 1]);
        }

        return null;
    }

    private static ConfigurationSourceOption? ParseOption(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Enum.TryParse<ConfigurationSourceOption>(value, ignoreCase: true, out var parsed))
            return parsed;

        return value.Trim().Equals("json", StringComparison.OrdinalIgnoreCase)
            ? ConfigurationSourceOption.AppSettings
            : null;
    }
}
