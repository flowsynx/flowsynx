using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Connectors.Stream.Csv;

public class CsvStreamSpecifications: Specifications
{
    public string Delimiter { get; set; } = ",";
}