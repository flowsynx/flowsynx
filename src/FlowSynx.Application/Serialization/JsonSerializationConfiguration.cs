namespace FlowSynx.Application.Serialization;

public class JsonSerializationConfiguration
{
    public bool Indented { get; set; } = false;
    public bool NameCaseInsensitive { get; set; } = true;
    public List<object>? Converters { get; set; }
}