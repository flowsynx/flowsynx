using FluentValidation;

namespace FlowSynx.Core.Features.Config.Command.Delete;

public class DeleteConfigValidator : AbstractValidator<DeleteConfigRequest>
{
    public DeleteConfigValidator()
    {
        RuleFor(request => request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.DeleteConfigValidatorNameValueMustNotNullOrEmptyMessage);
    }
}