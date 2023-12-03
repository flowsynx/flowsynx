using FlowSync.Abstractions;
using FlowSync.Core.Common;
using FluentValidation;

namespace FlowSync.Core.Features.Plugins.Query;

public class PluginValidator : AbstractValidator<PluginRequest>
{
    public PluginValidator()
    {
        RuleFor(x => x.Namespace)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<PluginNamespace>(x, out _))
            .WithMessage(FlowSyncCoreResource.ListValidatorKindValueMustBeValidMessage);
    }
}