using FlowSynx.Application.Extensions;
using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Command.DeleteWorkflow;

public class DeleteWorkflowValidator : AbstractValidator<DeleteWorkflowRequest>
{
    public DeleteWorkflowValidator(ILocalization localization)
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_WorkflowId_MustHaveValue"));

        RuleFor(x => x.WorkflowId)
            .MustBeValidGuid(localization.Get("Features_Validation_WorkflowId_InvalidGuidFormat"));
    }
}
