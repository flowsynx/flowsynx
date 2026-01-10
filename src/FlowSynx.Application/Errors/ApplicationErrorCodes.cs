using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Application.Errors;

public static class ApplicationErrorCodes
{
    public static readonly ErrorCode AuditTrailNotFound = new(200_101, ErrorCategory.Application);
}