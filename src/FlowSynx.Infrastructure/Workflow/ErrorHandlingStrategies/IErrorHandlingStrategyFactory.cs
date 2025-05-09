using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public interface IErrorHandlingStrategyFactory
{
    IErrorHandlingStrategy Create(ErrorHandling? errorHandling);
}