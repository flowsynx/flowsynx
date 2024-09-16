//using FlowSynx.Commons;
//using FlowSynx.Logging;
//using FluentValidation;

//namespace FlowSynx.Core.Features.Logs.Query.List;

//public class LogsListValidator : AbstractValidator<LogsListRequest>
//{
//    public LogsListValidator()
//    {
//        RuleFor(x => x.Level)
//            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<LoggingLevel>(x, out _))
//            .WithMessage(Resources.LogsValidatorKindValueMustBeValidMessage);
//    }
//}