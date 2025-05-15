using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Command.InstallPlugin;

public class AddPluginValidator : AbstractValidator<InstallPluginRequest>
{
    public AddPluginValidator()
    {
        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Plugin_Type_MustHaveValue);

        RuleFor(request => request.Version)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Plugin_Version_MustHaveValue);
    }
}