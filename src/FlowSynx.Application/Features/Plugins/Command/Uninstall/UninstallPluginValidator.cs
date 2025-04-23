using FluentValidation;

namespace FlowSynx.Application.Features.Plugins.Command.Uninstall;

public class UninstallPluginValidator : AbstractValidator<UninstallPluginRequest>
{
    public UninstallPluginValidator()
    {
        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Plugin_Validation_Type_MustHaveValue);

        RuleFor(request => request.Version)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Plugin_Validation_Version_MustHaveValue);
    }
}