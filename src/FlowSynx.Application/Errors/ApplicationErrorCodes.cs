using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Application.Errors;

public static class ApplicationErrorCodes
{
    public static readonly ErrorCode AuditTrailNotFound = new(200_101, ErrorCategory.Application);
    public static readonly ErrorCode TenantNotFound = new(200_102, ErrorCategory.Application);
}