using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionDetails;

public class WorkflowTaskExecutionDetailsValidator : AbstractValidator<WorkflowTaskExecutionDetailsRequest>
{
    public WorkflowTaskExecutionDetailsValidator()
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Id_MustHaveValue);

        RuleFor(x => x.WorkflowId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_Id_InvalidGuidFormat);

        RuleFor(x => x.WorkflowExecutionId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Id_MustHaveValue);

        RuleFor(x => x.WorkflowExecutionId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_Id_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}