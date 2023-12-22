using FluentValidation;

namespace FlowSynx.Core.Features.Storage.Move.Command;

public class MoveValidator : AbstractValidator<MoveRequest>
{
    public MoveValidator()
    {
        RuleFor(request => request.SourcePath)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.MoveValidatorSourcePathValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.DestinationPath)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.MoveValidatorDestinationPathValueMustNotNullOrEmptyMessage);
    }
}