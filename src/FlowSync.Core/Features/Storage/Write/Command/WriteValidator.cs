using FlowSync.Abstractions.Storage;
using FlowSync.Core.Common;
using FluentValidation;

namespace FlowSync.Core.Features.Storage.Write.Command;

public class WriteValidator : AbstractValidator<WriteRequest>
{
    public WriteValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ListValidatorPathValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.Data)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ListValidatorPathValueMustNotNullOrEmptyMessage);
    }
}