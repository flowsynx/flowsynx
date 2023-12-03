﻿using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Infrastructure.Exceptions;

public class SizeParserException : FlowSyncBaseException
{
    public SizeParserException(string message) : base(message) { }
    public SizeParserException(string message, Exception inner) : base(message, inner) { }
}