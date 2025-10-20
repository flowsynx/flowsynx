using FlowSynx.Application.Extensions;
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
            .MustBeValidGuid(localization.Get("Features_Validation_AuditId_InvalidGuidFormat"));
    }
}
