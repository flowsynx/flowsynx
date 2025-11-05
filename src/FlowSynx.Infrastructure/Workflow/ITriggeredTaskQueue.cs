using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowSynx.Infrastructure.Workflow;

public interface ITriggeredTaskQueue
{
    void Enqueue(Guid workflowExecutionId, string taskName);
    bool TryDequeue(Guid workflowExecutionId, out string taskName);
    void Clear(Guid workflowExecutionId);
}