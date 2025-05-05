using FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public interface IErrorHandlingStrategyFactory
{
    IErrorHandlingStrategy Create(ErrorHandling? errorHandling);
}