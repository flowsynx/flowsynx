using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Command.AddWorkflowTrigger;

public class AddWorkflowTriggerValidator : AbstractValidator<AddWorkflowTriggerRequest>
{
    public AddWorkflowTriggerValidator()
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_WorkflowId_MustHaveValue);

        RuleFor(x => x.WorkflowId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_WorkflowId_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}