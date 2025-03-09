namespace FlowSynx.Connectors.Stream.Json.Models;

public class WriteOptions
{
    public object? Data { get; set; }
    public bool? OverWrite { get; set; } = false;
}