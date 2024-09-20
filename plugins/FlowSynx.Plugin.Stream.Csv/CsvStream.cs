using FlowSynx.Data.Filter;
using FlowSynx.IO;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Plugin.Abstractions.Extensions;
using Microsoft.Extensions.Logging;
using FlowSynx.Data.Extensions;

namespace FlowSynx.Plugin.Stream.Csv;

public class CsvStream : IPlugin
{
    private CsvStreamSpecifications? _csvStreamSpecifications;
    private readonly IDeserializer _deserializer;
    private readonly IDataFilter _dataFilter;
    private readonly CsvLoader _csvLoader;

    public CsvStream(ILogger<CsvStream> logger, IDataFilter dataFilter,
        IDeserializer deserializer, ISerializer serializer)
    {
        _deserializer = deserializer;
        _dataFilter = dataFilter;
        _csvLoader = new CsvLoader(logger, serializer);
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

    public Task<object> About(PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new StreamException(Resources.AboutOperrationNotSupported);
    }

    public Task<object> CreateAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var createOptions = options.ToObject<CreateOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        if (File.Exists(path) && createOptions.Overwrite is false)
            throw new StreamException(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, path));

        var delimiter = GetDelimiter(createOptions.Delimiter);
        var headers = _deserializer.Deserialize<string[]>(createOptions.Headers);
        var data = string.Join(delimiter, headers);
        using (var writer = File.AppendText(path))
        {
            writer.WriteLine(data);
        }

        return Task.FromResult<object>(new { });
    }

    public Task<object> WriteAsync(string entity, PluginOptions? options, object dataOptions,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var writeOptions = options.ToObject<WriteOptions>();

        if (string.IsNullOrEmpty(path))
            throw new StreamException(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new StreamException(Resources.ThePathIsNotFile);

        var dataValue = dataOptions.GetObjectValue();

        if (dataValue is null)
            throw new StreamException("Data must have value.");

        if (dataValue is not string)
            throw new StreamException("Data is not in valid format.");

        var delimiter = GetDelimiter(writeOptions.Delimiter);
        var dataList = _deserializer.Deserialize<List<List<string>>>(dataValue.ToString());
        using (var writer = File.AppendText(path))
        {
            foreach (var rowData in dataList)
            {
                writer.WriteLine(string.Join(delimiter, rowData));
            }
        }
        
        return Task.FromResult<object>(new { });
    }

    public async Task<object> ReadAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await ListAsync(path, options, cancellationToken).ConfigureAwait(false);

        var streamEntities = entities.ToList();
        if (!streamEntities.Any())
            throw new StreamException("string.Format(Resources.NoFilesFoundWithTheGivenFilter, path)");

        if (streamEntities.Count() > 1)
            throw new StreamException("The item you filter should be only single!");

        return streamEntities.First();
    }

    public Task<object> UpdateAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<object>> DeleteAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        listOptions.Fields = string.Empty;
        listOptions.IncludeMetadata = false;
        var delimiter = GetDelimiter(listOptions.Delimiter);

        var fields = DeserializeToStringArray(listOptions.Fields);
        var dataFilterOptions = new DataFilterOptions
        {
            Fields = fields,
            FilterExpression = listOptions.Filter,
            SortExpression = listOptions.Sort,
            CaseSensetive = listOptions.CaseSensitive,
            Limit = listOptions.Limit
        };

        var dataTable = _csvLoader.Load(path, delimiter, listOptions.IncludeMetadata);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        _csvLoader.Delete(dataTable, filteredData);
        
        var result = filteredData.CreateListFromTable();
        var data = _csvLoader.ToCsv(dataTable, delimiter);
        await File.WriteAllTextAsync(path, data, cancellationToken);
        return result;
    }
    
    public async Task<bool> ExistAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var entities = await ListAsync(path, options, cancellationToken).ConfigureAwait(false);
        var streamEntities = entities.ToList();
        return streamEntities.Any();
    }

    public Task<IEnumerable<object>> ListAsync(string entity, PluginOptions? options, 
        CancellationToken cancellationToken = new CancellationToken())
    {
        var path = PathHelper.ToUnixPath(entity);
        var listOptions = options.ToObject<ListOptions>();
        var delimiter = GetDelimiter(listOptions.Delimiter);

        var fields = DeserializeToStringArray(listOptions.Fields);
        var dataFilterOptions = new DataFilterOptions
        {
            Fields = fields,
            FilterExpression = listOptions.Filter,
            SortExpression = listOptions.Sort,
            CaseSensetive = listOptions.CaseSensitive,
            Limit = listOptions.Limit
        };

        var dataTable = _csvLoader.Load(path, delimiter, listOptions.IncludeMetadata);
        var filteredData = _dataFilter.Filter(dataTable, dataFilterOptions);
        var result = filteredData.CreateListFromTable();

        return Task.FromResult<IEnumerable<object>>(result);
    }
    
    public Task<IEnumerable<TransmissionData>> PrepareTransmissionData(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<object>> TransmitDataAsync(string entity, PluginOptions? options, IEnumerable<TransmissionData> transmissionData,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CompressEntry>> CompressAsync(string entity, PluginOptions? options,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
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

    private string[] DeserializeToStringArray(string? fields)
    {
        var result = Array.Empty<string>();
        if (!string.IsNullOrEmpty(fields))
        {
            result = _deserializer.Deserialize<string[]>(fields);
        }

        return result;
    }
    #endregion
}