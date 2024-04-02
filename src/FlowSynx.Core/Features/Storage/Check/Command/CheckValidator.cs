using FluentValidation;

namespace FlowSynx.Core.Features.Storage.Check.Command;

public class CheckValidator : AbstractValidator<CheckRequest>
{
    public CheckValidator()
    {
        RuleFor(request => request.SourcePath)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CopyValidatorSourcePathValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.DestinationPath)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CopyValidatorDestinationPathValueMustNotNullOrEmptyMessage);
    }
}