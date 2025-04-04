﻿using FluentValidation;

namespace FlowSynx.Application.Features.PluginConfig.Query.Details;

public class PluginConfigDetailsValidator : AbstractValidator<PluginConfigDetailsRequest>
{
    public PluginConfigDetailsValidator()
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