using FluentValidation;

namespace FlowSynx.Core.Features.Plugins.Query.Details;

public class PluginDetailsValidator : AbstractValidator<PluginDetailsRequest>
{
    public PluginDetailsValidator()
    {
        RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.PluginValidatorPluginNamespaceValueMustBeValidMessage);
    }
}