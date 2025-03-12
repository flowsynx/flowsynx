using FluentValidation;

namespace FlowSynx.Application.Features.PluginConfig.Command.Update;

public class UpdatePluginConfigValidator : AbstractValidator<UpdatePluginConfigRequest>
{
    public UpdatePluginConfigValidator()
    {
        RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .Must(BeAValidGuid)
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);

        RuleFor(request => request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorNameValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.Name)
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage(Resources.AddConfigValidatorNameValueOnlyAcceptLatingCharacters);

        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorTypeValueMustNotNullOrEmptyMessage);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}