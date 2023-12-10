using FlowSync.Abstractions.Storage;
using FlowSync.Core.Common;
using FluentValidation;

namespace FlowSync.Core.Features.Storage.Move.Command;

public class MoveValidator : AbstractValidator<MoveRequest>
{
    public MoveValidator()
    {
        RuleFor(request => request.SourcePath)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ListValidatorPathValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.DestinationPath)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ListValidatorPathValueMustNotNullOrEmptyMessage);
    }
}