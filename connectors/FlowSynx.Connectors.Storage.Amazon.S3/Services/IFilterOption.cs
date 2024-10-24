using FlowSynx.Connectors.Storage.Options;
using FlowSynx.Data.Filter;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public interface IFilterOption
{
    DataFilterOptions GetFilterOptions(ListOptions options);
    string[] GetFields(string? fields);
}