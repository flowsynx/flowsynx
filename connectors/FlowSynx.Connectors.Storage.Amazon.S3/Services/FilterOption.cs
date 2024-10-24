using FlowSynx.Connectors.Storage.Options;
using FlowSynx.Data.Filter;
using FlowSynx.IO.Serialization;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public class FilterOption: IFilterOption
{
    private readonly IDeserializer _deserializer;

    public FilterOption(IDeserializer deserializer)
    {
        _deserializer = deserializer;
    }

    public DataFilterOptions GetFilterOptions(ListOptions options)
    {
        var fields = GetFields(options.Fields);
        var dataFilterOptions = new DataFilterOptions
        {
            Fields = fields,
            FilterExpression = options.Filter,
            SortExpression = options.Sort,
            CaseSensitive = options.CaseSensitive,
            Limit = options.Limit,
        };

        return dataFilterOptions;
    }

    public string[] GetFields(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }
}