using System.Reflection;
using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.Workflow.Parsers;
using FlowSynx.PluginCore.Exceptions;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Infrastructure.UnitTests.Workflow.Parsers;

public class ExpressionParserTests
{
    private readonly Dictionary<string, object?> _outputs;
    private readonly Dictionary<string, object?> _variables;

    public ExpressionParserTests()
    {
        _outputs = new Dictionary<string, object?>
        {
            ["Num"] = 5,
            ["Greeting"] = "Hello",
            ["User"] = new TestUser { Name = "Alice", Age = 30 },
            ["JsonObj"] = JObject.Parse("{\"Name\":\"Bob\",\"Friends\":[{\"Name\":\"Eva\"},{\"Name\":\"Max\"}],\"Scores\":[1,2,3]}"),
            ["List"] = new List<int> { 10, 20, 30 },
            ["JArray"] = JArray.Parse("[1,2,3]"),
            ["NullObj"] = null,
            ["score"] = 85,
            ["a"] = 10,
            ["b"] = 5
        };

        _variables = new Dictionary<string, object?>
        {
            ["Name"] = "World",
            ["KeyOfGreeting"] = "'Greeting'",
            ["DynamicKey"] = "JsonObj",
            ["threshold"] = 70,
            ["multiplier"] = 2
        };
    }

