namespace FlowSynx.Domain.Enums;

public enum ExpressionStatus
{
    Quiescent,          // Gene not active
    Expressing,         // Ribosome active
    Expressed,          // Protein functional
    Dysregulated,       // Abnormal expression
    Terminated          // Expression stopped
}