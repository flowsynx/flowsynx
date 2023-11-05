using FlowSync.Abstractions;
using FlowSync.Abstractions.Entities;
using FlowSync.Core.Wrapper;

namespace FlowSync.Core.FileSystem;

internal interface IFileSystemService
{
    Task<IResult<IEnumerable<Entity>>> List(string path, FilterOptions filters, CancellationToken cancellationToken);
}