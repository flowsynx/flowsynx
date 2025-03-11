using FluentValidation;

namespace FlowSynx.Core.Features.Workflows.Query.Details;

public class WorkflowDetailsValidator : AbstractValidator<WorkflowDetailsRequest>
{
    public WorkflowDetailsValidator()
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