namespace FlowSynx.BuildingBlocks.Errors;

public enum ErrorCategory
{
    Unknown = 0,
    System,
    Domain,
    Application,
    Infrastructure,
    Persistence,
    Security,
    Runtime,
    Serializations
}