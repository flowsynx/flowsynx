using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Stream.Csv.Models;
using FlowSynx.IO.Compression;
using System.Data;

namespace FlowSynx.Connectors.Stream.Csv.Services;

public interface ICsvManager
{
    Task<object> About(Context context, CancellationToken cancellationToken);

    Task CreateAsync(Context context, CancellationToken cancellationToken);

    Task WriteAsync(Context context, CancellationToken cancellationToken);

    Task<ReadResult> ReadAsync(Context context, CancellationToken cancellationToken);

    Task DeleteAsync(Context context, CancellationToken cancellationToken);

    Task<bool> ExistAsync(Context context, CancellationToken cancellationToken);
    
    Task<DataTable> FilteredEntitiesAsync(Context context, CancellationToken cancellationToken);

    Task<TransferData> PrepareDataForTransferring(Namespace @namespace, string type, Context context, CancellationToken cancellationToken);

    Task<IEnumerable<CompressEntry>> CompressDataTable(DataTable dataTable, DelimiterOptions delimiterOptions);

    Task<IEnumerable<CompressEntry>> CompressDataRows(DataRowCollection dataRows, DelimiterOptions delimiterOptions);

    Task TransferData(Context context, TransferData transferData, CancellationToken cancellationToken);

    bool IsCsvFile(string path);
}