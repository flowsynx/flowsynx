﻿using FlowSynx.Commons;
using FlowSynx.IO.Compression;
using FluentValidation;

namespace FlowSynx.Core.Features.Compress.Command;

public class CompressValidator : AbstractValidator<CompressRequest>
{
    public CompressValidator()
    {
        RuleFor(x => x.CompressType)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<CompressType>(x, out _))
            .WithMessage(Resources.CompressValidatorTypeValueShouldBeValidMessage);
    }
}