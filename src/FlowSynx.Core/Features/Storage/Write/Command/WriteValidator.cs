using FluentValidation;

namespace FlowSynx.Core.Features.Storage.Write.Command;

public class WriteValidator : AbstractValidator<WriteRequest>
{
    public WriteValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ListValidatorPathValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.Data)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ListValidatorPathValueMustNotNullOrEmptyMessage);
    }
}