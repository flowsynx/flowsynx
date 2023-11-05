namespace FlowSync.Core.Serialization;

public interface ISerializer
{
    string Serialize(object? input);
}