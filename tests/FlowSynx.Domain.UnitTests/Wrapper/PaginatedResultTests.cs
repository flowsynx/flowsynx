using FlowSynx.Domain.Wrapper;

namespace FlowSynx.Domain.UnitTests.Wrapper;

public class PaginatedResultTests
{
    [Fact]
    public async Task SuccessAsync_PopulatesMetadataCorrectly()
    {
        // Arrange
        var items = new List<int> { 4, 5, 6 };

        // Act
        var result = await PaginatedResult<int>.SuccessAsync(items, totalCount: 10, page: 2, pageSize: 3);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(items, result.Data);
        Assert.Equal(10, result.Pagination.TotalCount);
        Assert.Equal(2, result.Pagination.CurrentPage);
        Assert.Equal(3, result.Pagination.PageSize);
        Assert.Equal(4, result.Pagination.TotalPages);
        Assert.True(result.Pagination.HasPreviousPage);
        Assert.True(result.Pagination.HasNextPage);
    }

    [Fact]
    public async Task FailureAsync_SetsSucceededToFalse()
    {
        // Act
        var result = await PaginatedResult<int>.FailureAsync("oops");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("oops", result.Messages);
        Assert.Equal(0, result.Pagination.TotalCount);
        Assert.Equal(1, result.Pagination.CurrentPage);
        Assert.Equal(1, result.Pagination.PageSize);
        Assert.Empty(result.Data);
    }
}
