using FlowSynx.Core.Parers.Norms.Storage;
using FlowSynx.Plugin.Storage;

namespace FlowSynx.Core.Storage.Check;

public interface IEntityChecker
{
    Task<IEnumerable<CheckResult>> Check(StorageNormsInfo sourceStorageNormsInfo, StorageNormsInfo destinationStorageNormsInfo,
        StorageSearchOptions searchOptions, StorageCheckOptions checkOptions, CancellationToken cancellationToken = default);
}