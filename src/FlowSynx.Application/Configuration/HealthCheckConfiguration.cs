﻿using FlowSynx.Domain.Entities.Log;

namespace FlowSynx.Application.Configuration;

public class HealthCheckConfiguration
{
    public bool Enabled { get; set; } = true;
}