using FlowSynx.Parsers;
using FlowSynx.Plugin.Services;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Parers.PluginInstancing;

public interface IPluginInstanceParser : IParser
{
    PluginInstance Parse(string path);
}