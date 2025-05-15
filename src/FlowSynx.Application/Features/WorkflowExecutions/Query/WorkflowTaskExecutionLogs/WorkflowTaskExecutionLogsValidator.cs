using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionLogs;

public class WorkflowTaskExecutionLogsValidator : AbstractValidator<WorkflowTaskExecutionLogsRequest>
{
    public WorkflowTaskExecutionLogsValidator()
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_WorkflowId_MustHaveValue);

        RuleFor(x => x.WorkflowId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_WorkflowId_InvalidGuidFormat);

        RuleFor(x => x.WorkflowExecutionId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_ExecutionId_MustHaveValue);

        RuleFor(x => x.WorkflowExecutionId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_ExecutionId_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}