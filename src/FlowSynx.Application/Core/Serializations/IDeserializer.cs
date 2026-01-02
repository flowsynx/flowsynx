namespace FlowSynx.Application.Core.Serializations;

public interface IDeserializer
{
    T Deserialize<T>(string? input);
    T Deserialize<T>(string input, SerializationConfiguration configuration);
}