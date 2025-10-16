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

        RuleFor(request => request.SchemaUrl)
            .Must(value => string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out _))
            .WithMessage(localization.Get("Features_Workflow_Validation_SchemaUrl_Invalid"));
    }
}
