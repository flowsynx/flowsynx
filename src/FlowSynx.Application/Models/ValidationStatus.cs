using System;
using System.Collections.Generic;
using System.Text;

namespace FlowSynx.Application.Models;

public class ValidationStatus
{
    public bool Valid { get; set; }
    public int Score { get; set; } // 0-100
    public string Message { get; set; } = string.Empty;
}