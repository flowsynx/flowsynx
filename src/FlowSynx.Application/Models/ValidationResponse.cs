using System;
using System.Collections.Generic;
using System.Text;

namespace FlowSynx.Application.Models;

public class ValidationResponse
{
    public string ApiVersion { get; set; } = "validation/v1";
    public string Kind { get; set; } = "ValidationResponse";
    public ValidationMetadata Metadata { get; set; }
    public ValidationStatus Status { get; set; }
    public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
}