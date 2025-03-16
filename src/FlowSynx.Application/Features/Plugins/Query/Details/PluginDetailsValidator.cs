using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Query.Details;

public class PluginDetailsValidator : AbstractValidator<PluginDetailsRequest>
{
    public PluginDetailsValidator()
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