    [Fact]
    public void Parse_ReturnsNull_WhenExpressionIsNull()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        Assert.Null(parser.Parse(null));
    }

    [Fact]
    public void Parse_ReturnsInput_WhenNoExpressionsFound()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var input = "Just a plain string.";
        var result = parser.Parse(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void Parse_FullExpression_ReturnsRawObject_ForOutputs()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[Outputs('Num')]");
        Assert.IsType<int>(result);
        Assert.Equal(5, result);
    }

    [Fact]
    public void Parse_FullExpression_ReturnsRawObject_ForVariables()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[Variables('Name')]");
        Assert.Equal("World", result);
    }

    [Fact]
    public void Parse_InlineReplacement_ReplacesWithStringValue()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("Hi $[Variables('Name')]!");
        Assert.Equal("Hi World!", result);
    }

    [Fact]
    public void Parse_NestedKeyResolution_UsesVariablesForOutputsKey()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[Outputs(Variables('KeyOfGreeting'))]");
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Parse_UnbalancedBrackets_ThrowsFlowSynxException()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var ex = Assert.Throws<FlowSynxException>(() => parser.Parse("$[Outputs('Num')"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Unbalanced brackets", ex.Message);
    }

    [Fact]
    public void Parse_InvalidInnerExpression_ThrowsFlowSynxException()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var ex = Assert.Throws<FlowSynxException>(() => parser.Parse("$[Foo('Bar')]"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Invalid expression: Foo('Bar')", ex.Message);
    }

    [Fact]
    public void Parse_UnbalancedParentheses_ThrowsFlowSynxException()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var ex = Assert.Throws<FlowSynxException>(() => parser.Parse("$[Outputs('Num']"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Unbalanced parentheses", ex.Message);
    }

    [Fact]
    public void Parse_KeyNotFound_ThrowsFlowSynxException()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var ex = Assert.Throws<FlowSynxException>(() => parser.Parse("$[Outputs('NotExist')]"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("ExpressionParser: Outputs('NotExist') not found", ex.Message);
    }

    // ---------------- Arithmetic & Boolean Tests ----------------

    [Fact]
    public void Parse_ArithmeticExpression_Addition_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[(Outputs('a') + Outputs('b'))]");
        Assert.Equal(15d, result);
    }

    [Fact]
    public void Parse_ArithmeticExpression_MultiplicationAndDivision_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[(Outputs('a') * Variables('multiplier')) / Outputs('b')]");
        Assert.Equal(4d, result);
    }

    [Fact]
    public void Parse_BooleanComparison_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[ Outputs('score') > Variables('threshold') ]");
        Assert.True((bool)result!);
    }

    [Fact]
    public void Parse_BooleanComparison_LessThanOrEqual_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[ Outputs('a') <= Outputs('b') ]");
        Assert.False((bool)result!);
    }

    [Fact]
    public void Parse_Boolean_LogicalAndOr_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[ Outputs('score') > 80 && Variables('threshold') < 90 || Outputs('a') == 10 ]");
        Assert.True((bool)result!);
    }

    [Fact]
    public void Parse_ConditionalExpression_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[ Outputs('score') > Variables('threshold') ? 'PASS' : 'FAIL' ]");
        Assert.Equal("PASS", result);
    }

    [Fact]
    public void Parse_ArithmeticInsideConditional_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[ (Outputs('a') + Outputs('b')) > 12 ? Outputs('a') : Outputs('b') ]");
        Assert.Equal(10, result);
    }

    [Fact]
    public void Parse_ComplexArithmeticBooleanExpression_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[ ((Outputs('a') + Outputs('b')) * Variables('multiplier')) > 20 ]");
        Assert.True((bool)result!);
    }

    [Fact]
    public void Parse_NestedExpressions_InsideArithmetic_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[ (Outputs('a') + $[Variables('multiplier')]) * 2 ]");
        Assert.Equal(24d, result);
    }

    [Fact]
    public void Parse_ParenthesesPreserveOrderOfOperations()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[ (Outputs('a') + Outputs('b')) * 2 ]");
        Assert.Equal(30d, result);
    }

    [Fact]
    public void Parse_NestedArithmeticInsideBoolean_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[ (Outputs('a') + Outputs('b')) * 2 == 30 ]");
        Assert.True((bool)result!);
    }

    // ---------------- Functional Methods Tests ----------------

    [Fact]
    public void Parse_Functional_Min_WithNumbersAndOutputs_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[Min(Outputs('a'), Outputs('b'), 2, 100)]");
        Assert.Equal(2d, result);
    }

    [Fact]
    public void Parse_Functional_Max_WithEnumerable_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[Max(Outputs('List'))]");
        Assert.Equal(30d, result);
    }

    [Fact]
    public void Parse_Functional_Sum_WithEnumerableAndScalars_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[Sum(Outputs('List'), 5, 0.5)]");
        Assert.Equal(65.5d, result);
    }

    [Fact]
    public void Parse_Functional_Avg_WithJArray_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[Avg(Outputs('JArray'))]");
        Assert.Equal(2d, result);
    }

    [Fact]
    public void Parse_Functional_Count_EnumerableAndScalarArgs_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result1 = parser.Parse("$[Count(Outputs('JArray'))]");
        var result2 = parser.Parse("$[Count(1, 2, 3, 4)]");
        Assert.Equal(3, result1);
        Assert.Equal(4, result2);
    }

    [Fact]
    public void Parse_Functional_Contains_StringAndList_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);

        var containsStr = parser.Parse("$[Contains(Outputs('Greeting'), 'ell')]");
        var containsListTrue = parser.Parse("$[Contains(Outputs('List'), 20)]");
        var containsListFalse = parser.Parse("$[Contains(Outputs('List'), 99)]");

        Assert.True((bool)containsStr!);
        Assert.True((bool)containsListTrue!);
        Assert.False((bool)containsListFalse!);
    }

    [Fact]
    public void Parse_Functional_Nested_Functions_Works()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var result = parser.Parse("$[Min(Sum(1, 2, 3), Max(Outputs('List')))]");
        // Sum(1,2,3)=6, Max(List)=30, Min(6,30)=6
        Assert.Equal(6d, result);
    }

    [Fact]
    public void Parse_Functional_Contains_InvalidArgs_Throws()
    {
        var parser = new ExpressionParser(_outputs, _variables);
        var ex = Assert.Throws<FlowSynxException>(() => parser.Parse("$[Contains(Outputs('List'))]"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Contains() expects exactly 2 arguments", ex.Message);
    }

    // ---------------- Reflection-based helper coverage tests ----------------

    [Fact]
    public void FindMatchingBracket_Works()
    {
        var expr = "$[Outputs('A') and $[Outputs('B')]]";
        var start = expr.IndexOf('[');
        var idx = InvokePrivateStatic<int>(typeof(ExpressionParser), "FindMatchingBracket", expr, start);
        Assert.Equal(expr.Length - 1, idx);
    }

    [Fact]
    public void FindMatchingParenthesis_Works()
    {
        var expr = "Outputs('Key')";
        var start = expr.IndexOf('(');
        var idx = InvokePrivateStatic<int>(typeof(ExpressionParser), "FindMatchingParenthesis", expr, start);
        Assert.Equal(expr.Length - 1, idx);
    }

    [Fact]
    public void ReadUntil_Works_UntilEndChar()
    {
        var s = "12345]";
        object[] args = { s, 0, ']' };
        var value = InvokePrivateStatic<string>(typeof(ExpressionParser), "ReadUntil", args);
        Assert.Equal("12345", value);
        Assert.Equal(5, (int)args[1]); // stopped before ']'
    }

    [Fact]
    public void GetNestedValue_DirectCall_WorksForDotPath()
    {
        var obj = new TestUser { Name = "Casey", Age = 40 };
        var value = InvokePrivateStatic<object?>(typeof(ExpressionParser), "GetNestedValue", obj, ".Name");
        Assert.Equal("Casey", value);
    }

    private sealed class TestUser
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    // ---------- Reflection helpers ----------
    private static T InvokePrivateStatic<T>(Type type, string method, params object[]? args)
    {
        var mi = type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(mi);
        var result = mi!.Invoke(null, args ?? Array.Empty<object>());
        return (T?)result!;
    }

    private static T InvokePrivateStaticWithRef<T>(Type type, string method, object[] args)
    {
        var mi = type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(mi);
        var result = mi!.Invoke(null, args);
        return (T?)result!;
    }
}
