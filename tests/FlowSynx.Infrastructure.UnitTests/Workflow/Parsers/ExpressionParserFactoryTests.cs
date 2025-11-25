using FlowSynx.Infrastructure.Workflow.Expressions;

namespace FlowSynx.Infrastructure.UnitTests.Workflow.Expressions;

public class ExpressionParserFactoryTests
{
    [Fact]
    public void CreateParser_ReturnsIExpressionParser_Instance()
    {
        var factory = new ExpressionParserFactory();
        var outputs = new Dictionary<string, object?> { ["Greeting"] = "Hello" };
        var variables = new Dictionary<string, object?> { ["Name"] = "World" };

        var expressionParser = factory.CreateParser(outputs, variables);

        Assert.NotNull(expressionParser);
        Assert.IsAssignableFrom<IExpressionParser>(expressionParser);
    }

    [Fact]
    public async Task CreateParser_ParserParsesExpressions_WithProvidedDictionaries()
    {
        var factory = new ExpressionParserFactory();
        var outputs = new Dictionary<string, object?> { ["Greeting"] = "Hello" };
        var variables = new Dictionary<string, object?> { ["Name"] = "World" };

        var expressionParser = factory.CreateParser(outputs, variables);
        var result = await expressionParser.ParseAsync("Say $[Outputs('Greeting')], $[Variables('Name')]!", cancellationToken: default);

        Assert.Equal("Say Hello, World!", result);
    }

    [Fact]
    public async Task CreateParser_AllowsNullVariables_OutputsStillWork()
    {
        var factory = new ExpressionParserFactory();
        var outputs = new Dictionary<string, object?> { ["Greeting"] = "Hello" };

        var expressionParser = factory.CreateParser(outputs, variables: null);
        var result = await expressionParser.ParseAsync("$[Outputs('Greeting')]");

        // Variables-based parsing would NRE if used with null, but Outputs path must still work.
        Assert.Equal("Hello", result);
    }
}