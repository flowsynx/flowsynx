using FluentValidation;

namespace FlowSynx.Application.Features.Audit.Query.Details;

public class AuditDetailsValidator : AbstractValidator<AuditDetailsRequest>
{
    public AuditDetailsValidator()
    {
        RuleFor(x => x.Id)
            .NotNull()
            .NotEmpty()
            .Must(BeAValidGuid)
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}