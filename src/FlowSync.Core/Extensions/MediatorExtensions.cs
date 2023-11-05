using MediatR;
using FlowSync.Core.Wrapper;
using FlowSync.Core.Features.List;

namespace FlowSync.Core.Extensions;

public static class MediatorExtensions
{
    #region Plugins
    public static Task<Result<IEnumerable<ListResponse>>> List(this IMediator mediator, ListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion
}