using FluentValidation;

namespace FlowSynx.Application.Features.Tenants.Requests.TenantsDetails;

public class TenantDetailsValidator : AbstractValidator<TenantDetailsRequest>
{
    public TenantDetailsValidator()
    {
        RuleFor(x => x.TenantId)
            .NotNull()
            .NotEmpty()
            .WithMessage(ApplicationResources.Features_Validation_TenantId_MustHaveValue);
    }
}
