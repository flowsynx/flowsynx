using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Command.InstallPlugin;

public class AddPluginValidator : AbstractValidator<InstallPluginRequest>
{
    public AddPluginValidator(ILocalization localization)
    {
        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_Plugin_Type_MustHaveValue"));
    }
}