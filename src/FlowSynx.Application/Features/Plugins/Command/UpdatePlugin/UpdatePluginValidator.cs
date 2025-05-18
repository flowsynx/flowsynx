using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Command.UpdatePlugin;

public class UpdatePluginValidator : AbstractValidator<UpdatePluginRequest>
{
    public UpdatePluginValidator(ILocalization localization)
    {
        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_Plugin_Type_MustHaveValue"));

        RuleFor(request => request.OldVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_Plugin_OldVersion_MustHaveValue"));

        RuleFor(request => request.NewVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_Plugin_NewVersion_MustHaveValue"));
    }
}