using FlowSynx.Parsers;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Parers.Connector;

public interface IConnectorParser : IParser
{
    ConnectorContext Parse(string? connectorInput);
}