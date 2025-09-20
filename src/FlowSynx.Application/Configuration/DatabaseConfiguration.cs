namespace FlowSynx.Application.Configuration;

public class DatabaseConfiguration
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Name { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public Dictionary<string, string>? AdditionalOptions { get; set; }
    public string ConnectionString { get; set; } = "";
}