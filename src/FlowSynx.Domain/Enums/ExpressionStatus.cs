namespace FlowSynx.Domain.Enums;

public enum ExpressionStatus
{
    Quiescent,          // Gene not active
    Expressing,         // Ribosome active
    Expressed,          // Protein functional
    Skipped,            // Expression skipped
    Dysregulated,       // Abnormal expression
    Terminated          // Expression stopped,
}