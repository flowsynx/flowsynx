using System;
using System.Collections.Generic;
using System.Text;

namespace FlowSynx.Application.Models;

public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }
    public string Code { get; set; }
    public string Severity { get; set; } // "error", "fatal"
}