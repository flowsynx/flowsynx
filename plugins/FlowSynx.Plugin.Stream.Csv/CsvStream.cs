using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using System.Data;

namespace FlowSynx.Plugin.Stream.Csv;

public class CsvStream : IPlugin
{
    private CsvStreamSpecifications? _csvStreamSpecifications;
    private readonly IDeserializer _deserializer;

    public CsvStream(ILogger<CsvStream> logger, IDeserializer deserializer)
    {
        _deserializer = deserializer;
    }

    public Guid Id => Guid.Parse("ce2fc15b-cd5e-4eb0-a5b4-22fa714e5cc9");
    public string Name => "CSV";
    public PluginNamespace Namespace => PluginNamespace.Stream;
    public string? Description => Resources.PluginDescription;
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(CsvStreamSpecifications);

    public Task Initialize()
    {
        _csvStreamSpecifications = Specifications.ToObject<CsvStreamSpecifications>();
        return Task.CompletedTask;
    }

    public Task<object> About(PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<object> CreateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createFilters = filters.ToObject<CreateFilters>();

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && createFilters.Overwrite is false)
            throw new StreamException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        string delimiter = GetDelimiter(createFilters.Delimiter);
        var headers = _deserializer.Deserialize<string[]>(createFilters.Headers);
        var data = string.Join(delimiter, headers);
        File.WriteAllText(path, data);

        return Task.FromResult<object>(new { });
    }

    public Task<object> WriteAsync(string entity, PluginFilters? filters, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<object> ReadAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<object> UpdateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<object>> DeleteAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<object>> ListAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        var listFilters = filters.ToObject<ListFilters>();
        string delimiter = GetDelimiter(listFilters.Delimiter);

        DataTable dt = ImportCsv(entity, delimiter);
        var result = new List<object>();

        var colCount = dt.Columns.Count;
        foreach (DataRow dr in dt.Rows)
        {
            dynamic objExpando = new System.Dynamic.ExpandoObject();
            var obj = objExpando as IDictionary<string, object>;

            for (var i = 0; i < colCount; i++)
            {
                var key = dr.Table.Columns[i].ColumnName.ToString();
                var val = dr[key];

                if (obj != null) 
                    obj[key] = val;
            }

            if (obj != null)
                result.Add(obj);
        }

        return Task.FromResult<IEnumerable<object>>(result);
    }

    public DataTable ImportCsv(string fullPath, string sepString)
    {
        var dt = new DataTable();
        using var sr = new StreamReader(fullPath);
        var firstLine = sr.ReadLine();
        var headers = firstLine?.Split(sepString, StringSplitOptions.None);
        if (headers != null)
        {
            foreach (var header in headers)
            {
                dt.Columns.Add(header);
            }

            var columnInterval = headers.Count();
            var newLine = sr.ReadLine();
            while (newLine != null)
            {
                object?[] fields = newLine.Split(sepString, StringSplitOptions.None);
                var currentLength = fields.Count();
                if (currentLength < columnInterval)
                {
                    while (currentLength < columnInterval)
                    {
                        newLine += sr.ReadLine();
                        currentLength = newLine.Split(sepString, StringSplitOptions.None).Count();
                    }

                    fields = newLine.Split(sepString, StringSplitOptions.None);
                }

                if (currentLength > columnInterval)
                {
                    newLine = sr.ReadLine();
                    continue;
                }

                if (!fields.Any())
                    continue;

                dt.Rows.Add(fields);
                newLine = sr.ReadLine();
            }
        }

        sr.Close();
        return dt;
    }

    public Task<IEnumerable<TransmissionData>> PrepareTransmissionData(string entity, PluginFilters? filters,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<object>> TransmitDataAsync(string entity, PluginFilters? filters, IEnumerable<TransmissionData> transmissionData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginFilters? filters,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
        //throw new NotImplementedException();
    }

    #region internal methods
    private string GetDelimiter(string delimiter)
    {
        var defaultDelimiter = ",";
        if (_csvStreamSpecifications is null)
            return defaultDelimiter;

        var configDelimiter = string.IsNullOrEmpty(_csvStreamSpecifications.Delimiter) 
            ? defaultDelimiter 
            : _csvStreamSpecifications.Delimiter;

        return string.IsNullOrEmpty(delimiter) ? configDelimiter : delimiter;
    }
    #endregion
}