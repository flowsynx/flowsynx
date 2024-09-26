using FluentValidation;

namespace FlowSynx.Core.Features.Transfer.Command;

public class TransferValidator : AbstractValidator<TransferRequest>
{
    public TransferValidator()
    {
        RuleFor(request => request.SourceEntity)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CopyValidatorSourceEntityValueShouldNotNullOrEmptyMessage);

        RuleFor(request => request.DestinationEntity)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CopyValidatorDestinationEntityValueShouldNotNullOrEmptyMessage);
    }
}