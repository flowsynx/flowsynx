using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Application.Errors;

public static class ApplicationErrorCodes
{
    public static readonly ErrorCode AuditTrailNotFound = new(200_101, ErrorCategory.Application);
    public static readonly ErrorCode TenantNotFound = new(200_102, ErrorCategory.Application);
    public static readonly ErrorCode TenantAlreadyExists = new(200_103, ErrorCategory.Application);
    public static readonly ErrorCode TenantInvalidStatus = new(200_104, ErrorCategory.Application);
}