using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Core.Features.Config.Command.Add;
using FlowSynx.Core.Features.Config.Query.Details;
using FlowSynx.Core.Features.Config.Query.List;
using FlowSynx.Core.Features.Config.Command.Delete;
using FlowSynx.Core.Features.Plugins.Query.Details;
using FlowSynx.Core.Features.Plugins.Query.List;
using FlowSynx.Core.Features.Version.Query;
using FlowSynx.Core.Features.Logs.Query.List;
using FlowSynx.Core.Features.Copy.Command;
using FlowSynx.Core.Features.List.Query;
using FlowSynx.Core.Features.Write.Command;
using FlowSynx.Core.Features.Delete.Command;
using FlowSynx.Core.Features.Exist.Query;
using FlowSynx.Core.Features.PurgeDirectory.Command;
using FlowSynx.Core.Features.DeleteFile.Command;
using FlowSynx.Core.Features.Compress.Command;
using FlowSynx.Core.Features.Check.Command;
using FlowSynx.Core.Features.Read.Query;
using FlowSynx.Core.Features.Size.Query;
using FlowSynx.Core.Features.About.Query;
using FlowSynx.Core.Features.Move.Command;
using FlowSynx.Core.Features.Create.Command;

namespace FlowSynx.Core.Extensions;

public static class MediatorExtensions
{
    #region Storage
    public static Task<Result<object>> About(this IMediator mediator, AboutRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<IEnumerable<object>>> List(this IMediator mediator, ListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<SizeResponse>> Size(this IMediator mediator, SizeRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<object>> Write(this IMediator mediator, WriteRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<object>> Read(this IMediator mediator, ReadRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<IEnumerable<object>>> Delete(this IMediator mediator, DeleteRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<DeleteFileResponse>> DeleteFile(this IMediator mediator, DeleteFileRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<ExistResponse>> Exist(this IMediator mediator, ExistRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<object>> Create(this IMediator mediator, CreateRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<PurgeDirectoryResponse>> PurgeDirectory(this IMediator mediator, PurgeDirectoryRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<CopyResponse>> Copy(this IMediator mediator, CopyRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<MoveResponse>> Move(this IMediator mediator, MoveRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<IEnumerable<CheckResponse>>> Check(this IMediator mediator, CheckRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<CompressResponse>> Compress(this IMediator mediator, CompressRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Version
    public static Task<Result<VersionResponse>> Version(this IMediator mediator, VersionRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Plugins
    public static Task<Result<IEnumerable<PluginsListResponse>>> Plugins(this IMediator mediator, PluginsListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<PluginDetailsResponse>> PluginDetails(this IMediator mediator, PluginDetailsRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Config
    public static Task<Result<IEnumerable<ConfigListResponse>>> ConfigList(this IMediator mediator, ConfigListRequest request, CancellationToken cancellationToken)
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
    public static Task<Result<IEnumerable<LogsListResponse>>> Logs(this IMediator mediator, LogsListRequest listRequest, CancellationToken cancellationToken)
    {
        return mediator.Send(listRequest, cancellationToken);
    }
    #endregion
}