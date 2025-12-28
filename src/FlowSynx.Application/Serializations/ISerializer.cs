namespace FlowSynx.Application.Serializations;

public interface ISerializer
{
    string Serialize(object? input);
    string Serialize(object? input, SerializationConfiguration configuration);
}