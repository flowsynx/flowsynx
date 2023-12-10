using FlowSync.Abstractions.Storage;
using FlowSync.Core.Common;
using FluentValidation;

namespace FlowSync.Core.Features.Storage.DeleteFile.Command;

public class DeleteFileValidator : AbstractValidator<DeleteFileRequest>
{
    public DeleteFileValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ListValidatorPathValueMustNotNullOrEmptyMessage);
    }
}