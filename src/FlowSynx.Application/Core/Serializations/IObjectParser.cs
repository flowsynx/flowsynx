namespace FlowSynx.Application.Core.Serializations;

public interface IObjectParser
{
    object? ParseObject(string json);
}
