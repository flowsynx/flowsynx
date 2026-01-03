using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Interfaces;
using FlowSynx.Application.Core.Dispatcher;

namespace FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailsList;

internal class AuditTrailsListHandler : IActionHandler<AuditTrailsListRequest, PaginatedResult<AuditTrailsListResult>>
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

    public async Task<PaginatedResult<AuditTrailsListResult>> Handle(AuditTrailsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var audits = await _auditTrailRepository.All(cancellationToken);
            var response = audits.Select(audit => new AuditTrailsListResult
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
            return await PaginatedResult<AuditTrailsListResult>.SuccessAsync(
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
            return await PaginatedResult<AuditTrailsListResult>.FailureAsync(ex.ToString());
        }
    }
}
