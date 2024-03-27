using FluentValidation;

namespace FlowSynx.Core.Features.Storage.Exist.Query;

public class ExistValidator : AbstractValidator<ExistRequest>
{
    public ExistValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ListValidatorPathValueMustNotNullOrEmptyMessage);
    }
}