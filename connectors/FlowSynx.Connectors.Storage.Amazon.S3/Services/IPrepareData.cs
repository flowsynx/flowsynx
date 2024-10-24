using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.Options;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public interface IPrepareData
{
    Task<TransferData> PrepareTransferring(Namespace @namespace, string type, string entity,
        ListOptions listOptions, ReadOptions readOptions, CancellationToken cancellationToken = default);
}