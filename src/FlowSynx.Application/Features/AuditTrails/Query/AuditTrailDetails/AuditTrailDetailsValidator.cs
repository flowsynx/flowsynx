using FluentValidation;

namespace FlowSynx.Application.Features.AuditTrails.Query.AuditTrailDetails;

public class AuditTrailDetailsValidator : AbstractValidator<AuditTrailDetailsRequest>
{
    public AuditTrailDetailsValidator()
    {
        RuleFor(x => x.AuditId)
            .NotNull()
            .NotEmpty()
            .WithMessage(ApplicationResources.Features_Validation_AuditId_MustHaveValue);

        RuleFor(x => x.AuditId)
            .GreaterThan(0)
            .WithMessage(ApplicationResources.Features_Validation_AuditId_MustBePositive);
    }
}
