using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage.Check;

public class EntityChecker : IEntityChecker
{
    public async Task<IEnumerable<CheckResult>> Check(StorageNormsInfo sourceStorageNormsInfo,
        StorageNormsInfo destinationStorageNormsInfo, StorageSearchOptions searchOptions, StorageCheckOptions checkOptions,
        CancellationToken cancellationToken = default)
    {

        var sourceEntities = await sourceStorageNormsInfo.Plugin.ListAsync(sourceStorageNormsInfo.Path,
            searchOptions,
            new StorageListOptions { Kind = StorageFilterItemKind.File },
            new StorageHashOptions { Hashing = checkOptions.CheckHash },
            cancellationToken);

        var destinationEntities = await destinationStorageNormsInfo.Plugin.ListAsync(destinationStorageNormsInfo.Path,
            searchOptions,
            new StorageListOptions { Kind = StorageFilterItemKind.File },
            new StorageHashOptions { Hashing = checkOptions.CheckHash },
            cancellationToken);

        var storageSourceEntities = sourceEntities.ToList();
        var storageDestinationEntities = destinationEntities.ToList();

        var existOnSourceEntities = storageSourceEntities
            .Join(storageDestinationEntities, source => source.Id,
                destination => destination.Id,
                (source, destination) => (Source: source, Destination: destination)).ToList();

        var missedOnDestination = storageSourceEntities.Except(existOnSourceEntities.Select(x => x.Source));

        IEnumerable<StorageEntity> missedOnSource = new List<StorageEntity>();
        if (checkOptions.OneWay is false)
        {
            var existOnDestinationEntities = storageDestinationEntities
                .Join(storageSourceEntities, source => source.Id, destination => destination.Id, (source, destination) => source)
                .ToList();

            missedOnSource = storageDestinationEntities.Except(existOnDestinationEntities);
        }

        var result = new List<CheckResult>();
        foreach (var sourceEntity in existOnSourceEntities)
        {
            var state = CheckState.Different;
            if (sourceEntity.Source.Id == sourceEntity.Destination.Id)
            {
                state = checkOptions switch
                {
                    { CheckSize: true, CheckHash: false } => sourceEntity.Source.Size == sourceEntity.Destination.Size
                                                            ? CheckState.Match
                                                            : CheckState.Different,
                    { CheckSize: false, CheckHash: true } => !string.IsNullOrEmpty(sourceEntity.Source.Md5)
                                                           && !string.IsNullOrEmpty(sourceEntity.Destination.Md5)
                                                           && sourceEntity.Source.Md5 == sourceEntity.Destination.Md5
                                                            ? CheckState.Match
                                                            : CheckState.Different,
                    { CheckSize: true, CheckHash: true } => sourceEntity.Source.Size == sourceEntity.Destination.Size
                                                          && !string.IsNullOrEmpty(sourceEntity.Source.Md5)
                                                          && !string.IsNullOrEmpty(sourceEntity.Destination.Md5)
                                                          && sourceEntity.Source.Md5 == sourceEntity.Destination.Md5
                                                            ? CheckState.Match
                                                            : CheckState.Different,
                    _ => state = CheckState.Match,
                };
            }
            result.Add(new CheckResult { Entity = sourceEntity.Source, State = state });
        }

        result.AddRange(missedOnDestination.Select(sourceEntity => new CheckResult { Entity = sourceEntity, State = CheckState.MissedOnDestination }));
        result.AddRange(missedOnSource.Select(sourceEntity => new CheckResult { Entity = sourceEntity, State = CheckState.MissedOnSource }));

        return result;
    }
}