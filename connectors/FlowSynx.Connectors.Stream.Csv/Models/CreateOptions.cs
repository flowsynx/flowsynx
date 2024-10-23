namespace FlowSynx.Connectors.Stream.Csv.Models;

public class CreateOptions
{
    public string Headers { get; set; } = string.Empty;
    public bool? Overwrite { get; set; } = false;
}