using FluentValidation;

namespace FlowSynx.Core.Features.Read.Query;

public class ReadValidator : AbstractValidator<ReadRequest>
{
    public ReadValidator()
    {
        RuleFor(request => request)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ReadValidatorEntityValueShouldNotNullOrEmptyMessage);
    }
}