using FlowSynx.Parsers;

namespace FlowSynx.Core.Parers.Norms.Storage;

public interface IStorageNormsParser : IParser
{
    StorageNormsInfo Parse(string path);
}