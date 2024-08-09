using FlowSynx.Commons;
using FlowSynx.Plugin.Storage.Abstractions;
using FluentValidation;

namespace FlowSynx.Core.Features.Storage.List.Query;

public class ListValidator : AbstractValidator<ListRequest>
{
    public ListValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ListValidatorPathValueMustNotNullOrEmptyMessage);

        RuleFor(x => x.Kind)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<StorageFilterItemKind>(x, out _))
            .WithMessage(Resources.ListValidatorKindValueMustBeValidMessage);
    }
}