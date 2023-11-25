﻿namespace FlowSync.Core.Exceptions;

public class ConfigurationException : FlowSyncBaseException
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, Exception inner) : base(message, inner) { }
}