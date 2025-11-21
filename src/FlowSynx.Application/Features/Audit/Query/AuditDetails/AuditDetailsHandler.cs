using MediatR;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Wrapper;
using FlowSynx.Application.Services;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Domain;
using FlowSynx.Domain.Audit;
using FlowSynx.Application.Localizations;

namespace FlowSynx.Application.Features.Audit.Query.AuditDetails;

internal class AuditDetailsHandler : IRequestHandler<AuditDetailsRequest, Result<AuditDetailsResponse>>
{
    private readonly ILogger<AuditDetailsHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalization _localization;

    public AuditDetailsHandler(
        ILogger<AuditDetailsHandler> logger,
        IAuditService auditService, 
        ICurrentUserService currentUserService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _localization = localization;
    }

    /// <summary>
    /// Retrieves audit details for the provided identifier after validating the current user.
    /// </summary>
    /// <param name="request">Audit details request containing the audit id.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    /// <returns>A success result with audit details or failure describing the error.</returns>
    public async Task<Result<AuditDetailsResponse>> Handle(AuditDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            if (string.IsNullOrEmpty(_currentUserService.UserId()))
                throw new FlowSynxException((int)ErrorCode.SecurityAuthenticationIsRequired, 
                    _localization.Get("Authentication_Access_Denied"));

            var auditId = Guid.Parse(request.AuditId);
            var audit = await _auditService.Get(auditId, cancellationToken);
            if (audit is null)
            {
                var message = _localization.Get("Feature_Audit_DetailsNotFound", auditId);
                throw new FlowSynxException((int)ErrorCode.AuditNotFound, message);
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
            _logger.LogInformation("Audit details for '{AuditId}' has been retrieved successfully.", auditId);
            return await Result<AuditDetailsResponse>.SuccessAsync(response);
        }
        catch (FlowSynxException ex)
        {
            _logger.LogError(ex, "FlowSynx exception caught in AuditDetailsHandler for audit '{AuditId}'.", request.AuditId);
            return await Result<AuditDetailsResponse>.FailAsync(ex.ToString());
        }
    }
}
