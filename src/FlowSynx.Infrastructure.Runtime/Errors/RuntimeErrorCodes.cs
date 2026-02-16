using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Infrastructure.Runtime.Errors;

public class RuntimeErrorCodes
{
    public static readonly ErrorCode ActivityNotFound = new(500_001, ErrorCategory.Runtime);
    public static readonly ErrorCode ActivityExecutorNotFound = new(500_002, ErrorCategory.Runtime);
}