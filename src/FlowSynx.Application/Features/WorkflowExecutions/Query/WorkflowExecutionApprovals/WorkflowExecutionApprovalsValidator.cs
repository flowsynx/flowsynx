using FlowSynx.Application.Extensions;
using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionApprovals;

public class WorkflowExecutionApprovalsValidator : AbstractValidator<WorkflowExecutionApprovalsRequest>
{
    public WorkflowExecutionApprovalsValidator(ILocalization localization)
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_WorkflowId_MustHaveValue"));

        RuleFor(x => x.WorkflowId)
            .MustBeValidGuid(localization.Get("Features_Validation_WorkflowId_InvalidGuidFormat"));

        RuleFor(x => x.WorkflowExecutionId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_ExecutionId_MustHaveValue"));

        RuleFor(x => x.WorkflowExecutionId)
            .MustBeValidGuid(localization.Get("Features_Validation_ExecutionId_InvalidGuidFormat"));
    }
}
