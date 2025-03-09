using FluentValidation;

namespace FlowSynx.Core.Features.Workflows.Command.Delete;

public class DeleteWorkflowValidator : AbstractValidator<DeleteWorkflowRequest>
{
    public DeleteWorkflowValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);
    }
}