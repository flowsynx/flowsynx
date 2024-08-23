using FluentValidation;

namespace FlowSynx.Core.Features.Delete.Command;

public class DeleteValidator : AbstractValidator<DeleteRequest>
{
    public DeleteValidator()
    {
        RuleFor(request => request.Entity)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.DeleteValidatorPathValueMustNotNullOrEmptyMessage);
    }
}