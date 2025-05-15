using FluentValidation;

namespace FlowSynx.Application.Features.Audit.Query.AuditDetails;

public class AuditDetailsValidator : AbstractValidator<AuditDetailsRequest>
{
    public AuditDetailsValidator()
    {
        RuleFor(x => x.AuditId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_AuditId_MustHaveValue);

        RuleFor(x => x.AuditId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_AuditId_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}