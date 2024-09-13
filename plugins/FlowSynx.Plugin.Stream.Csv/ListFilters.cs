namespace FlowSynx.Plugin.Stream.Csv;

public class ListOptions
{
    public string Delimiter { get; set; } = ",";
    public string? Filter { get; set; } = string.Empty;
    public string? Sorting { get; set; } = string.Empty;

}