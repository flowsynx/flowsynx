using System.Runtime.Serialization;

namespace FlowSync.Abstractions.Entities;

public enum FilterItemKind
{
    File = 0,
    Directory,
    FileAndDirectory
}