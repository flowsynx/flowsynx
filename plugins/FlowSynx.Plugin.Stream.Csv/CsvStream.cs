using FlowSynx.IO.Compression;
using FlowSynx.Plugin.Abstractions;
using System.Data;

namespace FlowSynx.Plugin.Stream.Csv;

public class CsvStream: IPlugin
{
    public Guid Id => Guid.Parse("ce2fc15b-cd5e-4eb0-a5b4-22fa714e5cc9");
    public string Name => "CSV";
    public PluginNamespace Namespace => PluginNamespace.Stream;
    public string? Description { get; }
    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(CsvStreamSpecifications);

    public Task Initialize()
    {
        return Task.CompletedTask;
    }

    public Task<object> About(PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<object> CreateAsync(string entity, PluginFilters? filters, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
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
        DataTable dt = ImportCsv(entity, ",");
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
                //create column headers
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
}