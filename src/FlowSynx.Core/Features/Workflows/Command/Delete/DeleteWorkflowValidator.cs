using FluentValidation;

namespace FlowSynx.Core.Features.Workflows.Command.Delete;

public class DeleteWorkflowValidator : AbstractValidator<DeleteWorkflowRequest>
{
    public DeleteWorkflowValidator()
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