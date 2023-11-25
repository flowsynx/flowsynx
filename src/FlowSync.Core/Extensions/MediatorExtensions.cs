using MediatR;
using FlowSync.Core.Features.List.Query;
using FlowSync.Core.Features.Size.Query;
using FlowSync.Core.Features.Version.Query;
using FlowSync.Core.Features.About.Query;
using FlowSync.Core.Features.Config.Query.Details;
using FlowSync.Core.Features.Plugins.Query;
using FlowSync.Core.Features.Config.Query.List;
using FlowSync.Core.Common.Models;
using FlowSync.Core.Features.Read.Query;

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

    public static Task<Result<ReadResponse>> Read(this IMediator mediator, ReadRequest request, CancellationToken cancellationToken)
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