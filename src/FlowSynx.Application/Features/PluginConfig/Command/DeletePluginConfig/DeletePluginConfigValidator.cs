using FluentValidation;

namespace FlowSynx.Application.Features.PluginConfig.Command.DeletePluginConfig;

public class DeletePluginConfigValidator : AbstractValidator<DeletePluginConfigRequest>
{
    public DeletePluginConfigValidator()
    {
        RuleFor(x => x.ConfigId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_PluginConfigId_MustHaveValue);

        RuleFor(x => x.ConfigId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_PluginConfigId_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}