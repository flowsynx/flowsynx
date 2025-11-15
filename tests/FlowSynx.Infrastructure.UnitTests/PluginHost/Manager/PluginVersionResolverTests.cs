using System;
using System.Threading;
using System.Threading.Tasks;
using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.PluginHost.Manager;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlowSynx.Infrastructure.UnitTests.PluginHost.Manager;

public class PluginVersionResolverTests
{
    [Fact]
    public async Task ResolveVersionAsync_ReturnsExplicitVersion_WhenProvided()
    {
        var downloaderMock = new Mock<IPluginDownloader>(MockBehavior.Strict);
        var resolver = CreateResolver(downloaderMock);

        var version = await resolver.ResolveVersionAsync("https://registry", "TestPlugin", "2.0.0", CancellationToken.None);

        Assert.Equal("2.0.0", version);
        downloaderMock.Verify(d => d.GetPluginVersionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveVersionAsync_UsesIsLatestFlag_WhenAvailable()
    {
        var downloaderMock = new Mock<IPluginDownloader>(MockBehavior.Strict);
        downloaderMock
            .Setup(d => d.GetPluginVersionsAsync("https://registry", "TestPlugin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PluginVersion { Version = "1.0.0" },
                new PluginVersion { Version = "2.5.0", IsLatest = true }
            });
        var resolver = CreateResolver(downloaderMock);

        var version = await resolver.ResolveVersionAsync("https://registry", "TestPlugin", "latest", CancellationToken.None);

        Assert.Equal("2.5.0", version);
        downloaderMock.VerifyAll();
    }

    [Fact]
    public async Task ResolveVersionAsync_FallsBackToHighestSemanticVersion()
    {
        var downloaderMock = new Mock<IPluginDownloader>(MockBehavior.Strict);
        downloaderMock
            .Setup(d => d.GetPluginVersionsAsync("https://registry", "TestPlugin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PluginVersion { Version = "0.9.0" },
                new PluginVersion { Version = "1.10.0" },
                new PluginVersion { Version = "2.0.0" }
            });
        var resolver = CreateResolver(downloaderMock);

        var version = await resolver.ResolveVersionAsync("https://registry", "TestPlugin", null, CancellationToken.None);

        Assert.Equal("2.0.0", version);
        downloaderMock.VerifyAll();
    }

    [Fact]
    public async Task ResolveVersionAsync_UsesAlphabeticalOrder_WhenNoSemanticVersionExists()
    {
        var downloaderMock = new Mock<IPluginDownloader>(MockBehavior.Strict);
        downloaderMock
            .Setup(d => d.GetPluginVersionsAsync("https://registry", "TestPlugin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new PluginVersion { Version = "beta" },
                new PluginVersion { Version = "alpha" }
            });
        var resolver = CreateResolver(downloaderMock);

        var version = await resolver.ResolveVersionAsync("https://registry", "TestPlugin", "latest", CancellationToken.None);

        Assert.Equal("beta", version);
        downloaderMock.VerifyAll();
    }

    [Fact]
    public async Task ResolveVersionAsync_Throws_WhenRegistryHasNoVersions()
    {
        var downloaderMock = new Mock<IPluginDownloader>(MockBehavior.Strict);
        downloaderMock
            .Setup(d => d.GetPluginVersionsAsync("https://registry", "TestPlugin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PluginVersion>());
        var resolver = CreateResolver(downloaderMock);

        var exception = await Assert.ThrowsAsync<FlowSynxException>(() =>
            resolver.ResolveVersionAsync("https://registry", "TestPlugin", "latest", CancellationToken.None));

        Assert.Equal((int)ErrorCode.PluginNotFound, exception.ErrorCode);
        downloaderMock.VerifyAll();
    }

    private static PluginVersionResolver CreateResolver(Mock<IPluginDownloader> downloaderMock)
    {
        return new PluginVersionResolver(downloaderMock.Object, Mock.Of<ILogger<PluginVersionResolver>>());
    }
}
