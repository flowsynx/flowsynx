namespace FlowSync.Core.Parers.Norms.Storage;

public interface IStorageNormsParser : IParser
{
    StorageNormsInfo Parse(string path);
}