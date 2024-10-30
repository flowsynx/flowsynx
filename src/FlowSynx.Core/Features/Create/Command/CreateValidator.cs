using FluentValidation;

namespace FlowSynx.Core.Features.Create.Command;

public class CreateValidator : AbstractValidator<CreateRequest>
{
    public CreateValidator()
    {
        RuleFor(request => request)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.CreateValidatorEntityShouldNotBeNullOrEmptyMessage);
    }
}