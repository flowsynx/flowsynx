namespace FlowSynx.Connectors.Stream.Csv.Options;

public class CreateOptions
{
    public string Headers { get; set; } = string.Empty;
    public bool? Overwrite { get; set; } = false;
}