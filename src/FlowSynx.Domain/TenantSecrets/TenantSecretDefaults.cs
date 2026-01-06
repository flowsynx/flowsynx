using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.TenantSecrets;

public static class TenantSecretDefaults
{
    public static List<TenantSecret> Default(TenantId tenantId)
    {
        var defaultSecrets = new List<TenantSecret>();
        void AddSecret(SecretKey key, SecretValue value)
        {
            var secret = TenantSecret.Create(tenantId, key, value);
            defaultSecrets.Add(secret);
        }

        // --------------------
        // Logging
        // --------------------
        AddSecret(SecretKey.Create(TenantSecretKeys.Logging.Enabled), SecretValue.Create("true"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Logging.DefaultLogLevel), SecretValue.Create("Information"));

        // File logging
        AddSecret(SecretKey.Create(TenantSecretKeys.Logging.File.LogLevel), SecretValue.Create("Information"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Logging.File.LogPath), SecretValue.Create("logs/"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Logging.File.RollingInterval), SecretValue.Create("Day"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Logging.File.RetainedFileCountLimit), SecretValue.Create("30"));

        // Seq logging
        AddSecret(SecretKey.Create(TenantSecretKeys.Logging.Seq.LogLevel), SecretValue.Create("Information"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Logging.Seq.Url), SecretValue.Create("http://localhost:5341"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Logging.Seq.ApiKey), SecretValue.Create("changeme")); // sensitive

        // --------------------
        // CORS
        // --------------------
        AddSecret(SecretKey.Create(TenantSecretKeys.Cors.PolicyName), SecretValue.Create($"DefaultCorsPolicy_{tenantId}"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Cors.AllowedOrigins), SecretValue.Create("*"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Cors.AllowCredentials), SecretValue.Create("false"));

        // --------------------
        // Rate limiting
        // --------------------
        AddSecret(SecretKey.Create(TenantSecretKeys.RateLimiting.WindowSeconds), SecretValue.Create("60"));
        AddSecret(SecretKey.Create(TenantSecretKeys.RateLimiting.PermitLimit), SecretValue.Create("100"));
        AddSecret(SecretKey.Create(TenantSecretKeys.RateLimiting.QueueLimit), SecretValue.Create("0"));

        // --------------------
        // Authentication
        // --------------------
        AddSecret(SecretKey.Create(TenantSecretKeys.Authentication.Mode), SecretValue.Create("None"));

        // Basic auth (structure placeholder)
        AddSecret(SecretKey.Create($"{TenantSecretKeys.Authentication.Basic.Users}[0]:id"), 
            SecretValue.Create($"0960a93d-e42b-4987-bc07-7bda806a21c7"));
        AddSecret(SecretKey.Create($"{TenantSecretKeys.Authentication.Basic.Users}[0]:username"), SecretValue.Create("admin"));
        AddSecret(SecretKey.Create($"{TenantSecretKeys.Authentication.Basic.Users}[0]:password"), SecretValue.Create("admin"));
        AddSecret(SecretKey.Create($"{TenantSecretKeys.Authentication.Basic.Users}[0]:roles"), SecretValue.Create("admin"));

        // JWT
        AddSecret(SecretKey.Create(TenantSecretKeys.Authentication.Jwt.Issuer), SecretValue.Create("http://localhost:8080"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Authentication.Jwt.Audience), SecretValue.Create("flowsynx-api"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Authentication.Jwt.Authority), SecretValue.Create("http://localhost:8080"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Authentication.Jwt.Name), SecretValue.Create("Flowsynx JWT"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Authentication.Jwt.Secret), SecretValue.Create("changeme")); // sensitive
        AddSecret(SecretKey.Create(TenantSecretKeys.Authentication.Jwt.RequireHttps), SecretValue.Create("false"));
        AddSecret(SecretKey.Create(TenantSecretKeys.Authentication.Jwt.RoleClaimNames), SecretValue.Create("roles"));

        return defaultSecrets;
    }
}