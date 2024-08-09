using FlowSynx.Parsers;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Parers.Norms.Storage;

public interface IStoragePluginNormsParser : IParser
{
    StoragePluginNorms Parse(string path);
}