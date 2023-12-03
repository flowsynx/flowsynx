using FlowSync.Abstractions.Storage;
using FlowSync.Core.Common;
using FluentValidation;

namespace FlowSync.Core.Features.Storage.Size.Query;

public class SizeValidator : AbstractValidator<SizeRequest>
{
    public SizeValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.SizeValidatorPathValueMustNotNullOrEmptyMessage);

        RuleFor(x => x.Kind)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<StorageFilterItemKind>(x, out _))
            .WithMessage(FlowSyncCoreResource.SizeValidatorKindValueMustBeValidMessage);
    }
}