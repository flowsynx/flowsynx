namespace FlowSynx.Application.Serializations;

public interface IObjectParser
{
    object? ParseObject(string json);
}
