using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Audit;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Application.Features.Audit.Query.AuditsList;

internal class AuditsListHandler : IRequestHandler<AuditsListRequest, Result<IEnumerable<AuditsListResponse>>>
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

    public async Task<Result<IEnumerable<AuditsListResponse>>> Handle(AuditsListRequest request, CancellationToken cancellationToken)
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
            }).ToList();
            _logger.LogInformation("The audit list has been retrieved successfully.");
            return await Result<IEnumerable<AuditsListResponse>>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<IEnumerable<AuditsListResponse>>.FailAsync(ex.ToString());
        }
    }
}