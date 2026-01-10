using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Domain.Errors;

public static class DomainErrorCodes
{
    public static readonly ErrorCode GeneInstanceIdExists = new(100_101, ErrorCategory.Domain);
    public static readonly ErrorCode GeneInstanceIdNotFound = new(100_102, ErrorCategory.Domain);
    public static readonly ErrorCode GeneBlueprintGenerationRequired = new(100_103, ErrorCategory.Domain);
    public static readonly ErrorCode GeneBlueprintPhenotypicRequired = new(100_104, ErrorCategory.Domain);
    public static readonly ErrorCode GeneBlueprintAnnotationRequired = new(100_105, ErrorCategory.Domain);
    public static readonly ErrorCode GeneBlueprintExpressedProteinRequired = new(100_106, ErrorCategory.Domain);

    public static readonly ErrorCode ChromosomeIdExists = new(100_207, ErrorCategory.Domain);
    public static readonly ErrorCode ChromosomeIdNotFound = new(100_208, ErrorCategory.Domain);

    public static readonly ErrorCode TenantIdRequired = new(100_300, ErrorCategory.Domain);
    public static readonly ErrorCode TenantNameRequired = new(100_301, ErrorCategory.Domain);
    public static readonly ErrorCode TenantNameTooShort = new(100_302, ErrorCategory.Domain);
    public static readonly ErrorCode TenantNameTooLong = new(100_303, ErrorCategory.Domain);
    public static readonly ErrorCode TenantEmailRequired = new(100_304, ErrorCategory.Domain);
    public static readonly ErrorCode TenantEmailInvalid = new(100_305, ErrorCategory.Domain);
    public static readonly ErrorCode TenantCacheDurationOutOfRange = new(100_306, ErrorCategory.Domain);
    public static readonly ErrorCode TenantSuspensionReasonRequired = new(100_307, ErrorCategory.Domain);
    public static readonly ErrorCode TenantTerminationReasonRequired = new(100_308, ErrorCategory.Domain);
    public static readonly ErrorCode TenantSecretKeyAlreadyExists = new(100_309, ErrorCategory.Domain);
    public static readonly ErrorCode TenantContactEmailRequired = new(100_310, ErrorCategory.Domain);
    public static readonly ErrorCode TenantContactEmailAlreadyExists = new(100_311, ErrorCategory.Domain);
}