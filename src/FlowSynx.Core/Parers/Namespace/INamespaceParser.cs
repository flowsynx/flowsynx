using FlowSynx.Parsers;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Parers.Namespace;

public interface INamespaceParser: IParser
{
    PluginNamespace Parse(string type);
}