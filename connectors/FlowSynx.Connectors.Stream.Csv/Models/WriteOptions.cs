namespace FlowSynx.Connectors.Stream.Csv.Models;

public class WriteOptions
{
    public string Headers { get; set; } = string.Empty;
    public bool? OverWrite { get; set; } = false;
}