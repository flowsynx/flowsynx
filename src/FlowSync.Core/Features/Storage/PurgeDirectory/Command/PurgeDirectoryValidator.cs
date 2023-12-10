using FlowSync.Abstractions.Storage;
using FlowSync.Core.Common;
using FluentValidation;

namespace FlowSync.Core.Features.Storage.PurgeDirectory.Command;

public class PurgeDirectoryValidator : AbstractValidator<PurgeDirectoryRequest>
{
    public PurgeDirectoryValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ListValidatorPathValueMustNotNullOrEmptyMessage);
    }
}