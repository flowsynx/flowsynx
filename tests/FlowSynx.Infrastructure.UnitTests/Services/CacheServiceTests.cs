using FlowSynx.Infrastructure.Services;
using System;
using Xunit;

namespace FlowSynx.Infrastructure.UnitTests.Services;

public class CacheServiceTests
{
    [Fact]
    public void Set_AddsNewEntry()
    {
        // Arrange
        var cache = new CacheService<string, int>();

        // Act
        cache.Set("key1", 100);

        // Assert
        Assert.Equal(1, cache.Count());
        Assert.Equal(100, cache.Get("key1"));
    }

    [Fact]
    public void Set_UpdatesExistingEntry()
    {
        // Arrange
        var cache = new CacheService<string, int>();
        cache.Set("key1", 100);

        // Act
        cache.Set("key1", 200);

        // Assert
        Assert.Equal(1, cache.Count());
        Assert.Equal(200, cache.Get("key1"));
    }

    [Fact]
    public void Get_ReturnsDefault_WhenKeyDoesNotExist()
    {
        // Arrange
        var cache = new CacheService<string, int>();

        // Act
        var result = cache.Get("nonexistentKey");

        // Assert
        Assert.Equal(default(int), result);
    }

    [Fact]
    public void Delete_RemovesExistingEntry()
    {
        // Arrange
        var cache = new CacheService<string, int>();
        cache.Set("key1", 100);

        // Act
        cache.Delete("key1");

        // Assert
        Assert.Equal(0, cache.Count());
        Assert.Equal(default(int), cache.Get("key1"));
    }

    [Fact]
    public void Delete_DoesNothing_WhenKeyDoesNotExist()
    {
        // Arrange
        var cache = new CacheService<string, int>();

        // Act
        cache.Delete("nonexistentKey");

        // Assert
        Assert.Equal(0, cache.Count());
    }

    [Fact]
    public void Count_ReturnsCorrectCount()
    {
        // Arrange
        var cache = new CacheService<string, int>();
        cache.Set("key1", 100);
        cache.Set("key2", 200);

        // Act
        var count = cache.Count();

        // Assert
        Assert.Equal(2, count);
    }
}