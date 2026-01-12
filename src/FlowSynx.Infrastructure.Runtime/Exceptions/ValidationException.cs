using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.Runtime.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowSynx.Infrastructure.Runtime.Exceptions;

public class ValidationException : RuntimeException
{
    public List<ValidationError> Errors { get; }

    public ValidationException(string message) 
        : base(RuntimeErrorCodes.GenBlueprintNotFound, message)
    {
        Errors = new List<ValidationError>();
    }

    public ValidationException(string message, List<ValidationError> errors) 
        : base(RuntimeErrorCodes.GenBlueprintNotFound, message)
    {
        Errors = errors;
    }
}