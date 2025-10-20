using FlowSynx.Application.Extensions;
using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowTriggers.Query.WorkflowTriggerDetails;

public class WorkflowTriggerDetailsValidator : AbstractValidator<WorkflowTriggerDetailsRequest>
{
    public WorkflowTriggerDetailsValidator(ILocalization localization)
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_WorkflowId_MustHaveValue"));

        RuleFor(x => x.WorkflowId)
            .MustBeValidGuid(localization.Get("Features_Validation_WorkflowId_InvalidGuidFormat"));

        RuleFor(x => x.TriggerId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_TriggerId_MustHaveValue"));

        RuleFor(x => x.TriggerId)
            .MustBeValidGuid(localization.Get("Features_Validation_TriggerId_InvalidGuidFormat"));
    }
}
