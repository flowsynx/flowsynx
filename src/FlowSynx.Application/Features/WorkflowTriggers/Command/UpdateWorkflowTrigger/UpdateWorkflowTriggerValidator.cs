using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Command.UpdateWorkflowTrigger;

public class UpdateWorkflowTriggerValidator : AbstractValidator<UpdateWorkflowTriggerRequest>
{
    public UpdateWorkflowTriggerValidator()
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Id_MustHaveValue);

        RuleFor(x => x.WorkflowId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_Id_InvalidGuidFormat);

        RuleFor(x => x.TriggerId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Id_MustHaveValue);

        RuleFor(x => x.TriggerId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_Id_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}