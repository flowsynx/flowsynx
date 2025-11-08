using FlowSynx.Infrastructure.Workflow.Parsers;

namespace FlowSynx.Infrastructure.UnitTests.Workflow.Parsers;

public class ExpressionParserFactoryTests
{
    [Fact]
    public void CreateParser_ReturnsIExpressionParser_Instance()
    {
        var factory = new ExpressionParserFactory();
        var outputs = new Dictionary<string, object?> { ["Greeting"] = "Hello" };
        var variables = new Dictionary<string, object?> { ["Name"] = "World" };

        var parser = factory.CreateParser(outputs, variables);

        Assert.NotNull(parser);
        Assert.IsAssignableFrom<IExpressionParser>(parser);
    }

    [Fact]
    public void CreateParser_ParserParsesExpressions_WithProvidedDictionaries()
    {
        var factory = new ExpressionParserFactory();
        var outputs = new Dictionary<string, object?> { ["Greeting"] = "Hello" };
        var variables = new Dictionary<string, object?> { ["Name"] = "World" };

        var parser = factory.CreateParser(outputs, variables);
        var result = parser.Parse("Say $[Outputs('Greeting')], $[Variables('Name')]!");

        Assert.Equal("Say Hello, World!", result);
    }

    [Fact]
    public void CreateParser_AllowsNullVariables_OutputsStillWork()
    {
        var factory = new ExpressionParserFactory();
        var outputs = new Dictionary<string, object?> { ["Greeting"] = "Hello" };

        var parser = factory.CreateParser(outputs, variables: null);
        var result = parser.Parse("$[Outputs('Greeting')]");

        // Variables-based parsing would NRE if used with null, but Outputs path must still work.
        Assert.Equal("Hello", result);
    }
}