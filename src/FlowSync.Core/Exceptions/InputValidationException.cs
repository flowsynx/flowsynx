using FluentValidation.Results;

namespace FlowSync.Core.Exceptions;

public class InputValidationException : FlowSyncBaseException
{
    public InputValidationException() : base(FlowSyncCoreResource.InputValidationExceptionBaseMessage)
    {
        Errors = new List<string>();
    }
    public List<string> Errors { get; }
    public InputValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        foreach (var failure in failures)
        {
            Errors.Add(failure.ErrorMessage);
        }
    }

}