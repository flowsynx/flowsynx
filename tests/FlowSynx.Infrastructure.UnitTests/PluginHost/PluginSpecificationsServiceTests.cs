using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.Domain.Plugin;
using FlowSynx.Application.Localizations;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace FlowSynx.Infrastructure.UnitTests.PluginHost;

public class PluginSpecificationsServiceTests
{

    private readonly PluginSpecificationsService _service;

    public PluginSpecificationsServiceTests()
    {
        var logger = new Mock<ILogger<PluginSpecificationsService>>().Object;
        var localization = new Mock<ILocalization>().Object;

        _service = new PluginSpecificationsService(logger, localization);
    }

    [Fact]
    public void Validate_ShouldBeCultureInvariant()
    {
        var originalCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

        // Simulate User Input
        var inputSpecs = new Dictionary<string, object?>
        {
            { "input", "some value" }
        };

        // Required Plugin Specifications
        var pluginSpecs = new List<PluginSpecification>
        {
            new PluginSpecification
            {
                Name = "INPUT",
                IsRequired = true,
                Type = "SomeType"
            }
        };

        // Testing
        try
        {
            var result = _service.Validate(inputSpecs, pluginSpecs);

            Assert.True(result.Valid);
            Assert.Empty(result.Messages);
        }
        finally
        {
            // Revert to original culture
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }

    }
}