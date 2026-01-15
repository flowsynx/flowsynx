using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Infrastructure.Runtime.Errors;

public class RuntimeErrorCodes
{
    public static readonly ErrorCode GeneNotFound = new(500_001, ErrorCategory.Runtime);
    public static readonly ErrorCode GenExpressorNotFound = new(500_002, ErrorCategory.Runtime);
}