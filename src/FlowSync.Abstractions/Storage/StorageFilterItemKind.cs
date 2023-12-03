using System.Runtime.Serialization;

namespace FlowSync.Abstractions.Storage;

public enum StorageFilterItemKind
{
    File = 0,
    Directory,
    FileAndDirectory
}