using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Command.DeleteWorkflowTrigger;

public class DeleteWorkflowTriggerValidator : AbstractValidator<DeleteWorkflowTriggerRequest>
{
    public DeleteWorkflowTriggerValidator(ILocalization localization)
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_WorkflowId_MustHaveValue"));

        RuleFor(x => x.WorkflowId)
            .Must(BeAValidGuid)
            .WithMessage(localization.Get("Features_Validation_WorkflowId_InvalidGuidFormat"));

        RuleFor(x => x.TriggerId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_TriggerId_MustHaveValue"));

        RuleFor(x => x.TriggerId)
            .Must(BeAValidGuid)
            .WithMessage(localization.Get("Features_Validation_TriggerId_InvalidGuidFormat"));
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}