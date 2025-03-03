using FlowSynx.Core.Features.PluginConfig.Query.List;
using FluentValidation;

namespace FlowSynx.Core.Features.Config.Query.Details;

public class PluginConfigDetailsValidator : AbstractValidator<PluginConfigDetailsRequest>
{
    public PluginConfigDetailsValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);
    }
}