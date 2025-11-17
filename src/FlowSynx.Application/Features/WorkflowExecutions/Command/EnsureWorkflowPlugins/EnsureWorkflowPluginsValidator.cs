using FlowSynx.Application.Localizations;
using FluentValidation;

namespace FlowSynx.Application.Features.WorkflowExecutions.Command.EnsureWorkflowPlugins;

public class EnsureWorkflowPluginsValidator : AbstractValidator<EnsureWorkflowPluginsRequest>
{
    public EnsureWorkflowPluginsValidator(ILocalization localization)
    {
        RuleFor(x => x.WorkflowId)
            .NotNull()
            .NotEmpty()
            .WithMessage(localization.Get("Features_Validation_WorkflowId_MustHaveValue"));
    }
}
