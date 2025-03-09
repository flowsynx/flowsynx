using FluentValidation;

namespace FlowSynx.Core.Features.Workflows.Command.Update;

public class UpdateWorkflowValidator : AbstractValidator<UpdateWorkflowRequest>
{
    public UpdateWorkflowValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.ConnectorValidatorConnectorNamespaceValueMustBeValidMessage);
    }
}