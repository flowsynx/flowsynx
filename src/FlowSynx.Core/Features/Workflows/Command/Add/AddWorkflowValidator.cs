using FluentValidation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Core.Features.Workflows.Command.Add;

public class AddWorkflowValidator : AbstractValidator<AddWorkflowRequest>
{
    public AddWorkflowValidator()
    {
        RuleFor(request => request.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorNameValueMustNotNullOrEmptyMessage);
    }
}