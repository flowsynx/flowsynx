using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionList;

public class WorkflowExecutionListValidator : AbstractValidator<WorkflowExecutionListRequest>
{
    public WorkflowExecutionListValidator()
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Id_MustHaveValue);

        RuleFor(x => x.WorkflowId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_Id_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}