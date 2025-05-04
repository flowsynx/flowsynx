using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Audit;

namespace FlowSynx.Application.Features.Audit.Query.Details;

internal class AuditDetailsHandler : IRequestHandler<AuditDetailsRequest, Result<AuditDetailsResponse>>
{
    private readonly ILogger<AuditDetailsHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public AuditDetailsHandler(ILogger<AuditDetailsHandler> logger,
        IAuditService auditService, ICurrentUserService currentUserService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        _logger = logger;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AuditDetailsResponse>> Handle(AuditDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserService.UserId))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, Resources.Authentication_Access_Denied);

            var auditId = Guid.Parse(request.Id);
            var audit = await _auditService.Get(auditId, cancellationToken);
            if (audit is null)
            {
                var message = string.Format(Resources.Feature_Audit_DetailsNotFound, auditId);
                throw new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, message);
            }

            var response = new AuditDetailsResponse
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
            };
            _logger.LogInformation(string.Format(Resources.Feature_Audit_DetailesRetrievedSuccessfully, auditId));
            return await Result<AuditDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<AuditDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}