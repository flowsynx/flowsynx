using FlowSync.Abstractions;
using FlowSync.Abstractions.Entities;
using FlowSync.Core.Common.Utilities;
using FlowSync.Core.Features.List;
using FluentValidation;

namespace FlowSync.Core.Features.Size.Query;

public class SizeValidator : AbstractValidator<SizeRequest>
{
    public SizeValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.SizeValidatorPathValueMustNotNullOrEmptyMessage);

        RuleFor(x => x.Kind)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<FilterItemKind>(x, out _))
            .WithMessage(FlowSyncCoreResource.SizeValidatorKindValueMustBeValidMessage);
    }
}