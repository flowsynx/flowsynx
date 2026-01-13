using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Errors;

public class SystemErrorCodes
{
    public static readonly ErrorCode StartArgumentIsRequired = new(900_001, ErrorCategory.System);
    public static readonly ErrorCode AuthenticationRequired = new(900_002, ErrorCategory.System);
}