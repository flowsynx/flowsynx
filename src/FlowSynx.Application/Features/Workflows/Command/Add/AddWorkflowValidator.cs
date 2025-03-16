using FluentValidation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Application.Features.Workflows.Command.Add;

public class AddWorkflowValidator : AbstractValidator<AddWorkflowRequest>
{
    public AddWorkflowValidator()
    {
        RuleFor(request => request.Definition)
            .NotNull()
            .NotEmpty()
            .WithMessage(Resources.AddConfigValidatorNameValueMustNotNullOrEmptyMessage);
    }
}