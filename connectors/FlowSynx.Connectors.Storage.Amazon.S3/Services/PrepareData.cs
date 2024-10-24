using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Storage.Options;
using FlowSynx.IO;
using System.Data;
using FlowSynx.Data.Extensions;
using FlowSynx.Data.Filter;

namespace FlowSynx.Connectors.Storage.Amazon.S3.Services;

public class PrepareData: IPrepareData
{
    private readonly IAmazonS3Manager _browser;
    private readonly IDataFilter _dataFilter;
    private readonly IFilterOption _filterOption;

    public PrepareData(IAmazonS3Manager browser, IDataFilter dataFilter, IFilterOption filterOption)
    {
        _browser = browser;
        _dataFilter = dataFilter;
        _filterOption = filterOption;
    }

    public async Task<TransferData> PrepareTransferring(Namespace @namespace, string type, string entity, 
        ListOptions listOptions, ReadOptions readOptions, CancellationToken cancellationToken = default)
    {
        var path = PathHelper.ToUnixPath(entity);

        var storageEntities = await _browser.ListAsync(path, listOptions, cancellationToken);

        var fields = _filterOption.GetFields(listOptions.Fields);
        var kindFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("Kind", StringComparison.OrdinalIgnoreCase));
        var fullPathFieldExist = fields.Length == 0 || fields.Any(s => s.Equals("FullPath", StringComparison.OrdinalIgnoreCase));

        if (!kindFieldExist)
            fields = fields.Append("Kind").ToArray();

        if (!fullPathFieldExist)
            fields = fields.Append("FullPath").ToArray();

        var dataFilterOptions = _filterOption.GetFilterOptions(listOptions);

        var dataTable = storageEntities.ToDataTable();
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var transferDataRow = new List<TransferDataRow>();

        foreach (DataRow row in filteredData.Rows)
        {
            var content = string.Empty;
            var contentType = string.Empty;
            var fullPath = row["FullPath"].ToString() ?? string.Empty;

            if (string.Equals(row["Kind"].ToString(), StorageEntityItemKind.File, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var read = await _browser.ReadAsync(path, readOptions, cancellationToken).ConfigureAwait(false);
                    content = read.Content.ToBase64String();
                }
            }

            if (!kindFieldExist)
                row["Kind"] = DBNull.Value;

            if (!fullPathFieldExist)
                row["FullPath"] = DBNull.Value;

            var itemArray = row.ItemArray.Where(x => x != DBNull.Value).ToArray();
            transferDataRow.Add(new TransferDataRow
            {
                Key = fullPath,
                ContentType = contentType,
                Content = content,
                Items = itemArray
            });
        }

        if (!kindFieldExist)
            filteredData.Columns.Remove("Kind");

        if (!fullPathFieldExist)
            filteredData.Columns.Remove("FullPath");

        var columnNames = filteredData.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
        var result = new TransferData
        {
            Namespace = @namespace,
            ConnectorType = type,
            Kind = TransferKind.Copy,
            Columns = columnNames,
            Rows = transferDataRow
        };

        return result;
    }
}