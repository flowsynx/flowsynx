namespace FlowSynx.Application.Tenancy;

public enum TenantResolutionStatus
{
    Pending = 0,      // Tenant successfully created but not yet active
    Active = 1,       // Tenant is active and operational
    Invalid = 2,      // Tenant identifier is malformed or invalid
    NotFound = 3,     // Tenant does not exist
    Suspended = 4,    // Tenant is temporarily disabled
    Terminated = 5,   // Tenant is permanently disabled (no recovery)
    Forbidden = 6,    // Access to the tenant is forbidden
    Error = 7         // Resolution failed due to system error
}