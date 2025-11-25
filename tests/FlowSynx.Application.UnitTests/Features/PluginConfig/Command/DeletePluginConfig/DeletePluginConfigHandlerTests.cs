using FlowSynx.Application.Features.PluginConfig.Command.DeletePluginConfig;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Services;
using FlowSynx.Domain;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowSynx.Application.UnitTests.Features.PluginConfig.Command.DeletePluginConfig;

public class DeletePluginConfigHandlerTests
{
    [Fact]
    public async Task Handle_WhenFlowSynxExceptionThrown_LogsStructuredExceptionAndReturnsFailureMessage()
    {
        var loggerMock = new Mock<ILogger<DeletePluginConfigHandler>>();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        var pluginConfigurationServiceMock = new Mock<IPluginConfigurationService>();
        var localizationMock = new Mock<ILocalization>();

        var request = new DeletePluginConfigRequest { ConfigId = Guid.NewGuid().ToString() };
        var exception = new FlowSynxException((int)ErrorCode.PluginConfigurationNotFound, "Config not found.");

        currentUserServiceMock.Setup(service => service.UserId()).Returns("user-id");
        currentUserServiceMock.Setup(service => service.ValidateAuthentication());
        pluginConfigurationServiceMock
            .Setup(service => service.Get("user-id", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var handler = new DeletePluginConfigHandler(
            loggerMock.Object,
            currentUserServiceMock.Object,
            pluginConfigurationServiceMock.Object,
            localizationMock.Object);

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(exception.Message, result.Messages!);

        loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state != null &&
                    state.ToString()!.Contains("FlowSynx exception caught while deleting plugin config") &&
                    state.ToString()!.Contains(request.ConfigId)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
