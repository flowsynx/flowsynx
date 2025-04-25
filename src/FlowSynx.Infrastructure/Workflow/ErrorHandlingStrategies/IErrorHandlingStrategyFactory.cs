using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public interface IErrorHandlingStrategyFactory
{
    IErrorHandlingStrategy Create(ErrorHandling? errorHandling);
}