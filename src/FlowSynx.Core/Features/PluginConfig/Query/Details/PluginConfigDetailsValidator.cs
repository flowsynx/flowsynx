﻿using FluentValidation;

namespace FlowSynx.Core.Features.PluginConfig.Query.Details;

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