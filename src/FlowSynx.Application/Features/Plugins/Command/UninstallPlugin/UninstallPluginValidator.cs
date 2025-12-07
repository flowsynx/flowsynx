using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Command.UninstallPlugin;

public class UninstallPluginValidator : AbstractValidator<UninstallPluginRequest>
{
    public UninstallPluginValidator(ILocalization localization)
    {
        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_Plugin_Type_MustHaveValue"));
    }
}