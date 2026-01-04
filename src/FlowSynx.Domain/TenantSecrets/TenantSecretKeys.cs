namespace FlowSynx.Domain.TenantSecrets;

public static class TenantSecretKeys
{
    public static class Logging
    {
        public const string Enabled = "logging:enabled";

        public static class File
        {
            public const string LogLevel = "logging:File:logLevel";
            public const string LogPath = "logging:File:logPath";
            public const string RollingInterval = "logging:File:rollingInterval";
            public const string RetainedFileCountLimit = "logging:File:retainedFileCountLimit";
        }

        public static class Seq
        {
            public const string LogLevel = "logging:seq:logLevel";
            public const string Url = "logging:seq:url";
            public const string ApiKey = "logging:seq:apiKey";
        }
    }

    public static class Cors
    {
        public const string PolicyName = "cors:policyName";
        public const string AllowedOrigins = "cors:allowedOrigins";
        public const string AllowCredentials = "cors:allowCredentials";
    }

    public static class RateLimiting
    {
        public const string WindowSeconds = "rateLimiting:windowSeconds";
        public const string PermitLimit = "rateLimiting:permitLimit";
        public const string QueueLimit = "rateLimiting:queueLimit";
    }

    public static class Authentication
    {
        public const string Mode = "security:authentication:mode";

        public static class Basic
        {
            public const string Users = "security:authentication:basic:users";
            public const string Id = "id";
            public const string Username = "username";
            public const string Password = "password";
            public const string Roles = "roles";
        }

        public static class Jwt
        {
            public const string Issuer = "security:authentication:jwt:issuer";
            public const string Audience = "security:authentication:jwt:audience";
            public const string Authority = "security:authentication:jwt:authority";
            public const string Name = "security:authentication:jwt:name";
            public const string Secret = "security:authentication:jwt:secret";
            public const string RequireHttps = "security:authentication:jwt:requireHttps";
            public const string RoleClaimNames = "security:authentication:jwt:roleClaimNames";
        }
    }
}
