using FlowSynx.Application.Extensions;
using System.Linq.Expressions;

namespace FlowSynx.Application.UnitTests.Extensions;

public class PredicateBuilderTests
{
    [Fact]
    public void TruePredicate_ShouldAlwaysReturnTrue()
    {
        var predicate = PredicateBuilder.True<int>().Compile();
        Assert.True(predicate(5));
        Assert.True(predicate(100));
    }

    [Fact]
    public void FalsePredicate_ShouldAlwaysReturnFalse()
    {
        var predicate = PredicateBuilder.False<int>().Compile();
        Assert.False(predicate(5));
        Assert.False(predicate(100));
    }

    [Fact]
    public void CreatePredicate_ShouldMatchGivenExpression()
    {
        Expression<Func<int, bool>> expr = x => x > 10;
        var predicate = PredicateBuilder.Create(expr).Compile();
        Assert.False(predicate(5));
        Assert.True(predicate(15));
    }

    [Fact]
    public void AndPredicate_ShouldReturnExpectedResults()
    {
        var predicate1 = PredicateBuilder.True<int>();
        var predicate2 = PredicateBuilder.Create<int>(x => x > 10);
        var combinedPredicate = predicate1.And(predicate2).Compile();

        Assert.False(combinedPredicate(5));
        Assert.True(combinedPredicate(15));
    }

    [Fact]
    public void OrPredicate_ShouldReturnExpectedResults()
    {
        var predicate1 = PredicateBuilder.False<int>();
        var predicate2 = PredicateBuilder.Create<int>(x => x > 10);
        var combinedPredicate = predicate1.Or(predicate2).Compile();

        Assert.False(combinedPredicate(5));
        Assert.True(combinedPredicate(15));
    }

    [Fact]
    public void NotPredicate_ShouldNegateExpression()
    {
        var predicate = PredicateBuilder.Create<int>(x => x > 10).Not().Compile();
        Assert.True(predicate(5));
        Assert.False(predicate(15));
    }
}