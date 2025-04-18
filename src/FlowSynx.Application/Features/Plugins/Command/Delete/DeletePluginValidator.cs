using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Command.Delete;

public class DeletePluginValidator : AbstractValidator<DeletePluginRequest>
{
    public DeletePluginValidator()
    {
        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorNameValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.Version)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorTypeValueMustNotNullOrEmptyMessage);
    }
}