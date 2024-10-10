namespace FlowSynx.Plugin.Stream.Csv;

public class WriteOptions
{
    public string Headers { get; set; } = string.Empty;
    public bool? Append { get; set; } = false;
}