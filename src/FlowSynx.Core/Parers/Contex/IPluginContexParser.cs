using FlowSynx.Parsers;
using FlowSynx.Plugin;

namespace FlowSynx.Core.Parers.Contex;

public interface IPluginContexParser : IParser
{
    PluginContex Parse(string path);
}