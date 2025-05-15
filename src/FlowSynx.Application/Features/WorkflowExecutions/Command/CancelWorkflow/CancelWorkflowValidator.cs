using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.CancelWorkflow;

public class CancelWorkflowValidator : AbstractValidator<CancelWorkflowRequest>
{
    public CancelWorkflowValidator()
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_WorkflowId_MustHaveValue);

        RuleFor(x => x.WorkflowExecutionId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_WorkflowId_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}