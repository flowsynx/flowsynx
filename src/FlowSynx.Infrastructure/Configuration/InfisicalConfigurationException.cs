using System;

namespace FlowSynx.Infrastructure.Configuration;

/// <summary>
/// Represents errors that occur while loading Infisical configuration.
/// </summary>
public sealed class InfisicalConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InfisicalConfigurationException"/> class.
    /// </summary>
    public InfisicalConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InfisicalConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InfisicalConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InfisicalConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public InfisicalConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
