using FlowSynx.Infrastructure.Services;
using System.Security.Cryptography;

namespace FlowSynx.Infrastructure.UnitTests.Services;

public class HashServiceTests
{
    private readonly HashService _hashService;

    public HashServiceTests()
    {
        _hashService = new HashService();
    }

    [Fact]
    public void Hash_ValidInput_ReturnsCorrectHash()
    {
        // Arrange
        string input = "hello";
        string expectedHash = "5D41402ABC4B2A76B9719D911017C592"; // Precomputed MD5 hash for "hello"

        // Act
        string result = _hashService.Hash(input);

        // Assert
        Assert.Equal(expectedHash, result);
    }

    [Fact]
    public void Hash_EmptyInput_ReturnsEmptyHash()
    {
        // Arrange
        string input = string.Empty;

        // Act
        string result = _hashService.Hash(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Hash_NullInput_ReturnsEmptyHash()
    {
        // Arrange
        string? input = null;

        // Act
        string result = _hashService.Hash(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Hash_LongInput_ReturnsCorrectHash()
    {
        // Arrange
        string input = new string('a', 1000); // Large input string
        var hasher = MD5.Create();
        var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
        var hashBytes = hasher.ComputeHash(inputBytes);
        string expectedHash = Convert.ToHexString(hashBytes);

        // Act
        string result = _hashService.Hash(input);

        // Assert
        Assert.Equal(expectedHash, result);
    }
}