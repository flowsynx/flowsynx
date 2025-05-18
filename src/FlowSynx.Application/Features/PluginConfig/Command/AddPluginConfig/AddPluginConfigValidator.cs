using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.PluginConfig.Command.AddPluginConfig;

public class AddPluginConfigValidator : AbstractValidator<AddPluginConfigRequest>
{
    public AddPluginConfigValidator(ILocalization localization)
    {
        RuleFor(request => request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_PluginConfig_Name_MustHaveValue"));

        RuleFor(request => request.Name)
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage(localization.Get("Features_Validation_PluginConfig_Name_OnlyAcceptLatingCharacters"));

        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_PluginConfig_Type_MustHaveValue"));

        RuleFor(request => request.Version)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_PluginConfig_Version_MustHaveValue"));
    }
}