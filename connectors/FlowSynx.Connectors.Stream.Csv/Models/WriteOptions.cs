namespace FlowSynx.Connectors.Stream.Csv.Models;

public class WriteOptions
{
    public string Headers { get; set; } = string.Empty;
    public object? Data { get; set; }
    public bool? OverWrite { get; set; } = false;
}