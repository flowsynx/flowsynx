using FluentValidation;

namespace FlowSync.Core.Features.Read.Query;

public class ReadValidator : AbstractValidator<ReadRequest>
{
    public ReadValidator()
    {
        RuleFor(request => request.Path)
            .NotNull()
            .NotEmpty()
            .WithMessage(FlowSyncCoreResource.ReadValidatorPathValueMustNotNullOrEmptyMessage);
    }
}