using FlowSynx.Commons;
using FlowSynx.Logging;
using FluentValidation;

namespace FlowSynx.Core.Features.Logs.Query;

public class LogsValidator : AbstractValidator<LogsRequest>
{
    public LogsValidator()
    {
        RuleFor(x => x.Level)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<LoggingLevel>(x, out _))
            .WithMessage(Resources.LogsValidatorKindValueMustBeValidMessage);
    }
}