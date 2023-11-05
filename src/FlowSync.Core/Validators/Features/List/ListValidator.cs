using FlowSync.Abstractions.Entities;
using FlowSync.Core.Enums;
using FlowSync.Core.Features.List;
using FlowSync.Core.Utilities;
using FluentValidation;

namespace FlowSync.Core.Validators.Features.List;

public class ListValidator : AbstractValidator<ListRequest>
{
    public ListValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage("{PropertyName} should be not empty.");

        RuleFor(x => x.Kind)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<FilterItemKind>(x, out _))
            .WithMessage("Kind value must be [ File | Directory | FileAndDirectory ]. By default it is FileAndDirectory.");

        RuleFor(x => x.Output)
            .Must(x => string.IsNullOrEmpty(x) || EnumUtils.TryParseWithMemberName<OutputType>(x, out _))
            .WithMessage("Output value must be [Json|Xml|Yaml]");
    }
}