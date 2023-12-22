using FluentValidation;

namespace FlowSynx.Core.Features.Storage.Copy.Command;

public class CopyValidator : AbstractValidator<CopyRequest>
{
    public CopyValidator()
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