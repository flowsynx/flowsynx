namespace FlowSynx.Application.Serialization;

public interface IJsonParser
{
    object? ParseObject(string json);
}
