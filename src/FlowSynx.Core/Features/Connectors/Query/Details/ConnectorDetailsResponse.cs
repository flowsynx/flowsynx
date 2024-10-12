namespace FlowSynx.Core.Features.Connectors.Query.Details;

public class ConnectorDetailsResponse
{
    public required Guid Id { get; set; }
    public required string Type { get; set; }
    public string? Description { get; set; }
    public List<ConnectorDetailsSpecification>? Specifications { get; set; } = new List<ConnectorDetailsSpecification>();
}