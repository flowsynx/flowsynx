using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Plugin.Stream.Csv;

public class CsvStreamSpecifications: PluginSpecifications
{
    public string Delimiter { get; set; } = ",";
}