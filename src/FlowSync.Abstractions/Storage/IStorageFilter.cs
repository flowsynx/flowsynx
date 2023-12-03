namespace FlowSync.Abstractions.Storage;

public interface IStorageFilter
{
    public IEnumerable<StorageEntity> FilterEntitiesList(IEnumerable<StorageEntity> entities, StorageSearchOptions storageSearchOptions);
}