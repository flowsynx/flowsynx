using FluentValidation;

namespace FlowSynx.Core.Features.Read.Query;

public class ReadValidator : AbstractValidator<ReadRequest>
{
    public ReadValidator()
    {
        RuleFor(request => request.Entity)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ReadValidatorPathValueMustNotNullOrEmptyMessage);
    }
}