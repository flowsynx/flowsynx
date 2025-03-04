using FlowSynx.Core.Features.PluginConfig.Query.List;
using FluentValidation;

namespace FlowSynx.Core.Features.Config.Command.Delete;

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