namespace FlowSynx.Application.Serialization;

public interface IJsonDeserializer
{
    T Deserialize<T>(string? input);
    T Deserialize<T>(string input, JsonSerializationConfiguration configuration);
}