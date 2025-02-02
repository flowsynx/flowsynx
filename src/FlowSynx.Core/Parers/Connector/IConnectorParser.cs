using FlowSynx.Parsers;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Parers.Connector;

public interface IConnectorParser : IParser
{
    FlowSynx.Connectors.Abstractions.Connector Parse(string? connector);
}