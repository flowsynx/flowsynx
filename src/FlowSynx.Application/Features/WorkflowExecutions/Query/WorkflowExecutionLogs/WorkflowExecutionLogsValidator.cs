using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionLogs;

public class WorkflowExecutionLogsValidator : AbstractValidator<WorkflowExecutionLogsRequest>
{
    public WorkflowExecutionLogsValidator()
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