﻿using FlowSynx.Domain.Trigger;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class WorkflowTrigger
{
    public WorkflowTriggerType Type { get; set; } = WorkflowTriggerType.Manual;
    public Dictionary<string, object> Properties { get; set; } = new();
}