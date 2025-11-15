using System.Text;
using FlowSynx.Plugins.LocalFileSystem.Extensions;

namespace FlowSynx.UnitTests.Plugins.LocalFileSystem;

/// <summary>
/// Validates the binary file detection heuristics exposed by <see cref="ConverterExtensions"/>.
/// </summary>
public class ConverterExtensionsTests
{
    [Fact]
    public void IsBinaryFile_ReturnsFalse_ForPrintableAsciiContent()
    {
        var data = Encoding.UTF8.GetBytes("Line 1\nLine 2\r\nTab\tSeparated");

        var isBinary = ConverterExtensions.IsBinaryFile(data);

        Assert.False(isBinary);
    }

    [Fact]
    public void IsBinaryFile_ReturnsTrue_WhenNonPrintableThresholdExceeded()
    {
        var data = new byte[]
        {
            0x00, // NUL
            0x01, // SOH
            0x0A, // LF (ignored)
            0x0D, // CR (ignored)
            0x7F, // DEL
            0xC8  // Extended ASCII
        };

        var isBinary = ConverterExtensions.IsBinaryFile(data);

        Assert.True(isBinary);
    }

    [Fact]
    public void IsBinaryFile_ReturnsFalse_ForNullInput()
    {
        var isBinary = ConverterExtensions.IsBinaryFile(null);

        Assert.False(isBinary);
    }
}
