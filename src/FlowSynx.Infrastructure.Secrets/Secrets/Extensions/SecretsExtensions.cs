using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using FlowSynx.Domain.TenantSecretConfigs.Networking;
using FlowSynx.Domain.TenantSecretConfigs.Security;
using FlowSynx.Domain.TenantSecrets;

namespace FlowSynx.Infrastructure.Security.Secrets.Extensions;

public static class SecretsExtensions
{
    public static TenantCorsPolicy? GetCorsPolicy(this Dictionary<string, string?> secrets, TenantId tenantId)
    {
        return new TenantCorsPolicy
        {
            PolicyName =
                secrets.GetValueOrDefault(TenantSecretKeys.Cors.PolicyName)
                ?? $"DefaultCorsPolicy.{tenantId}",

            AllowedOrigins =
                secrets.GetValueOrDefault(TenantSecretKeys.Cors.AllowedOrigins)
                    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>(),

            AllowCredentials =
                bool.TryParse(
                    secrets.GetValueOrDefault(TenantSecretKeys.Cors.AllowCredentials),
                    out var allowCredentials)
                && allowCredentials
        };
    }

    public static TenantRateLimitingPolicy? GetRateLimitingPolicy(this Dictionary<string, string?> secrets)
    {
        return new TenantRateLimitingPolicy
        {
            WindowSeconds = int.TryParse(secrets.GetValueOrDefault(TenantSecretKeys.RateLimiting.WindowSeconds), out var windowSeconds) ? windowSeconds : 60,
            PermitLimit = int.TryParse(secrets.GetValueOrDefault(TenantSecretKeys.RateLimiting.PermitLimit), out var permitLimit) ? permitLimit : 100,
            QueueLimit = int.TryParse(secrets.GetValueOrDefault(TenantSecretKeys.RateLimiting.QueueLimit), out var queueLimit) ? queueLimit : 0
        };
    }

    public static TenantAuthenticationPolicy GetAuthenticationPolicy(this Dictionary<string, string?> secrets)
    {
        return new TenantAuthenticationPolicy
        {
            Mode = Enum.TryParse<TenantAuthenticationMode>(secrets.GetValueOrDefault(TenantSecretKeys.Authentication.Mode), out var mode) ? mode : TenantAuthenticationMode.None,
            Basic = new TenantBasicPolicy
            {
                Users = ParseBasicUsers(secrets)
            },
            Jwt = new TenantJwtAuthenticationPolicy
            {
                Issuer = secrets.GetValueOrDefault(TenantSecretKeys.Authentication.Jwt.Issuer) ?? string.Empty,
                Audience = secrets.GetValueOrDefault(TenantSecretKeys.Authentication.Jwt.Audience) ?? string.Empty,
                Authority = secrets.GetValueOrDefault(TenantSecretKeys.Authentication.Jwt.Authority) ?? string.Empty,
                Name = secrets.GetValueOrDefault(TenantSecretKeys.Authentication.Jwt.Name) ?? string.Empty,
                Secret = secrets.GetValueOrDefault(TenantSecretKeys.Authentication.Jwt.Secret) ?? string.Empty,
                RequireHttps = bool.TryParse(secrets.GetValueOrDefault(TenantSecretKeys.Authentication.Jwt.RequireHttps), out var requireHttps) && requireHttps,
                RoleClaimNames = (secrets.GetValueOrDefault(TenantSecretKeys.Authentication.Jwt.RoleClaimNames) ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList()
            }
        };
    }

    private static List<TenantBasicAuthenticationPolicy> ParseBasicUsers(Dictionary<string, string?> secrets)
    {
        var users = new List<TenantBasicAuthenticationPolicy>();
        var index = 0;

        while (true)
        {
            var prefix = $"{TenantSecretKeys.Authentication.Basic.Users}[{index}]";

            if (!secrets.ContainsKey($"{prefix}:{TenantSecretKeys.Authentication.Basic.Username}"))
                break;

            users.Add(new TenantBasicAuthenticationPolicy
            {
                Id = secrets.GetValueOrDefault($"{prefix}:{TenantSecretKeys.Authentication.Basic.Id}") ?? string.Empty,
                UserName = secrets.GetValueOrDefault($"{prefix}:{TenantSecretKeys.Authentication.Basic.Username}") ?? string.Empty,
                Password = secrets.GetValueOrDefault($"{prefix}:{TenantSecretKeys.Authentication.Basic.Password}") ?? string.Empty,
                Roles = (secrets.GetValueOrDefault($"{prefix}:{TenantSecretKeys.Authentication.Basic.Roles}") ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList()
            });

            index++;
        }

        return users;
    }

    public static TenantLoggingPolicy GetLoggingPolicy(this Dictionary<string, string?> secrets)
    {
        return new TenantLoggingPolicy
        {
            Enabled = bool.TryParse(secrets.GetValueOrDefault("logging:enabled"), out var enabled) && enabled,
            DefaultLogLevel = secrets.GetValueOrDefault("logging:defaultLogLevel") ?? "Information",
            File = new TenantFileLoggingPolicy
            {
                LogLevel = secrets.GetValueOrDefault("logging:File:logLevel") ?? "Information",
                LogPath = secrets.GetValueOrDefault("logging:File:logPath") ?? "logs/",
                RollingInterval = secrets.GetValueOrDefault("logging:File:rollingInterval") ?? "Day",
                RetainedFileCountLimit = int.TryParse(secrets.GetValueOrDefault("logging:File:retainedFileCountLimit"), out var retainedLimit) ? retainedLimit : 7
            },
            Seq = new TenantSeqLoggingPolicy
            {
                LogLevel = secrets.GetValueOrDefault("logging:seq:logLevel") ?? "Information",
                Url = secrets.GetValueOrDefault("logging:seq:url") ?? string.Empty,
                ApiKey = secrets.GetValueOrDefault("logging:seq:apiKey") ?? string.Empty
            }
        };
    }
}
