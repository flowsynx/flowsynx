using System.Reflection;
using FlowSynx.Infrastructure.Secrets.Infisical;
using Infisical.Sdk.Model;
using Xunit;

namespace FlowSynx.Infrastructure.UnitTests.Secrets.Infisical;

public class InfisicalSecretProviderTests
{
    [Fact]
    public void Name_ReturnsExpected()
    {
        var provider = new InfisicalSecretProvider();
        Assert.Equal("Infisical", provider.Name);
    }

    [Fact]
    public void Configure_SetsOptionsValues()
    {
        var config = new Dictionary<string, string>
        {
            ["HostUri"] = "https://example.com",
            ["EnvironmentSlug"] = "dev",
            ["ProjectId"] = "proj-123",
            ["SecretPath"] = "/app",
            ["ClientId"] = "cid",
            ["ClientSecret"] = "csecret",
        };

        var provider = new InfisicalSecretProvider();
        provider.Configure(config);

        var options = GetPrivateField<object>(provider, "_options");
        Assert.NotNull(options);

        // Verify each property via reflection
        Assert.Equal("https://example.com", GetProperty<string>(options!, "HostUri"));
        Assert.Equal("dev", GetProperty<string>(options!, "EnvironmentSlug"));
        Assert.Equal("proj-123", GetProperty<string>(options!, "ProjectId"));
        Assert.Equal("/app", GetProperty<string>(options!, "SecretPath"));
        Assert.Equal("cid", GetProperty<string>(options!, "ClientId"));
        Assert.Equal("csecret", GetProperty<string>(options!, "ClientSecret"));
    }

    [Fact]
    public void Configure_MissingKeys_DefaultsToEmpty()
    {
        var provider = new InfisicalSecretProvider();
        provider.Configure(new Dictionary<string, string>());

        var options = GetPrivateField<object>(provider, "_options");
        Assert.NotNull(options);

        Assert.Equal(string.Empty, GetProperty<string>(options!, "HostUri"));
        Assert.Equal(string.Empty, GetProperty<string>(options!, "EnvironmentSlug"));
        Assert.Equal(string.Empty, GetProperty<string>(options!, "ProjectId"));
        Assert.Equal(string.Empty, GetProperty<string>(options!, "SecretPath"));
        Assert.Equal(string.Empty, GetProperty<string>(options!, "ClientId"));
        Assert.Equal(string.Empty, GetProperty<string>(options!, "ClientSecret"));
    }

