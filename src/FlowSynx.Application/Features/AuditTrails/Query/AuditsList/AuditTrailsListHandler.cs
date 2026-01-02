using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Interfaces;

namespace FlowSynx.Application.Features.AuditTrails.Query.AuditTrailsList;

internal class AuditTrailsListHandler : IRequestHandler<AuditTrailsListRequest, PaginatedResult<AuditTrailsListResponse>>
{
    private readonly ILogger<AuditTrailsListHandler> _logger;
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly ICurrentUserService _currentUserService;

    public AuditTrailsListHandler(ILogger<AuditTrailsListHandler> logger, IAuditTrailRepository auditTrailRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditTrailRepository = auditTrailRepository ?? throw new ArgumentNullException(nameof(auditTrailRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<AuditTrailsListResponse>> Handle(AuditTrailsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var audits = await _auditTrailRepository.All(cancellationToken);
            var response = audits.Select(audit => new AuditTrailsListResponse
            {
                Id = audit.Id,
                UserId = audit.UserId,
                EntityName = audit.EntityName,
                Action = audit.Action,
                ChangedColumns = audit.ChangedColumns,
                PrimaryKey = audit.PrimaryKey,
                OldValues = audit.OldValues,
                NewValues = audit.NewValues,
                OccurredAtUtc = audit.OccurredAtUtc
            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            _logger.LogInformation(
                "Audit list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<AuditTrailsListResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in AuditsListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<AuditTrailsListResponse>.FailureAsync(ex.ToString());
        }
    }
}
