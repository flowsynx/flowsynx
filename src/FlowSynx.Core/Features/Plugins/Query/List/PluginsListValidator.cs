using FlowSynx.Commons;
using FlowSynx.Plugin.Abstractions;
using FluentValidation;

namespace FlowSynx.Core.Features.Plugins.Query.List;

public class PluginsListByNamespaceValidator : AbstractValidator<PluginsListRequest>
{
    public PluginsListByNamespaceValidator()
    {
        RuleFor(x => x.Namespace)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<PluginNamespace>(x, out _))
            .WithMessage(Resources.PluginValidatorPluginNamespaceValueMustBeValidMessage);
    }
}