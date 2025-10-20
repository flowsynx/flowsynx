using FlowSynx.Application.Extensions;
using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowDetails;

public class WorkflowDetailsValidator : AbstractValidator<WorkflowDetailsRequest>
{
    public WorkflowDetailsValidator(ILocalization localization)
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_WorkflowId_MustHaveValue"));

        RuleFor(x => x.WorkflowId)
            .MustBeValidGuid(localization.Get("Features_Validation_WorkflowId_InvalidGuidFormat"));
    }
}
