using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Infrastructure.Security.Errors;

public class SecurityErrorCodes
{
    public static readonly ErrorCode NoSecretProviderFound = new(400_001, ErrorCategory.Security);
    public static readonly ErrorCode TenantNotFound = new(400_002, ErrorCategory.Security);
}