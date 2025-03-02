﻿namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowTaskRetry
{
    public int? Max { get; set; } = 3;
    public int? Delay { get; set; } = 1000;
}