namespace FlowSync.Abstractions.Storage;

public interface IStorageFilter
{
    IEnumerable<StorageEntity> FilterEntitiesList(IEnumerable<StorageEntity> entities, StorageSearchOptions storageSearchOptions, StorageListOptions listOptions);
}