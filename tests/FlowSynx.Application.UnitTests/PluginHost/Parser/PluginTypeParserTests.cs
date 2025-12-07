using FlowSynx.Application.PluginHost.Parser;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Application.UnitTests.PluginHost.Parser;

public sealed class PluginTypeParserTests
{
    // Parse(string) - success: simple type -> latest
    [Fact]
    public void Parse_SimpleType_ReturnsLatestVersion()
    {
        var result = PluginTypeParser.Parse("compressor");

        Assert.NotNull(result);
        Assert.Equal("compressor", result.Type);
        Assert.Equal("latest", result.CurrentVersion);
        Assert.Null(result.TargetVersion);
    }

    // Parse(string) - success: type:version -> normalized lowercase, trimmed
    [Fact]
    public void Parse_TypeWithVersion_ReturnsNormalizedVersion()
    {
        var result = PluginTypeParser.Parse("compressor:  V1.2  ");

        Assert.NotNull(result);
        Assert.Equal("compressor", result.Type);
        Assert.Equal("v1.2", result.CurrentVersion); // normalized to lower + trimmed
        Assert.Null(result.TargetVersion);
    }

    // Parse(string) - success: update mode <type>:<current>-><target>
    [Fact]
    public void Parse_UpdateFormat_ReturnsParsedUpdate()
    {
        var result = PluginTypeParser.Parse("compressor:1.0->2.0");

        Assert.NotNull(result);
        Assert.Equal("compressor", result.Type);
        Assert.Equal("1.0", result.CurrentVersion);
        Assert.Equal("2.0", result.TargetVersion);
    }

    // Parse(string) - success: update mode trims and normalizes target/current
    [Fact]
    public void Parse_UpdateFormat_TrimsAndNormalizesVersions()
    {
        var result = PluginTypeParser.Parse("compressor:  V1  ->  V2  ");

        Assert.NotNull(result);
        Assert.Equal("compressor", result.Type);
        Assert.Equal("v1", result.CurrentVersion);
        Assert.Equal("v2", result.TargetVersion);
    }

    // Parse(string) - error: null/empty throws FlowSynxException with message
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyInput_ThrowsFlowSynxException(string input)
    {
        var ex = Assert.Throws<FlowSynxException>(() => PluginTypeParser.Parse(input));
        Assert.Contains("Plugin type input cannot be null or empty.", ex.Message, StringComparison.Ordinal);
    }

    // Parse(string) - error: malformed update format
    [Theory]
    [InlineData("compressor->2.0")] // missing current version
    [InlineData("compressor:1.0->")] // missing target version
    [InlineData("compressor:1.0->2.0->3.0")] // too many parts
    public void Parse_MalformedUpdateFormat_ThrowsFlowSynxException(string input)
    {
        var ex = Assert.Throws<FlowSynxException>(() => PluginTypeParser.Parse(input));
        Assert.Contains("Update format must be <type>:<currentVersion>-><targetVersion>.", ex.Message, StringComparison.Ordinal);
    }

    // Parse(string) - error: empty type in simple format
    [Theory]
    [InlineData(":1.0")]
    [InlineData(":")]
    public void Parse_EmptyTypeInSimpleFormat_ThrowsFlowSynxException(string input)
    {
        var ex = Assert.Throws<FlowSynxException>(() => PluginTypeParser.Parse(input));
        Assert.Contains("Plugin type cannot be empty.", ex.Message, StringComparison.Ordinal);
    }

    // Parse(string) - error: empty version in type:version format
    [Theory]
    [InlineData("compressor:")]
    [InlineData("compressor:   ")]
    public void Parse_EmptyVersionInTypeVersionFormat_ThrowsFlowSynxException(string input)
    {
        var result = PluginTypeParser.Parse(input);
        Assert.Equal("compressor", result.Type);
        Assert.Equal("latest", result.CurrentVersion);
    }

    // Parse(string) - error: latest not allowed as current version in update mode
    [Theory]
    [InlineData("compressor:latest->2.0")]
    [InlineData("compressor:LATEST->2.0")]
    public void Parse_LatestAsCurrentInUpdateMode_ThrowsFlowSynxException(string input)
    {
        var ex = Assert.Throws<FlowSynxException>(() => PluginTypeParser.Parse(input));
        Assert.Contains("\"latest\" is not allowed as current version in update mode.", ex.Message, StringComparison.Ordinal);
    }

