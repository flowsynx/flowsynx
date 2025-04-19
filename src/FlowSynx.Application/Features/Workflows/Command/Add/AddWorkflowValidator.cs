using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Command.Add;

public class AddWorkflowValidator : AbstractValidator<AddWorkflowRequest>
{
    public AddWorkflowValidator()
    {
        RuleFor(request => request.Definition)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Workflow_Validation_Definition_MustHaveValue);
    }
}