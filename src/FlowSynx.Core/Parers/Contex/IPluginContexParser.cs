using FlowSynx.Parsers;
using FlowSynx.Plugin;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Parers.Contex;

public interface IPluginContextParser : IParser
{
    PluginContext Parse(string path);
}