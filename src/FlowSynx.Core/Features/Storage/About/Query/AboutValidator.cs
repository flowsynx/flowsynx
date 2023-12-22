using FluentValidation;

namespace FlowSynx.Core.Features.Storage.About.Query;

public class AboutValidator : AbstractValidator<AboutRequest>
{
    public AboutValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AboutValidatorPathValueMustNotNullOrEmptyMessage);
    }
}