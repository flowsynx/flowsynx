using System;
using FlowSynx.Application.Configuration;
using FlowSynx.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace FlowSynx.UnitTests.Configuration;

public class ConfigurationSourceSelectorTests
{
    [Fact]
    public void Resolve_UsesCommandLineArgumentWhenPresent()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var option = ConfigurationSourceSelector.Resolve(
            new[] { "--config-source=Infisical" },
            configuration);

        Assert.Equal(ConfigurationSourceOption.Infisical, option);
    }

    [Fact]
    public void Resolve_UsesEnvironmentVariableAsFallback()
    {
        var configuration = new ConfigurationBuilder().Build();
        var originalValue = Environment.GetEnvironmentVariable(ConfigurationSourceSelector.EnvironmentVariableKey);

        try
        {
            Environment.SetEnvironmentVariable(ConfigurationSourceSelector.EnvironmentVariableKey, "Infisical");

            var option = ConfigurationSourceSelector.Resolve(Array.Empty<string>(), configuration);

            Assert.Equal(ConfigurationSourceOption.Infisical, option);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ConfigurationSourceSelector.EnvironmentVariableKey, originalValue);
        }
    }

    [Fact]
    public void Resolve_DefaultsToAppSettingsWhenNoOverrides()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var option = ConfigurationSourceSelector.Resolve(Array.Empty<string>(), configuration);

        Assert.Equal(ConfigurationSourceOption.AppSettings, option);
    }
}