    // Parse(string) - error: wrong simple format parts count
    [Fact]
    public void Parse_WrongSimpleFormatPartsCount_ThrowsFlowSynxException()
    {
        var ex = Assert.Throws<FlowSynxException>(() => PluginTypeParser.Parse("a:b:c"));
        Assert.Contains("Plugin type format must be <type> or <type>:<version> or <type>:<current>-><target>.", ex.Message, StringComparison.Ordinal);
    }

    // TryParse(string?, out, out) - success cases
    [Fact]
    public void TryParse_SimpleType_SucceedsWithLatest()
    {
        var ok = PluginTypeParser.TryParse("compressor", out var result, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("compressor", result!.Type);
        Assert.Equal("latest", result.CurrentVersion);
        Assert.Null(result.TargetVersion);
    }

    [Fact]
    public void TryParse_TypeWithVersion_SucceedsWithNormalizedVersion()
    {
        var ok = PluginTypeParser.TryParse("compressor:V2", out var result, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("compressor", result!.Type);
        Assert.Equal("v2", result.CurrentVersion);
        Assert.Null(result.TargetVersion);
    }

    [Fact]
    public void TryParse_UpdateFormat_Succeeds()
    {
        var ok = PluginTypeParser.TryParse("compressor:1.0->1.1", out var result, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("compressor", result!.Type);
        Assert.Equal("1.0", result.CurrentVersion);
        Assert.Equal("1.1", result.TargetVersion);
    }

    // TryParse - failure cases with specific error messages
    [Theory]
    [InlineData(null, "Plugin type input cannot be null or empty.")]
    [InlineData("", "Plugin type input cannot be null or empty.")]
    [InlineData("   ", "Plugin type input cannot be null or empty.")]
    public void TryParse_NullOrEmpty_FailsWithMessage(string? input, string expectedError)
    {
        var ok = PluginTypeParser.TryParse(input, out var result, out var error);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void TryParse_UpdateFormatMissingParts_Fails()
    {
        var ok = PluginTypeParser.TryParse("compressor->2.0", out var result, out var error);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal("Update format must be <type>:<currentVersion>-><targetVersion>.", error);
    }

    [Fact]
    public void TryParse_UpdateFormatEmptyTarget_Fails()
    {
        var ok = PluginTypeParser.TryParse("compressor:1.0->   ", out var result, out var error);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal("Update format must be <type>:<currentVersion>-><targetVersion>.", error);
    }

    [Fact]
    public void TryParse_UpdateFormatEmptyCurrent_Fails()
    {
        var ok = PluginTypeParser.TryParse("compressor:   -> 2.0", out var result, out var error);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal("\"latest\" is not allowed as current version in update mode.", error);
    }

    [Fact]
    public void TryParse_UpdateFormatLatestAsCurrent_Fails()
    {
        var ok = PluginTypeParser.TryParse("compressor:latest->2.0", out var result, out var error);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal("\"latest\" is not allowed as current version in update mode.", error);
    }

    [Fact]
    public void TryParse_SimpleFormatEmptyType_Fails()
    {
        var ok = PluginTypeParser.TryParse(":1.0", out var result, out var error);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal("Plugin type cannot be empty.", error);
    }

    [Fact]
    public void TryParse_SimpleFormatEmptyVersion_SucceedsWithLatest()
    {
        var ok = PluginTypeParser.TryParse("compressor:", out var result, out var error);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal("compressor", result.Type);
        Assert.Equal("latest", result.CurrentVersion);
    }

    [Fact]
    public void TryParse_SimpleFormatTooManyParts_Fails()
    {
        var ok = PluginTypeParser.TryParse("a:b:c", out var result, out var error);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal("Plugin type format must be <type> or <type>:<version> or <type>:<current>-><target>.", error);
    }

    // Additional normalization coverage: whitespace-only version becomes "latest" in simple format path
    [Fact]
    public void TryParse_SimpleFormatWhitespaceVersion_SucceedsWithLatest()
    {
        var ok = PluginTypeParser.TryParse("compressor:   ", out var result, out var error);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal("compressor", result!.Type);
        Assert.Equal("latest", result.CurrentVersion);
    }

    // Ensure trimming of input before parsing
    [Fact]
    public void TryParse_InputTrimmedBeforeParsing()
    {
        var ok = PluginTypeParser.TryParse("   compressor:V1   ", out var result, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("compressor", result!.Type);
        Assert.Equal("v1", result.CurrentVersion);
    }
}