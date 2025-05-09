using FluentValidation;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowTriggerDetails;

public class WorkflowTriggerDetailsValidator : AbstractValidator<WorkflowTriggerDetailsRequest>
{
    public WorkflowTriggerDetailsValidator()
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Id_MustHaveValue);

        RuleFor(x => x.WorkflowId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_Id_InvalidGuidFormat);

        RuleFor(x => x.TriggerId)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.Features_Validation_Id_MustHaveValue);

        RuleFor(x => x.TriggerId)
            .Must(BeAValidGuid)
            .WithMessage(Resources.Features_Validation_Id_InvalidGuidFormat);
    }

    private bool BeAValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}