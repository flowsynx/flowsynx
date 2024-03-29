﻿using FluentValidation;

namespace FlowSynx.Core.Features.Storage.Read.Query;

public class ReadValidator : AbstractValidator<ReadRequest>
{
    public ReadValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ReadValidatorPathValueMustNotNullOrEmptyMessage);
    }
}