using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Command.Update;

public class UpdatePluginValidator : AbstractValidator<UpdatePluginRequest>
{
    public UpdatePluginValidator()
    {
        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorNameValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.OldVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorTypeValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.NewVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorTypeValueMustNotNullOrEmptyMessage);
    }
}