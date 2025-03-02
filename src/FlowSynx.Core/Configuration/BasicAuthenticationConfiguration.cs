namespace FlowSynx.Core.Configuration;

public class BasicAuthenticationConfiguration
{
    public List<BasicAuthenticationUser> Users { get; set; } = new();
}

public class BasicAuthenticationUser
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new List<string>();
}