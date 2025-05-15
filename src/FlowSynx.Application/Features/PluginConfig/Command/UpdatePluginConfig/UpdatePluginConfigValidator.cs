using FluentValidation;

namespace FlowSynx.Application.Features.PluginConfig.Command.UpdatePluginConfig;

public class UpdatePluginConfigValidator : AbstractValidator<UpdatePluginConfigRequest>
{
    public UpdatePluginConfigValidator()
    {
        RuleFor(x => x.ConfigId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_PluginConfigId_MustHaveValue);

        RuleFor(x => x.ConfigId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_PluginConfigId_InvalidGuidFormat);

        RuleFor(request => request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_PluginConfig_Name_MustHaveValue);

        RuleFor(request => request.Name)
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage(Resources.Features_Validation_PluginConfig_Name_OnlyAcceptLatingCharacters);

        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_PluginConfig_Type_MustHaveValue);

        RuleFor(request => request.Version)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_PluginConfig_Version_MustHaveValue);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}