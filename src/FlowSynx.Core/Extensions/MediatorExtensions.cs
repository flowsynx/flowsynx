using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Core.Features.Config.Command.Add;
using FlowSynx.Core.Features.Config.Query.Details;
using FlowSynx.Core.Features.Config.Query.List;
using FlowSynx.Core.Features.Config.Command.Delete;
using FlowSynx.Core.Features.Connectors.Query.Details;
using FlowSynx.Core.Features.Connectors.Query.List;
using FlowSynx.Core.Features.Version.Query;
using FlowSynx.Core.Features.Logs.Query.List;
using FlowSynx.Core.Features.List.Query;
using FlowSynx.Core.Features.Write.Command;
using FlowSynx.Core.Features.Delete.Command;
using FlowSynx.Core.Features.Exist.Query;
using FlowSynx.Core.Features.Compress.Command;
using FlowSynx.Core.Features.Read.Query;
using FlowSynx.Core.Features.About.Query;
using FlowSynx.Core.Features.Create.Command;
using FlowSynx.Core.Features.Transfer.Command;
using FlowSynx.Connectors.Abstractions;
using FlowSynx.IO.Compression;
using FlowSynx.Data;
using FlowSynx.Core.Features.Workflow.Query;

namespace FlowSynx.Core.Extensions;

public static class MediatorExtensions
{
    #region Connectors
    public static Task<Result<object>> About(this IMediator mediator, AboutRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<InterchangeData>> List(this IMediator mediator, ListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> Write(this IMediator mediator, WriteRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<InterchangeData>> Read(this IMediator mediator, ReadRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> Delete(this IMediator mediator, DeleteRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<object>> Exist(this IMediator mediator, ExistRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> Create(this IMediator mediator, CreateRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> Transfer(this IMediator mediator, TransferRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<CompressResult>> Compress(this IMediator mediator, CompressRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Workflow
    public static Task<Result<object?>> Workflow(this IMediator mediator, WorkflowRequest workflow, CancellationToken cancellationToken)
    {
        return mediator.Send(workflow, cancellationToken);
    }
    #endregion

    #region Version
    public static Task<Result<VersionResponse>> Version(this IMediator mediator, VersionRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Connectors
    public static Task<Result<IEnumerable<object>>> Connectors(this IMediator mediator, ConnectorListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<ConnectorDetailsResponse>> ConnectorDetails(this IMediator mediator, ConnectorDetailsRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Config
    public static Task<Result<IEnumerable<object>>> ConfigList(this IMediator mediator, ConfigListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<ConfigDetailsResponse>> ConfigDetails(this IMediator mediator, ConfigDetailsRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<AddConfigResponse>> AddConfig(this IMediator mediator, AddConfigRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<IEnumerable<DeleteConfigResponse>>> DeleteConfig(this IMediator mediator, DeleteConfigRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Logs
    public static Task<Result<IEnumerable<object>>> Logs(this IMediator mediator, LogsListRequest listRequest, CancellationToken cancellationToken)
    {
        return mediator.Send(listRequest, cancellationToken);
    }
    #endregion
}