namespace FlowSynx.Connectors.Stream.Csv.Options;

public class WriteOptions
{
    public string Headers { get; set; } = string.Empty;
    public bool? OverWrite { get; set; } = false;
}