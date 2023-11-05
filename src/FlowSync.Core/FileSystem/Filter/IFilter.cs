using FlowSync.Abstractions;
using FlowSync.Abstractions.Entities;

namespace FlowSync.Core.FileSystem.Filter;

internal interface IFilter
{
    public IEnumerable<Entity> FilterList(IEnumerable<Entity> entities, FilterOptions filterOptions);
}