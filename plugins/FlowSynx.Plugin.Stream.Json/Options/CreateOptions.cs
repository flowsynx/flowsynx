namespace FlowSynx.Plugin.Stream.Json.Options;

public class CreateOptions
{
    public string Headers { get; set; } = string.Empty;
    public bool? Overwrite { get; set; } = false;
}