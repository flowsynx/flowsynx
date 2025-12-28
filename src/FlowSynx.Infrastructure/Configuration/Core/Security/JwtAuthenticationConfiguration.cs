namespace FlowSynx.Infrastructure.Configuration.Core.Security;

public class JwtAuthenticationsConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool RequireHttps { get; set; } = false;
    public List<string> RoleClaimNames { get; set; } = new() { "roles", "role", "groups" };
}