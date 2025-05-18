using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Command.AddWorkflow;

public class AddWorkflowValidator : AbstractValidator<AddWorkflowRequest>
{
    public AddWorkflowValidator(ILocalization localization)
    {
        RuleFor(request => request.Definition)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Workflow_Validation_Definition_MustHaveValue"));
    }
}