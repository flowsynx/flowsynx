using FluentValidation;

namespace FlowSynx.Core.Features.Copy.Command;

public class CopyValidator : AbstractValidator<CopyRequest>
{
    public CopyValidator()
    {
        RuleFor(request => request.SourceEntity)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CopyValidatorSourcePathValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.DestinationEntity)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CopyValidatorDestinationPathValueMustNotNullOrEmptyMessage);
    }
}