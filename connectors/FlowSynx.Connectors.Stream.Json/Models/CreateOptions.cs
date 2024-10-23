namespace FlowSynx.Connectors.Stream.Json.Models;

public class CreateOptions
{
    public string Headers { get; set; } = string.Empty;
    public bool? Overwrite { get; set; } = false;
}