using FluentValidation;

namespace FlowSynx.Core.Features.Transfer.Command;

public class TransferValidator : AbstractValidator<TransferRequest>
{
    public TransferValidator()
    {
        RuleFor(request => request.Source)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CopyValidatorSourceEntityValueShouldNotNullOrEmptyMessage);

        RuleFor(request => request.Destination)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CopyValidatorDestinationEntityValueShouldNotNullOrEmptyMessage);
    }
}