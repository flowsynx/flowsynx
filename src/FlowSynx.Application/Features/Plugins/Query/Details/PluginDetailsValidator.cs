using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Query.Details;

public class PluginDetailsValidator : AbstractValidator<PluginDetailsRequest>
{
    public PluginDetailsValidator()
    {
        RuleFor(x => x.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);
    }
}