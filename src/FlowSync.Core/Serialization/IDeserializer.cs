namespace FlowSync.Core.Serialization;

public interface IDeserializer
{
    T? Deserialize<T>(string? input);
}