namespace FlowSynx.Application.Serializations;

public class SerializationConfiguration
{
    public bool Indented { get; set; } = false;
    public bool NameCaseInsensitive { get; set; } = true;
    public List<object>? Converters { get; set; }
}