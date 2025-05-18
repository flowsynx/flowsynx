using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionDetails;

public class WorkflowExecutionDetailsValidator : AbstractValidator<WorkflowExecutionDetailsRequest>
{
    public WorkflowExecutionDetailsValidator(ILocalization localization)
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_WorkflowId_MustHaveValue"));

        RuleFor(x => x.WorkflowId)
            .Must(BeAValidGuid)
            .WithMessage(localization.Get("Features_Validation_WorkflowId_InvalidGuidFormat"));

        RuleFor(x => x.WorkflowExecutionId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_ExecutionId_MustHaveValue"));

        RuleFor(x => x.WorkflowExecutionId)
            .Must(BeAValidGuid)
            .WithMessage(localization.Get("Features_Validation_ExecutionId_InvalidGuidFormat"));
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}