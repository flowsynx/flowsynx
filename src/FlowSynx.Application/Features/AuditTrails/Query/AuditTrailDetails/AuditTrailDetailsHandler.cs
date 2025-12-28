using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Application.Features.AuditTrails.Query.AuditTrailDetails;

internal class AuditTrailDetailsHandler : IRequestHandler<AuditTrailDetailsRequest, Result<AuditTrailDetailsResponse>>
{
    private readonly ILogger<AuditTrailDetailsHandler> _logger;
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly ICurrentUserService _currentUserService;

    public AuditTrailDetailsHandler(
        ILogger<AuditTrailDetailsHandler> logger,
        IAuditTrailRepository auditTrailRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditTrailRepository = auditTrailRepository ?? throw new ArgumentNullException(nameof(auditTrailRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<AuditTrailDetailsResponse>> Handle(AuditTrailDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var auditId = Guid.Parse(request.AuditId);
            var audit = await _auditTrailRepository.Get(auditId, cancellationToken);
            //if (audit is null)
            //{
            //    var message = _localization.Get("Feature_Audit_DetailsNotFound", auditId);
            //    throw new FlowSynxException((int)ErrorCode.AuditNotFound, message);
            //}

            var response = new AuditTrailDetailsResponse
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
            _logger.LogInformation("Audit details for '{AuditId}' has been retrieved successfully.", auditId);
            return await Result<AuditTrailDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, "FlowSynx exception caught in AuditTrailDetailsHandler for audit '{AuditId}'.", request.AuditId);
            return await Result<AuditTrailDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}
