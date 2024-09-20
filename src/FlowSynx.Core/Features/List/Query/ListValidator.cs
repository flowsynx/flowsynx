using FlowSynx.Commons;
using FlowSynx.Plugin.Storage.Abstractions;
using FluentValidation;

namespace FlowSynx.Core.Features.List.Query;

public class ListValidator : AbstractValidator<ListRequest>
{
    public ListValidator()
    {
        RuleFor(request => request.Entity)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ListValidatorEntityValueShouldNotNullOrEmptyMessage);
    }
}