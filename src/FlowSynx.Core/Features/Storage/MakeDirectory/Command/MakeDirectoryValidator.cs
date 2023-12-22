using FluentValidation;

namespace FlowSynx.Core.Features.Storage.MakeDirectory.Command;

public class MakeDirectoryValidator : AbstractValidator<MakeDirectoryRequest>
{
    public MakeDirectoryValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.MakeDirectoryValidatorPathValueMustNotNullOrEmptyMessage);
    }
}