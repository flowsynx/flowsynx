using FluentValidation;

namespace FlowSynx.Application.Features.PluginConfig.Command.Delete;

public class DeletePluginConfigValidator : AbstractValidator<DeletePluginConfigRequest>
{
    public DeletePluginConfigValidator()
    {
        RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .Must(BeAValidGuid)
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}