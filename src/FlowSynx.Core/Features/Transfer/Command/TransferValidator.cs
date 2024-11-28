using FlowSynx.Commons;
using FlowSynx.Connectors.Abstractions;
using FluentValidation;

namespace FlowSynx.Core.Features.Transfer.Command;

public class TransferValidator : AbstractValidator<TransferRequest>
{
    public TransferValidator()
    {
        RuleFor(x => x.TransferKind)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<TransferKind>(x, out _))
            .WithMessage(Resources.TransferKindValidatorTypeValueShouldBeValidMessage);
    }
}