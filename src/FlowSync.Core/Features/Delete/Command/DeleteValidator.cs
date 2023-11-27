using FlowSync.Abstractions.Entities;
using FlowSync.Core.Common.Utilities;
using FluentValidation;

namespace FlowSync.Core.Features.Delete.Command;

public class DeleteValidator : AbstractValidator<DeleteRequest>
{
    public DeleteValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ListValidatorPathValueMustNotNullOrEmptyMessage);

        RuleFor(x => x.Kind)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<FilterItemKind>(x, out _))
            .WithMessage(FlowSyncCoreResource.ListValidatorKindValueMustBeValidMessage);
    }
}