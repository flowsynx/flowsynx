﻿namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public BackoffStrategy BackoffStrategy { get; set; } = Execute.BackoffStrategy.Fixed;
    public int InitialDelay { get; set; } = 1000;    // In millisecond
    public int MaxDelay { get; set; } = 10000;  // In millisecond
}