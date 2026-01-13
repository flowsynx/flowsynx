using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Infrastructure.Serializations.Json.Errors;

public class SerializationErrorCodes
{
    public static readonly ErrorCode JsonSerializationInputRequired = new(600_001, ErrorCategory.Serializations);
    public static readonly ErrorCode JsonSerializationFailed = new(600_002, ErrorCategory.Serializations);
}