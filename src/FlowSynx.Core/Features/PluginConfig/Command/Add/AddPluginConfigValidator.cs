using FluentValidation;

namespace FlowSynx.Core.Features.PluginConfig.Command.Add;

public class AddPluginConfigValidator : AbstractValidator<AddPluginConfigRequest>
{
    public AddPluginConfigValidator()
    {
        RuleFor(request => request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorNameValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.Name)
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage(Resources.AddConfigValidatorNameValueOnlyAcceptLatingCharacters);

        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorTypeValueMustNotNullOrEmptyMessage);
    }
}