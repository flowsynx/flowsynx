using FlowSync.Abstractions.Entities;
using FlowSync.Core.Common.Utilities;
using FluentValidation;

namespace FlowSync.Core.Features.List.Query;

public class ListValidator : AbstractValidator<ListRequest>
{
    public ListValidator()
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