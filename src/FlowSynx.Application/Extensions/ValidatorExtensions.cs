using FluentValidation;

namespace FlowSynx.Application.Extensions;

public static class ValidatorExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidGuid<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        string message)
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);
        ArgumentNullException.ThrowIfNull(message);

        return ruleBuilder
            .Must(id => Guid.TryParse(id, out _))
            .WithMessage(message);
    }
}

