using FluentValidation;

namespace FlowSynx.Core.Features.List.Query;

public class ListValidator : AbstractValidator<ListRequest>
{
    public ListValidator()
    {
        RuleFor(request => request)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ListValidatorEntityValueShouldNotNullOrEmptyMessage);
    }
}