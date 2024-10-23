namespace FlowSynx.Connectors.Stream.Json.Models;

public class TransferOptions
{
    public string? TransferKind { get; set; }
    public bool SeparateJsonPerRow { get; set; }
}