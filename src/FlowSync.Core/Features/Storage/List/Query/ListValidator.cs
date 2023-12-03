using FlowSync.Abstractions.Storage;
using FlowSync.Core.Common;
using FluentValidation;

namespace FlowSync.Core.Features.Storage.List.Query;

public class ListValidator : AbstractValidator<ListRequest>
{
    public ListValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ListValidatorPathValueMustNotNullOrEmptyMessage);

        RuleFor(x => x.Kind)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<StorageFilterItemKind>(x, out _))
            .WithMessage(FlowSyncCoreResource.ListValidatorKindValueMustBeValidMessage);
    }
}