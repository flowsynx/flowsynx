namespace FlowSynx.Application.Core.Serializations;

public interface ISerializer
{
    string Serialize(object? input);
    string Serialize(object? input, SerializationConfiguration configuration);
}