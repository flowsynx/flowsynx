using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Query.PluginDetails;

public class PluginDetailsValidator : AbstractValidator<PluginDetailsRequest>
{
    public PluginDetailsValidator()
    {
        RuleFor(x => x.PluginId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_PluginId_MustHaveValue);

        RuleFor(x => x.PluginId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_PluginId_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}