using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Command.Add;

public class AddPluginValidator : AbstractValidator<AddPluginRequest>
{
    public AddPluginValidator()
    {
        RuleFor(request => request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorNameValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.Version)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorTypeValueMustNotNullOrEmptyMessage);
    }
}