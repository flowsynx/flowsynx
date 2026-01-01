namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record SecurityConfiguration
{
    public AuthenticationConfiguration Authentication { get; set; } = new();

    public static SecurityConfiguration Create()
    {
        return new SecurityConfiguration
        {
            Authentication = new AuthenticationConfiguration
            {
                Mode = AuthenticationMode.None,
                Basic = new BasicConfiguration
                {
                    Users = new List<BasicAuthenticationConfiguration>()
                    {
                        new BasicAuthenticationConfiguration
                        {
                            Id = "admin",
                            Name = "Administrator",
                            Password = "admin",
                            Roles = new List<string> { "admin" }
                        }
                    }
                },
                Jwt = new JwtAuthenticationsConfiguration
                {
                    Name = string.Empty,
                    Authority = string.Empty,
                    Audience = string.Empty,
                    Issuer = string.Empty,
                    Secret = string.Empty,
                    RequireHttps = false,
                    RoleClaimNames = new List<string> { "role", "roles" }
                }
            }
        };
    }
}