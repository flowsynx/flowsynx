using FluentValidation;

namespace FlowSynx.Application.Features.PluginConfig.Command.DeletePluginConfig;

public class DeletePluginConfigValidator : AbstractValidator<DeletePluginConfigRequest>
{
    public DeletePluginConfigValidator()
    {
        RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Id_MustHaveValue);

        RuleFor(x => x.Id)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_Id_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}