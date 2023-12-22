using FluentValidation;

namespace FlowSynx.Core.Features.Storage.PurgeDirectory.Command;

public class PurgeDirectoryValidator : AbstractValidator<PurgeDirectoryRequest>
{
    public PurgeDirectoryValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.PurgeDirectoryValidatorPathValueMustNotNullOrEmptyMessage);
    }
}