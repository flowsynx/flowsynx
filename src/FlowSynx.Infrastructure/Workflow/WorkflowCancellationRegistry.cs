using FlowSynx.Application.Models;
using FlowSynx.Application.Workflow;
using FlowSynx.PluginCore.Exceptions;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowCancellationRegistry : IWorkflowCancellationRegistry
{
    private readonly ConcurrentDictionary<CancellationRegistryKey, CancellationTokenSource> _tokens = new();

    public CancellationToken Register(string userId, Guid workflowId, Guid workflowExecutionId)
    {
        var cts = new CancellationTokenSource();
        _tokens[new CancellationRegistryKey(userId, workflowId, workflowExecutionId)] = cts;
        return cts.Token;
    }

    public void Cancel(string userId, Guid workflowId, Guid workflowExecutionId)
    {
        var key = new CancellationRegistryKey(userId, workflowId, workflowExecutionId);
        if (_tokens.TryGetValue(key, out var cancellationToken))
        {
            cancellationToken.Cancel();
            _tokens.Remove(key, out _);
        }
        else
        {
            throw new FlowSynxException((int)ErrorCode.WorkflowCancellationRegistry,
               string.Format(Resources.Workflow_CancellationRegistry_Execution_NotFound, workflowExecutionId.ToString()));
        }
    }

    public void Remove(string userId, Guid workflowId, Guid workflowExecutionId)
    {
        var key = new CancellationRegistryKey(userId, workflowId, workflowExecutionId);
        if (_tokens.TryGetValue(key, out var cancellationToken))
        {
            _tokens.Remove(key, out _);
        }
        else
        {
            throw new FlowSynxException((int)ErrorCode.WorkflowCancellationRegistry,
                string.Format(Resources.Workflow_CancellationRegistry_Execution_NotFound, workflowExecutionId.ToString()));
        }
    }

    public bool IsRegistered(string userId, Guid workflowId, Guid workflowExecutionId) => 
        _tokens.ContainsKey(new CancellationRegistryKey(userId, workflowId, workflowExecutionId));
}