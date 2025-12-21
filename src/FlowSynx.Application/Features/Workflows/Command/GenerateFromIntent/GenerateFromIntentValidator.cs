using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Command.GenerateFromIntent;

public class GenerateFromIntentValidator : AbstractValidator<GenerateFromIntentRequest>
{
    public GenerateFromIntentValidator(ILocalization localization)
    {
        RuleFor(request => request.Goal)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_WorkflowFromIntent_Goal_MustHaveValue"));

        RuleFor(request => request.SchemaUrl)
            .Must(value => string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out _))
            .WithMessage(localization.Get("Features_WorkflowFromIntent_Validation_SchemaUrl_Invalid"));
    }
}
