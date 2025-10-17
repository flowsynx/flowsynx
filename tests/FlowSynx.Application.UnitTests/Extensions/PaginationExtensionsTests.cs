using FlowSynx.Application.Extensions;

namespace FlowSynx.Application.UnitTests.Extensions;

public class PaginationExtensionsTests
{
    [Fact]
    public void ToPaginatedList_ReturnsExpectedSubset()
    {
        // Arrange
        var source = Enumerable.Range(1, 10);

        // Act
        var items = source.ToPaginatedList(2, 3, out var totalCount, out var currentPage, out var pageSize);

        // Assert
        Assert.Equal(10, totalCount);
        Assert.Equal(2, currentPage);
        Assert.Equal(3, pageSize);
        Assert.Equal(new List<int> { 4, 5, 6 }, items);
    }

    [Fact]
    public void ToPaginatedList_NormalizesInvalidArguments()
    {
        // Arrange
        var source = Enumerable.Range(1, 5);

        // Act
        var items = source.ToPaginatedList(-1, 0, out var totalCount, out var currentPage, out var pageSize);

        // Assert
        Assert.Equal(5, totalCount);
        Assert.Equal(1, currentPage);
        Assert.Equal(5, pageSize);
        Assert.Equal(source.ToList(), items);
    }

    [Fact]
    public void ToPaginatedList_ClampsPageToLastPage()
    {
        // Arrange
        var source = Enumerable.Range(1, 10);

        // Act
        var items = source.ToPaginatedList(5, 3, out var totalCount, out var currentPage, out var pageSize);

        // Assert
        Assert.Equal(10, totalCount);
        Assert.Equal(4, currentPage);
        Assert.Equal(3, pageSize);
        Assert.Equal(new List<int> { 10 }, items);
    }
}
