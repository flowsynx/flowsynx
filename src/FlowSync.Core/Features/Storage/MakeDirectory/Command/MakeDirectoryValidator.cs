using FlowSync.Abstractions.Storage;
using FlowSync.Core.Common;
using FluentValidation;

namespace FlowSync.Core.Features.Storage.MakeDirectory.Command;

public class MakeDirectoryValidator : AbstractValidator<MakeDirectoryRequest>
{
    public MakeDirectoryValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ListValidatorPathValueMustNotNullOrEmptyMessage);
    }
}