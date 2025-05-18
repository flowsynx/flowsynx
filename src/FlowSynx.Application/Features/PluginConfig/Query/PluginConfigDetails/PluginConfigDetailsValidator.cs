using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.PluginConfig.Query.PluginConfigDetails;

public class PluginConfigDetailsValidator : AbstractValidator<PluginConfigDetailsRequest>
{
    public PluginConfigDetailsValidator(ILocalization localization)
    {
        RuleFor(x => x.ConfigId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_PluginConfigId_MustHaveValue"));

        RuleFor(x => x.ConfigId)
            .Must(BeAValidGuid)
            .WithMessage(localization.Get("Features_Validation_PluginConfigId_InvalidGuidFormat"));
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}