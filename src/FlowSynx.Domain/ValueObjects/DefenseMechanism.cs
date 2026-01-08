namespace FlowSynx.Domain.ValueObjects;

public record DefenseMechanism(
    string ResponsePattern = "propagate",           // "apoptosis", "quarantine", "regenerate"
    int MaximumMutationAttempts = 3,                // Max retry attempts
    int RecoveryLatency = 100,                      // Delay between attempts
    string AlternateExpressionPath = null,          // Fallback gene expression
    string HomeostaticCheck = null                  // Health monitoring
);