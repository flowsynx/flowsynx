﻿using FlowSynx.Domain.Log;

namespace FlowSynx.Application.Features.Logs.Query.LogsList;

public class LogsListResponse
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public LogsLevel Level { get; set; }
    public DateTime TimeStamp { get; set; }
    public string? Exception { get; set; }
}