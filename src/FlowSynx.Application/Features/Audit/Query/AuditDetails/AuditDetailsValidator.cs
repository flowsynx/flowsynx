using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.Audit.Query.AuditDetails;

public class AuditDetailsValidator : AbstractValidator<AuditDetailsRequest>
{
    public AuditDetailsValidator(ILocalization localization)
    {
        RuleFor(x => x.AuditId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_AuditId_MustHaveValue"));

        RuleFor(x => x.AuditId)
            .Must(BeAValidGuid)
            .WithMessage(localization.Get("Features_Validation_AuditId_InvalidGuidFormat"));
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}