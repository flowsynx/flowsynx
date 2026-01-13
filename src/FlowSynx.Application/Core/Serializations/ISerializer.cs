namespace FlowSynx.Application.Core.Serializations;

public interface ISerializer
{
    string Serialize<T>(T obj);
    string SerializePretty<T>(T obj);
}