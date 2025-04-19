using FluentValidation;

namespace FlowSynx.Application.Features.PluginConfig.Command.Add;

public class AddPluginConfigValidator : AbstractValidator<AddPluginConfigRequest>
{
    public AddPluginConfigValidator()
    {
        RuleFor(request => request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_PluginConfig_Validation_Name_MustHaveValue);

        RuleFor(request => request.Name)
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$")
            .WithMessage(Resources.Features_PluginConfig_Validation_Name_OnlyAcceptLatingCharacters);

        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_PluginConfig_Validation_Type_MustHaveValue);

        RuleFor(request => request.Version)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_PluginConfig_Validation_Version_MustHaveValue);
    }
}