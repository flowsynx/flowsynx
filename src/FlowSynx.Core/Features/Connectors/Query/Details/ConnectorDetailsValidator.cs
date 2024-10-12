using FluentValidation;

namespace FlowSynx.Core.Features.Connectors.Query.Details;

public class ConnectorDetailsValidator : AbstractValidator<ConnectorDetailsRequest>
{
    public ConnectorDetailsValidator()
    {
        RuleFor(x => x.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);
    }
}