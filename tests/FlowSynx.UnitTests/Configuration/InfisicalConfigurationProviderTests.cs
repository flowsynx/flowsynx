using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlowSynx.Infrastructure.Configuration;

namespace FlowSynx.UnitTests.Configuration;

public class InfisicalConfigurationProviderTests
{
    [Fact]
    public void Load_PopulatesConfigurationDataFromClient()
    {
        var secrets = new[]
        {
            new KeyValuePair<string, string>("Logger:Level", "Debug"),
            new KeyValuePair<string, string>("Security:DefaultScheme", "Jwt")
        };

        var provider = new InfisicalConfigurationProvider(new InMemorySecretClient(secrets));

        provider.Load();

        Assert.True(provider.TryGet("Logger:Level", out var loggerLevel));
        Assert.Equal("Debug", loggerLevel);

        Assert.True(provider.TryGet("Security:DefaultScheme", out var defaultScheme));
        Assert.Equal("Jwt", defaultScheme);
    }

    [Fact]
    public void Load_ThrowsInfisicalConfigurationExceptionWhenClientFails()
    {
        var provider = new InfisicalConfigurationProvider(new ThrowingSecretClient());

        Assert.Throws<InfisicalConfigurationException>(provider.Load);
    }

    private sealed class InMemorySecretClient : IInfisicalSecretClient
    {
        private readonly IReadOnlyCollection<KeyValuePair<string, string>> _secrets;

        public InMemorySecretClient(IReadOnlyCollection<KeyValuePair<string, string>> secrets)
        {
            _secrets = secrets;
        }

        public Task<IReadOnlyCollection<KeyValuePair<string, string>>> GetSecretsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_secrets);
        }
    }

    private sealed class ThrowingSecretClient : IInfisicalSecretClient
    {
        public Task<IReadOnlyCollection<KeyValuePair<string, string>>> GetSecretsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("boom");
        }
    }
}
