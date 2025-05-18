using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Command.AddWorkflowTrigger;

public class AddWorkflowTriggerValidator : AbstractValidator<AddWorkflowTriggerRequest>
{
    public AddWorkflowTriggerValidator(ILocalization localization)
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_WorkflowId_MustHaveValue"));

        RuleFor(x => x.WorkflowId)
            .Must(BeAValidGuid)
            .WithMessage(localization.Get("Features_Validation_WorkflowId_InvalidGuidFormat"));
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}