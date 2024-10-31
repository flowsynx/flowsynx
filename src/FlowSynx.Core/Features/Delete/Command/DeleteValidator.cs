using FluentValidation;

namespace FlowSynx.Core.Features.Delete.Command;

public class DeleteValidator : AbstractValidator<DeleteRequest>
{
    public DeleteValidator()
    {

    }
}