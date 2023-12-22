using FlowSynx.Plugin.Abstractions;
using FluentValidation;

namespace FlowSynx.Core.Features.Config.Command.Add;

public class AddConfigValidator : AbstractValidator<AddConfigRequest>
{
    private readonly IPluginsManager _pluginsManager;

    public AddConfigValidator(IPluginsManager pluginsManager)
    {
        _pluginsManager = pluginsManager;
        RuleFor(request => request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorNameValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorTypeValueMustNotNullOrEmptyMessage);

        RuleFor(request => request.Type)
            .Must(IsTypeValid)
            .WithMessage(Resources.AddConfigValidatorTypeValueIsNotValid);
    }

    private bool IsTypeValid(string type)
    {
        return _pluginsManager.IsExist(type);
    }
}