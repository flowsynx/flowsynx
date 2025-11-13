using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Configuration.Core.Security;

public class AuthenticationConfiguration
{
    public bool Enabled { get; set; } = false;
    public string? DefaultScheme { get; set; } = string.Empty;
    public BasicConfiguration Basic { get; set; } = new();
    public List<JwtAuthenticationsConfiguration> JwtProviders { get; set; } = new();

    public void ValidateDefaultScheme(ILogger logger)
    {
        var validSchemes = GetValidSchemes(logger);

        if (string.IsNullOrEmpty(DefaultScheme))
            SetDefaultSchemeWhenEmpty(logger);
        else
            ValidateAndNormalizeDefaultScheme(validSchemes, logger);
    }

    private List<string> GetValidSchemes(ILogger logger)
    {
        var validSchemes = new List<string>();

        if (!Enabled)
        {
            validSchemes.Add("Disabled");
            logger.LogWarning("Authentication is Disabled. This configuration introduces a security risk by allowing unrestricted access.");
            return validSchemes;
        }

        if (Basic.Enabled)
        {
            validSchemes.Add("Basic");
            logger.LogInformation("Basic authentication is enabled.");
        }

        foreach (var jwt in JwtProviders)
        {
            validSchemes.Add(jwt.Name);
            logger.LogInformation("JWT provider '{Scheme}' configured", jwt.Name);
        }

        return validSchemes;
    }

    private void SetDefaultSchemeWhenEmpty(ILogger logger)
    {
        if (!Enabled)
        {
            DefaultScheme = "Disabled";
            logger.LogWarning("Default authentication scheme set to 'Disabled'. This configuration introduces a security risk by allowing unrestricted access.");
        }
        else
        {
            logger.LogWarning("No default authentication scheme is defined.");
        }
    }

    private void ValidateAndNormalizeDefaultScheme(List<string> validSchemes, ILogger logger)
    {
        if (!Enabled)
        {
            DefaultScheme = "Disabled";
        }

        if (!validSchemes.Contains(DefaultScheme))
        {
            throw new FlowSynxException(
                (int)ErrorCode.SecurityConfigurationInvalidScheme,
                Localizations.Localization.Get(
                    "SecurityConfiguration_InvalidScheme",
                    DefaultScheme,
                    string.Join(", ", validSchemes))
            );
        }

        logger.LogInformation("Default authentication scheme set to: {Scheme}", DefaultScheme);
    }
}