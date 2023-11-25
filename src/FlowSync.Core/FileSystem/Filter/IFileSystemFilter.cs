using FlowSync.Abstractions;
using FlowSync.Abstractions.Entities;

namespace FlowSync.Core.FileSystem.Filter;

internal interface IFileSystemFilter
{
    public IEnumerable<FileSystemEntity> FilterList(IEnumerable<FileSystemEntity> entities, FileSystemFilterOptions fileSystemFilterOptions);
}