﻿using FluentValidation;

namespace FlowSynx.Core.Features.Storage.DeleteFile.Command;

public class DeleteFileValidator : AbstractValidator<DeleteFileRequest>
{
    public DeleteFileValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ListValidatorPathValueMustNotNullOrEmptyMessage);
    }
}