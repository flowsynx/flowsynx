using System.Data;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Stream.Json.Services;

public interface IJsonManager
{
    Task<object> About(Context context, CancellationToken cancellationToken);

    Task CreateAsync(Context context, CancellationToken cancellationToken);

    Task WriteAsync(Context context, CancellationToken cancellationToken);

    Task<ReadResult> ReadAsync(Context context, CancellationToken cancellationToken);

    Task DeleteAsync(Context context, CancellationToken cancellationToken);

    Task PurgeAsync(Context context, CancellationToken cancellationToken);

    Task<bool> ExistAsync(Context context, CancellationToken cancellationToken);

    Task<DataTable> FilteredEntitiesAsync(Context context, CancellationToken cancellationToken);

    Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, Context context, CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> CompressDataTable(DataTable dataTable, bool? indented);

    Task<IEnumerable<CompressEntry>> CompressDataRows(DataRowCollection dataRows, bool? indented);

    Task TransferData(Context context, TransferData transferData, CancellationToken cancellationToken);
}