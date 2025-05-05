using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Command.UpdatePlugin;

public class UpdatePluginValidator : AbstractValidator<UpdatePluginRequest>
{
    public UpdatePluginValidator()
    {
        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Plugin_Validation_Type_MustHaveValue);

        RuleFor(request => request.OldVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Plugin_Validation_OldVersion_MustHaveValue);

        RuleFor(request => request.NewVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Plugin_Validation_NewVersion_MustHaveValue);
    }
}