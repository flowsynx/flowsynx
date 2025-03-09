using FlowSynx.Core.Features.PluginConfig.Query.List;
using FluentValidation;

namespace FlowSynx.Core.Features.PluginConfig.Command.Delete;

public class DeletePluginConfigValidator : AbstractValidator<DeletePluginConfigRequest>
{
    public DeletePluginConfigValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);
    }
}