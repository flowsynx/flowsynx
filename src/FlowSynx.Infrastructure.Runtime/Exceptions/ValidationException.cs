using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.Runtime.Errors;

namespace FlowSynx.Infrastructure.Runtime.Exceptions;

public class ValidationException : RuntimeException
{
    public List<ValidationError> Errors { get; }

    public ValidationException(string message) 
        : base(RuntimeErrorCodes.GeneNotFound, message)
    {
        Errors = new List<ValidationError>();
    }

    public ValidationException(string message, List<ValidationError> errors) 
        : base(RuntimeErrorCodes.GeneNotFound, message)
    {
        Errors = errors;
    }
}