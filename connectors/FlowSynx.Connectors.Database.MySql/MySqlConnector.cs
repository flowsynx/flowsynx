using FlowSynx.Connectors.Abstractions;
using FlowSynx.Connectors.Database.MySql.Models;
using FlowSynx.IO.Compression;

namespace FlowSynx.Connectors.Database.MySql;

public class MySqlConnector : Connector
{
    public override Guid Id => Guid.Parse("20fc1d0c-7fef-43e4-95bc-f99f3e5fd088");
    public override string Name => "MySql";
    public override Namespace Namespace => Namespace.Database;
    public override string? Description => Resources.ConnectorDescription;
    public override Specifications? Specifications { get; set; }
    public override Type SpecificationsType => typeof(MySqlpecifications);

    public override Task Initialize()
    {
        return Task.CompletedTask;
    }

    public override Task<object> About(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task CreateAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task WriteAsync(Context context, ConnectorOptions? options,
        object dataOptions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<ReadResult> ReadAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task UpdateAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<bool> ExistAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<IEnumerable<object>> ListAsync(Context context, ConnectorOptions? options,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task TransferAsync(Context sourceContext, Connector destinationConnector,
        Context destinationContext, ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task ProcessTransferAsync(Context context, TransferData transferData,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<IEnumerable<CompressEntry>> CompressAsync(Context context,
        ConnectorOptions? options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}