using FlowSynx.Domain.Exceptions;
using FluentValidation.Results;

namespace FlowSynx.Application.Exceptions;

public class InputValidationException : BaseException
{
    public InputValidationException() : base(Resources.InputValidationExceptionBaseMessage)
    {
        Errors = new List<string>();
    }

    public List<string> Errors { get; }
    public InputValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        foreach (var failure in failures)
        {
            Errors.Add(failure.ErrorMessage);
        }
    }

}