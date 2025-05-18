using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionList;

public class WorkflowExecutionListValidator : AbstractValidator<WorkflowExecutionListRequest>
{
    public WorkflowExecutionListValidator(ILocalization localization)
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