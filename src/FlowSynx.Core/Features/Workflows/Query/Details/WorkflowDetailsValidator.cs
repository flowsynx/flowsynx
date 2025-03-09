using FluentValidation;

namespace FlowSynx.Core.Features.Workflows.Query.Details;

public class WorkflowDetailsValidator : AbstractValidator<WorkflowDetailsRequest>
{
    public WorkflowDetailsValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);
    }
}