    [Fact]
    public async Task GetSecretsAsync_WhenProjectIdMissing_Throws()
    {
        var provider = new InfisicalSecretProvider();
        provider.Configure(new Dictionary<string, string>
        {
            ["EnvironmentSlug"] = "dev",
            ["ClientId"] = "cid",
            ["ClientSecret"] = "csecret",
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetSecretsAsync());
        Assert.Contains("ProjectId is required", ex.Message);
    }

    [Fact]
    public async Task GetSecretsAsync_WhenEnvironmentSlugMissing_Throws()
    {
        var provider = new InfisicalSecretProvider();
        provider.Configure(new Dictionary<string, string>
        {
            ["ProjectId"] = "proj-123",
            ["ClientId"] = "cid",
            ["ClientSecret"] = "csecret",
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetSecretsAsync());
        Assert.Contains("EnvironmentSlug is required", ex.Message);
    }

    [Fact]
    public async Task GetSecretsAsync_WhenClientIdMissing_Throws()
    {
        var provider = new InfisicalSecretProvider();
        provider.Configure(new Dictionary<string, string>
        {
            ["ProjectId"] = "proj-123",
            ["EnvironmentSlug"] = "dev",
            ["ClientSecret"] = "csecret",
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetSecretsAsync());
        Assert.Contains("ClientId is required", ex.Message);
    }

    [Fact]
    public async Task GetSecretsAsync_WhenClientSecretMissing_Throws()
    {
        var provider = new InfisicalSecretProvider();
        provider.Configure(new Dictionary<string, string>
        {
            ["ProjectId"] = "proj-123",
            ["EnvironmentSlug"] = "dev",
            ["ClientId"] = "cid",
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetSecretsAsync());
        Assert.Contains("ClientSecret is required", ex.Message);
    }

    [Fact]
    public async Task GetSecretsAsync_WhenInvalidHostUri_ThrowsInvalidOperation()
    {
        var provider = new InfisicalSecretProvider();
        provider.Configure(new Dictionary<string, string>
        {
            ["HostUri"] = "not-a-valid-uri",
            ["ProjectId"] = "proj-123",
            ["EnvironmentSlug"] = "dev",
            ["ClientId"] = "cid",
            ["ClientSecret"] = "csecret",
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetSecretsAsync());
        Assert.Contains("Invalid Infisical host URI", ex.Message);
    }

    [Fact]
    public async Task GetSecretsAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        var provider = new InfisicalSecretProvider();
        provider.Configure(new Dictionary<string, string>
        {
            ["ProjectId"] = "proj-123",
            ["EnvironmentSlug"] = "dev",
            ["ClientId"] = "cid",
            ["ClientSecret"] = "csecret",
        });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => provider.GetSecretsAsync(cts.Token));
    }

    [Fact]
    public void BuildListSecretOptions_UsesConfiguredValues()
    {
        var provider = new InfisicalSecretProvider();
        provider.Configure(new Dictionary<string, string>
        {
            ["ProjectId"] = "proj-123",
            ["EnvironmentSlug"] = "dev",
            ["ClientId"] = "cid",
            ["ClientSecret"] = "csecret",
            ["SecretPath"] = "/my-path",
        });

        var options = InvokePrivate<ListSecretsOptions>(provider, "BuildListSecretOptions");

        Assert.NotNull(options);
        Assert.Equal("dev", options.EnvironmentSlug);
        Assert.Equal("proj-123", options.ProjectId);
        Assert.Equal("/my-path", options.SecretPath);
        Assert.True(options.ExpandSecretReferences);
        Assert.False(options.SetSecretsAsEnvironmentVariables);
    }

    [Fact]
    public void BuildListSecretOptions_DefaultsSecretPathToRoot()
    {
        var provider = new InfisicalSecretProvider();
        provider.Configure(new Dictionary<string, string>
        {
            ["ProjectId"] = "proj-123",
            ["EnvironmentSlug"] = "dev",
            ["ClientId"] = "cid",
            ["ClientSecret"] = "csecret",
            ["SecretPath"] = "",
        });

        var options = InvokePrivate<ListSecretsOptions>(provider, "BuildListSecretOptions");

        Assert.NotNull(options);
        Assert.Equal("/", options.SecretPath);
    }

    [Theory]
    [InlineData(" KEY ", "KEY")]
    [InlineData("A__B", "A:B")]
    [InlineData("A__B__C", "A:B:C")]
    [InlineData("A__ B ", "A:B")]
    [InlineData("  ", "")]
    [InlineData("A  B", "AB")]
    public void NormalizeKey_WorksAsExpected(string input, string expected)
    {
        var normalized = InvokePrivateStatic<string>(typeof(InfisicalSecretProvider), "NormalizeKey", input);
        Assert.Equal(expected, normalized);
    }

    // ----- Reflection helpers -----
    private static TProperty GetProperty<TProperty>(object instance, string property)
    {
        var prop = instance.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(prop);
        return (TProperty)(prop!.GetValue(instance) ?? default!);
    }

    private static TField GetPrivateField<TField>(object instance, string field)
    {
        var fi = instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(fi);
        return (TField)(fi!.GetValue(instance) ?? default!);
    }

    private static T InvokePrivate<T>(object instance, string method, params object[]? args)
    {
        var mi = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(mi);
        var result = mi!.Invoke(instance, args ?? Array.Empty<object>());
        return (T)result!;
    }

    private static T InvokePrivateStatic<T>(Type type, string method, params object[]? args)
    {
        var mi = type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(mi);
        var result = mi!.Invoke(null, args ?? Array.Empty<object>());
        return (T)result!;
    }
}
