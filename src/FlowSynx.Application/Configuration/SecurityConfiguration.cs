using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Configuration;

public class SecurityConfiguration
{
    public bool EnableBasic { get; set; } = true;
    public List<BasicAuthenticationConfiguration> BasicUsers { get; set; } = new();
    public List<JwtAuthenticationsConfiguration> JwtProviders { get; set; } = new();
    public string? DefaultScheme { get; set; }

    public void ValidateDefaultScheme(ILogger logger)
    {
        var validSchemes = new List<string>();

        if (EnableBasic)
        {
            validSchemes.Add("Basic");
            logger.LogInformation("Basic authentication is enabled.");
        }

        foreach (var jwt in JwtProviders)
        {
            validSchemes.Add(jwt.Name);
            logger.LogInformation("JWT provider '{Scheme}' configured", jwt.Name);
        }

        if (!string.IsNullOrEmpty(DefaultScheme))
        {
            if (!validSchemes.Contains(DefaultScheme))
            {
                throw new InvalidOperationException(
                    $"Invalid DefaultScheme '{DefaultScheme}'. Must be one of: {string.Join(", ", validSchemes)}");
            }

            logger.LogInformation("Default authentication scheme set to: {Scheme}", DefaultScheme);
        }
        else
        {
            logger.LogWarning("No default authentication scheme is defined.");
        }
    }
}