using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions.Models;

namespace FlowSync.Abstractions.Filter;

public interface IFileSystemFilter
{
    public IEnumerable<FileSystemEntity> FilterEntitiesList(IEnumerable<FileSystemEntity> entities, FileSystemFilterOptions fileSystemFilterOptions);
}