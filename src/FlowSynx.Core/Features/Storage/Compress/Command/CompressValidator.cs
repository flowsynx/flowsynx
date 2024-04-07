using FlowSynx.Commons;
using FlowSynx.IO.Compression;
using FluentValidation;

namespace FlowSynx.Core.Features.Storage.Compress.Command;

public class CompressValidator : AbstractValidator<CompressRequest>
{
    public CompressValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CopyValidatorSourcePathValueMustNotNullOrEmptyMessage);

        RuleFor(x => x.Kind)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<CompressType>(x, out _))
            .WithMessage(Resources.ListValidatorKindValueMustBeValidMessage);
    }
}