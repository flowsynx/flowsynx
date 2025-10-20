using FlowSynx.Application.Extensions;
using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ApproveWorkflow;

public class ApproveWorkflowValidator : AbstractValidator<ApproveWorkflowRequest>
{
    public ApproveWorkflowValidator(ILocalization localization)
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_WorkflowId_MustHaveValue"));

        RuleFor(x => x.WorkflowExecutionId)
            .MustBeValidGuid(localization.Get("Features_Validation_WorkflowId_InvalidGuidFormat"));

        RuleFor(x => x.WorkflowExecutionApprovalId)
            .MustBeValidGuid(localization.Get("Features_Validation_WorkflowId_InvalidGuidFormat"));
    }
}
