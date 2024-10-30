using FluentValidation;

namespace FlowSynx.Core.Features.Write.Command;

public class WriteValidator : AbstractValidator<WriteRequest>
{
    public WriteValidator()
    {
        RuleFor(request => request.Data)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.WriteValidatorDataValueShouldNotNullOrEmptyMessage);
    }
}