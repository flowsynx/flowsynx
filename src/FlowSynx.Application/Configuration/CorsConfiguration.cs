namespace FlowSynx.Application.Configuration;

public class CorsConfiguration
{
    public string? PolicyName { get; set; } = "DefaultCorsPolicy";
    public List<string> AllowedOrigins { get; set; } = new();
    public bool AllowCredentials { get; set; } = false;
}