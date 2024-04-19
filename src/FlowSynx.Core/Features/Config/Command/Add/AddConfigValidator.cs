using FlowSynx.Plugin.Abstractions;
using FluentValidation;
using FlowSynx.Core.Parers.Specifications;

namespace FlowSynx.Core.Features.Config.Command.Add;

public class AddConfigValidator : AbstractValidator<AddConfigRequest>
{
    private readonly IPluginsManager _pluginsManager;
    private readonly ISpecificationsParser _specificationsParser;

    public AddConfigValidator(IPluginsManager pluginsManager, ISpecificationsParser specificationsParser)
    {
        _pluginsManager = pluginsManager;
        _specificationsParser = specificationsParser;
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

        RuleFor(request => request.Specifications)
            .Custom(IsSpecificationsValid);
    }

    private bool IsTypeValid(string type)
    {
        return _pluginsManager.IsExist(type);
    }

    private void IsSpecificationsValid(Dictionary<string, string?>? specifications, ValidationContext<AddConfigRequest> context)
    {
        var result = _specificationsParser.Parse(context.InstanceToValidate.Type, specifications);
        if (!result.Valid)
        {
            context.AddFailure(result.Message);
        }
    }
}