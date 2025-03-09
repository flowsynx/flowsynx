using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Connectors.Stream.Csv.Models;

public class CsvSpecifications : Specifications
{
    public string Delimiter { get; set; } = ",";
}