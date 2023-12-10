using MediatR;
using FlowSync.Core.Features.Config.Query.Details;
using FlowSync.Core.Features.Plugins.Query;
using FlowSync.Core.Features.Config.Query.List;
using FlowSync.Core.Common.Models;
using FlowSync.Core.Features.Storage.Version.Query;
using FlowSync.Core.Features.Storage.Delete.Command;
using FlowSync.Core.Features.Storage.About.Query;
using FlowSync.Core.Features.Storage.Copy.Command;
using FlowSync.Core.Features.Storage.DeleteFile.Command;
using FlowSync.Core.Features.Storage.List.Query;
using FlowSync.Core.Features.Storage.MakeDirectory.Command;
using FlowSync.Core.Features.Storage.Move.Command;
using FlowSync.Core.Features.Storage.PurgeDirectory.Command;
using FlowSync.Core.Features.Storage.Size.Query;
using FlowSync.Core.Features.Storage.Read.Query;
using FlowSync.Core.Features.Storage.Write.Command;

namespace FlowSync.Core.Extensions;

public static class MediatorExtensions
{
    #region FileSystem
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
    #endregion

    #region Version
    public static Task<Result<VersionResponse>> Version(this IMediator mediator, VersionRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Plugins
    public static Task<Result<IEnumerable<PluginResponse>>> Plugins(this IMediator mediator, PluginRequest request, CancellationToken cancellationToken)
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
    #endregion
}