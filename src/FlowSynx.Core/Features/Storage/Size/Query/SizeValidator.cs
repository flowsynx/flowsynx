using FlowSynx.Commons;
using FlowSynx.Plugin.Storage;
using FluentValidation;

namespace FlowSynx.Core.Features.Storage.Size.Query;

public class SizeValidator : AbstractValidator<SizeRequest>
{
    public SizeValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.SizeValidatorPathValueMustNotNullOrEmptyMessage);

        RuleFor(x => x.Kind)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<StorageFilterItemKind>(x, out _))
            .WithMessage(Resources.SizeValidatorKindValueMustBeValidMessage);
    }
}