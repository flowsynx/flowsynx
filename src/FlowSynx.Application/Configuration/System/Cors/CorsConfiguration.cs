namespace FlowSynx.Application.Configuration.System.Cors;

public class CorsConfiguration
{
    public string? PolicyName { get; set; } = "DefaultCorsPolicy";
    public List<string> AllowedOrigins { get; set; } = new();
    public bool AllowCredentials { get; set; } = false;
}