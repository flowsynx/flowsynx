using FlowSynx.Application.Extensions;
using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Query.PluginDetails;

public class PluginDetailsValidator : AbstractValidator<PluginDetailsRequest>
{
    public PluginDetailsValidator(ILocalization localization)
    {
        RuleFor(x => x.PluginId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_PluginId_MustHaveValue"));

        RuleFor(x => x.PluginId)
            .MustBeValidGuid(localization.Get("Features_Validation_PluginId_InvalidGuidFormat"));
    }
}
