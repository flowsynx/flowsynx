using FlowSync.Abstractions;

namespace FlowSync.Core.Parers.Namespace;

public interface INamespaceParser: IParser
{
    PluginNamespace Parse(string type);
}