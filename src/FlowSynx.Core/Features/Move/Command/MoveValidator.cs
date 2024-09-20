using FluentValidation;

namespace FlowSynx.Core.Features.Move.Command;

public class MoveValidator : AbstractValidator<MoveRequest>
{
    public MoveValidator()
    {
        RuleFor(request => request.SourceEntity)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.MoveValidatorSourceEntityValueShouldNotNullOrEmptyMessage);

        RuleFor(request => request.DestinationEntity)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.MoveValidatorDestinationEntityValueShouldNotNullOrEmptyMessage);
    }
}