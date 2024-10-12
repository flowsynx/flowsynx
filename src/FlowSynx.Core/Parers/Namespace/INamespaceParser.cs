using FlowSynx.Parsers;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Parers.Namespace;

public interface INamespaceParser: IParser
{
    Connectors.Abstractions.Namespace Parse(string type);
}