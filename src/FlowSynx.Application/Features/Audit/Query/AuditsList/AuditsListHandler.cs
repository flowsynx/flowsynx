using FlowSynx.Application.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Audit;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Application.Features.Audit.Query.AuditsList;

internal class AuditsListHandler : IRequestHandler<AuditsListRequest, PaginatedResult<AuditsListResponse>>
{
    private readonly ILogger<AuditsListHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public AuditsListHandler(ILogger<AuditsListHandler> logger, IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedResult<AuditsListResponse>> Handle(AuditsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var audits = await _auditService.All(cancellationToken);
            var response = audits.Select(audit => new AuditsListResponse
            {
                Id = audit.Id,
                UserId = audit.UserId,
                TableName = audit.TableName,
                Type = audit.Type,
                AffectedColumns = audit.AffectedColumns,
                PrimaryKey = audit.PrimaryKey,
                OldValues = audit.OldValues,
                NewValues = audit.NewValues,
                DateTime = audit.DateTime
            });
            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);
            _logger.LogInformation("The audit list has been retrieved successfully.");
            return await PaginatedResult<AuditsListResponse>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, "FlowSynxException caught while retrieving the audit list.");
            return await PaginatedResult<AuditsListResponse>.FailureAsync(ex.ToString());
        }
    }
}
