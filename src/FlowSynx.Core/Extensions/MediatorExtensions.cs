using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Core.Features.Config.Command.Add;
using FlowSynx.Core.Features.Config.Query.Details;
using FlowSynx.Core.Features.Config.Query.List;
using FlowSynx.Core.Features.Storage.About.Query;
using FlowSynx.Core.Features.Storage.Copy.Command;
using FlowSynx.Core.Features.Storage.Delete.Command;
using FlowSynx.Core.Features.Storage.DeleteFile.Command;
using FlowSynx.Core.Features.Storage.List.Query;
using FlowSynx.Core.Features.Storage.MakeDirectory.Command;
using FlowSynx.Core.Features.Storage.Move.Command;
using FlowSynx.Core.Features.Storage.PurgeDirectory.Command;
using FlowSynx.Core.Features.Storage.Read.Query;
using FlowSynx.Core.Features.Storage.Size.Query;
using FlowSynx.Core.Features.Storage.Write.Command;
using FlowSynx.Core.Features.Config.Command.Delete;
using FlowSynx.Core.Features.Plugins.Query.Details;
using FlowSynx.Core.Features.Plugins.Query.List;
using FlowSynx.Core.Features.Storage.Check.Command;
using FlowSynx.Core.Features.Storage.Exist.Query;
using FlowSynx.Core.Features.Version.Query;
using FlowSynx.Core.Features.Storage.Compress.Command;
using FlowSynx.Core.Features.Logs.Query.List;

namespace FlowSynx.Core.Extensions;

public static class MediatorExtensions
{
    #region Storage
    public static Task<Result<AboutResponse>> About(this IMediator mediator, AboutRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<IEnumerable<ListResponse>>> List(this IMediator mediator, ListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<SizeResponse>> Size(this IMediator mediator, SizeRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<WriteResponse>> Write(this IMediator mediator, WriteRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<ReadResponse>> Read(this IMediator mediator, ReadRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<DeleteResponse>> Delete(this IMediator mediator, DeleteRequest request, CancellationToken cancellationToken)
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

    public static Task<Result<MakeDirectoryResponse>> MakeDirectory(this IMediator mediator, MakeDirectoryRequest request, CancellationToken cancellationToken)
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