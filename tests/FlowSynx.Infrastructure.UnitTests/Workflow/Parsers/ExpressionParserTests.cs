using System.Reflection;
using FlowSynx.Application.Secrets;
using FlowSynx.Domain.Primitives;
using FlowSynx.Infrastructure.Secrets;
using FlowSynx.Infrastructure.Workflow.Expressions;
using FlowSynx.PluginCore.Exceptions;
using Moq;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Infrastructure.UnitTests.Workflow.Expressions;

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
    public async Task Parse_ReturnsNull_WhenExpressionIsNull()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        Assert.Null(await expressionParser.ParseAsync(null));
    }

    [Fact]
    public async Task Parse_ReturnsInput_WhenNoExpressionsFound()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var input = "Just a plain string.";
        var result = await expressionParser.ParseAsync(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task Parse_FullExpression_ReturnsRawObject_ForOutputs()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Outputs('Num')]");
        Assert.IsType<int>(result);
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task Parse_FullExpression_ReturnsRawObject_ForVariables()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Variables('Name')]");
        Assert.Equal("World", result);
    }

    [Fact]
    public async Task Parse_InlineReplacement_ReplacesWithStringValue()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("Hi $[Variables('Name')]!");
        Assert.Equal("Hi World!", result);
    }

    [Fact]
    public async Task Parse_NestedKeyResolution_UsesVariablesForOutputsKey()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Outputs(Variables('KeyOfGreeting'))]");
        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task Parse_UnbalancedBrackets_ThrowsFlowSynxException()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var ex = await Assert.ThrowsAsync<FlowSynxException>(async () => await expressionParser.ParseAsync("$[Outputs('Num')"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Unbalanced brackets", ex.Message);
    }

    [Fact]
    public async Task Parse_InvalidInnerExpression_ThrowsFlowSynxException()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var ex = await Assert.ThrowsAsync<FlowSynxException>(async () => await expressionParser.ParseAsync("$[Foo('Bar')]"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Invalid expression: Foo('Bar')", ex.Message);
    }

    [Fact]
    public async Task Parse_UnbalancedParentheses_ThrowsFlowSynxException()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var ex = await Assert.ThrowsAsync<FlowSynxException>(async () => await expressionParser.ParseAsync("$[Outputs('Num']"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Unbalanced parentheses", ex.Message);
    }

    [Fact]
    public async Task Parse_KeyNotFound_ThrowsFlowSynxException()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var ex = await Assert.ThrowsAsync<FlowSynxException>(async () => await expressionParser.ParseAsync("$[Outputs('NotExist')]"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("ExpressionParser: Outputs('NotExist') not found", ex.Message);
    }

    // ---------------- Arithmetic & Boolean Tests ----------------

    [Fact]
    public async Task Parse_ArithmeticExpression_Addition_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[(Outputs('a') + Outputs('b'))]");
        Assert.Equal(15d, result);
    }

    [Fact]
    public async Task Parse_ArithmeticExpression_MultiplicationAndDivision_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[(Outputs('a') * Variables('multiplier')) / Outputs('b')]");
        Assert.Equal(4d, result);
    }

    [Fact]
    public async Task Parse_BooleanComparison_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[ Outputs('score') > Variables('threshold') ]");
        Assert.True((bool)result!);
    }

    [Fact]
    public async Task Parse_BooleanComparison_LessThanOrEqual_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[ Outputs('a') <= Outputs('b') ]");
        Assert.False((bool)result!);
    }

    [Fact]
    public async Task Parse_Boolean_LogicalAndOr_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[ Outputs('score') > 80 && Variables('threshold') < 90 || Outputs('a') == 10 ]");
        Assert.True((bool)result!);
    }

    [Fact]
    public async Task Parse_ConditionalExpression_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[ Outputs('score') > Variables('threshold') ? 'PASS' : 'FAIL' ]");
        Assert.Equal("PASS", result);
    }

    [Fact]
    public async Task Parse_ArithmeticInsideConditional_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[ (Outputs('a') + Outputs('b')) > 12 ? Outputs('a') : Outputs('b') ]");
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task Parse_ComplexArithmeticBooleanExpression_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[ ((Outputs('a') + Outputs('b')) * Variables('multiplier')) > 20 ]");
        Assert.True((bool)result!);
    }

    [Fact]
    public async Task Parse_NestedExpressions_InsideArithmetic_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[ (Outputs('a') + $[Variables('multiplier')]) * 2 ]");
        Assert.Equal(24d, result);
    }

    [Fact]
    public async Task Parse_ParenthesesPreserveOrderOfOperations()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[ (Outputs('a') + Outputs('b')) * 2 ]");
        Assert.Equal(30d, result);
    }

    [Fact]
    public async Task Parse_NestedArithmeticInsideBoolean_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[ (Outputs('a') + Outputs('b')) * 2 == 30 ]");
        Assert.True((bool)result!);
    }

    // ---------------- Functional Methods Tests ----------------

    [Fact]
    public async Task Parse_Functional_Min_WithNumbersAndOutputs_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Min(Outputs('a'), Outputs('b'), 2, 100)]");
        Assert.Equal(2d, result);
    }

    [Fact]
    public async Task Parse_Functional_Max_WithEnumerable_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Max(Outputs('List'))]");
        Assert.Equal(30d, result);
    }

    [Fact]
    public async Task Parse_Functional_Sum_WithEnumerableAndScalars_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Sum(Outputs('List'), 5, 0.5)]");
        Assert.Equal(65.5d, result);
    }

    [Fact]
    public async Task Parse_Functional_Avg_WithJArray_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Avg(Outputs('JArray'))]");
        Assert.Equal(2d, result);
    }

    [Fact]
    public async Task Parse_Functional_Count_EnumerableAndScalarArgs_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result1 = await expressionParser.ParseAsync("$[Count(Outputs('JArray'))]");
        var result2 = await expressionParser.ParseAsync("$[Count(1, 2, 3, 4)]");
        Assert.Equal(3, result1);
        Assert.Equal(4, result2);
    }

    [Fact]
    public async Task Parse_Functional_Contains_StringAndList_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);

        var containsStr = await expressionParser.ParseAsync("$[Contains(Outputs('Greeting'), 'ell')]");
        var containsListTrue = await expressionParser.ParseAsync("$[Contains(Outputs('List'), 20)]");
        var containsListFalse = await expressionParser.ParseAsync("$[Contains(Outputs('List'), 99)]");

        Assert.True((bool)containsStr!);
        Assert.True((bool)containsListTrue!);
        Assert.False((bool)containsListFalse!);
    }

    [Fact]
    public async Task Parse_Functional_Nested_Functions_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Min(Sum(1, 2, 3), Max(Outputs('List')))]");
        // Sum(1,2,3)=6, Max(List)=30, Min(6,30)=6
        Assert.Equal(6d, result);
    }

    [Fact]
    public async Task Parse_Functional_Contains_InvalidArgs_Throws()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var ex = await Assert.ThrowsAsync<FlowSynxException>(async () => await expressionParser.ParseAsync("$[Contains(Outputs('List'))]"));
        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Contains() expects exactly 2 arguments", ex.Message);
    }

    // ---------------- Secrets Resolver Tests ----------------

    [Fact]
    public async Task Parse_Secrets_WithDefaultProvider_Works()
    {
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("ApiKey", "secret-api-key-123"),
            new("Token", "bearer-token-xyz")
        };

        var mockSecretProvider = new Mock<ISecretProvider>();
        mockSecretProvider
            .Setup(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets);

        var mockSecretFactory = new Mock<ISecretFactory>();
        mockSecretFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(mockSecretProvider.Object);

        var expressionParser = new ExpressionParser(_outputs, _variables, mockSecretFactory.Object);
        var result = await expressionParser.ParseAsync("$[Secrets('ApiKey')]");

        Assert.Equal("secret-api-key-123", result);
        mockSecretProvider.Verify(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Parse_Secrets_InlineReplacement_Works()
    {
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("Token", "bearer-token-xyz")
        };

        var mockSecretProvider = new Mock<ISecretProvider>();
        mockSecretProvider
            .Setup(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets);

        var mockSecretFactory = new Mock<ISecretFactory>();
        mockSecretFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(mockSecretProvider.Object);

        var expressionParser = new ExpressionParser(_outputs, _variables, mockSecretFactory.Object);
        var result = await expressionParser.ParseAsync("Authorization: $[Secrets('Token')]");

        Assert.Equal("Authorization: bearer-token-xyz", result);
    }

    [Fact]
    public async Task Parse_Secrets_WithoutDefaultProvider_DoesNotAddResolver()
    {
        var mockSecretFactory = new Mock<ISecretFactory>();
        mockSecretFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns((ISecretProvider?)null);

        var expressionParser = new ExpressionParser(_outputs, _variables, mockSecretFactory.Object);
        var ex = await Assert.ThrowsAsync<FlowSynxException>(async () => await expressionParser.ParseAsync("$[Secrets('ApiKey')]"));

        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Invalid expression: Secrets('ApiKey')", ex.Message);
    }

    [Fact]
    public async Task Parse_Secrets_WithNullFactory_DoesNotAddResolver()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables, null);
        var ex = await Assert.ThrowsAsync<FlowSynxException>(async () => await expressionParser.ParseAsync("$[Secrets('ApiKey')]"));

        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Parse_Secrets_UsedInConditional_Works()
    {
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("Environment", "production")
        };

        var mockSecretProvider = new Mock<ISecretProvider>();
        mockSecretProvider
            .Setup(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets);

        var mockSecretFactory = new Mock<ISecretFactory>();
        mockSecretFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(mockSecretProvider.Object);

        var expressionParser = new ExpressionParser(_outputs, _variables, mockSecretFactory.Object);
        var result = await expressionParser.ParseAsync("$[Secrets('Environment') == 'production' ? 'PROD' : 'DEV']");

        Assert.Equal("PROD", result);
    }

    [Fact]
    public async Task Parse_Secrets_CaseInsensitiveKeyLookup_Works()
    {
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("ApiKey", "secret-value")
        };

        var mockSecretProvider = new Mock<ISecretProvider>();
        mockSecretProvider
            .Setup(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets);

        var mockSecretFactory = new Mock<ISecretFactory>();
        mockSecretFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(mockSecretProvider.Object);

        var expressionParser = new ExpressionParser(_outputs, _variables, mockSecretFactory.Object);

        var result1 = await expressionParser.ParseAsync("$[Secrets('ApiKey')]");
        var result2 = await expressionParser.ParseAsync("$[Secrets('apikey')]");
        var result3 = await expressionParser.ParseAsync("$[Secrets('APIKEY')]");

        Assert.Equal("secret-value", result1);
        Assert.Equal("secret-value", result2);
        Assert.Equal("secret-value", result3);
    }

    [Fact]
    public async Task Parse_Secrets_SecretNotFound_ThrowsException()
    {
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("ApiKey", "secret-value")
        };

        var mockSecretProvider = new Mock<ISecretProvider>();
        mockSecretProvider
            .Setup(x => x.Name)
            .Returns("TestProvider");
        mockSecretProvider
            .Setup(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets);

        var mockSecretFactory = new Mock<ISecretFactory>();
        mockSecretFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(mockSecretProvider.Object);

        var expressionParser = new ExpressionParser(_outputs, _variables, mockSecretFactory.Object);

        var ex = await Assert.ThrowsAsync<FlowSynxException>(async () => await expressionParser.ParseAsync("$[Secrets('NonExistent')]"));

        Assert.Equal((int)ErrorCode.ExpressionParserKeyNotFound, ex.ErrorCode);
        Assert.Contains("Secret 'NonExistent' not found", ex.Message);
        Assert.Contains("TestProvider", ex.Message);
    }

    [Fact]
    public async Task Parse_Secrets_CachesSecretsOnFirstAccess()
    {
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("Key1", "value1"),
            new("Key2", "value2")
        };

        var mockSecretProvider = new Mock<ISecretProvider>();
        mockSecretProvider
            .Setup(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets);

        var mockSecretFactory = new Mock<ISecretFactory>();
        mockSecretFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(mockSecretProvider.Object);

        var expressionParser = new ExpressionParser(_outputs, _variables, mockSecretFactory.Object);
        
        // Access multiple secrets
        await expressionParser.ParseAsync("$[Secrets('Key1')]");
        await expressionParser.ParseAsync("$[Secrets('Key2')]");
        await expressionParser.ParseAsync("$[Secrets('Key1')]");

        // GetSecretsAsync should only be called once due to caching
        mockSecretProvider.Verify(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------- Custom Functions Tests ----------------

    [Fact]
    public async Task Parse_CustomFunction_RegisterAndExecute_Works()
    {
        var customFunction = new UpperCaseFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, null, new[] { customFunction });

        var result = await expressionParser.ParseAsync("$[ToUpper(Outputs('Greeting'))]");

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public async Task Parse_CustomFunction_MultipleCustomFunctions_Works()
    {
        var upperFunction = new UpperCaseFunction();
        var lengthFunction = new LengthFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, null, new IFunctionEvaluator[] { upperFunction, lengthFunction });

        var result1 = await expressionParser.ParseAsync("$[ToUpper(Outputs('Greeting'))]");
        var result2 = await expressionParser.ParseAsync("$[Length(Outputs('Greeting'))]");

        Assert.Equal("HELLO", result1);
        Assert.Equal(5d, result2);
    }

    [Fact]
    public async Task Parse_CustomFunction_OverrideBuiltIn_Works()
    {
        // Custom function with same name as built-in should override
        var customMax = new CustomMaxFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, null, new[] { customMax });

        var result = await expressionParser.ParseAsync("$[Max(1, 2, 3)]");

        Assert.Equal(999d, result); // Custom implementation returns 999
    }

    [Fact]
    public void RegisterFunction_WithNullFunction_ThrowsArgumentNullException()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);

        Assert.Throws<ArgumentNullException>(() => expressionParser.RegisterFunction(null!));
    }

    [Fact]
    public async Task RegisterFunction_DynamicallyAfterConstruction_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var customFunction = new ReverseFunction();

        expressionParser.RegisterFunction(customFunction);

        var result = await expressionParser.ParseAsync("$[Reverse(Outputs('Greeting'))]");

        Assert.Equal("olleH", result);
    }

    [Fact]
    public async Task UnregisterFunction_RemovesFunction_Works()
    {
        var customFunction = new UpperCaseFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, null, new[] { customFunction });

        // Function should work before unregistration
        var result1 = await expressionParser.ParseAsync("$[ToUpper(Outputs('Greeting'))]");
        Assert.Equal("HELLO", result1);

        // Unregister the function
        var unregistered = expressionParser.UnregisterFunction("ToUpper");
        Assert.True(unregistered);

        // Function should not work after unregistration
        var ex = await Assert.ThrowsAsync<FlowSynxException>(async () => await expressionParser.ParseAsync("$[ToUpper(Outputs('Greeting'))]"));
        Assert.Contains("Invalid expression", ex.Message);
    }

    [Fact]
    public void UnregisterFunction_NonExistentFunction_ReturnsFalse()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);

        var result = expressionParser.UnregisterFunction("NonExistentFunction");

        Assert.False(result);
    }

    [Fact]
    public async Task Parse_CustomFunction_WithMultipleArgs_Works()
    {
        var concatFunction = new ConcatFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, null, new[] { concatFunction });

        var result = await expressionParser.ParseAsync("$[Concat(Outputs('Greeting'), ' ', Variables('Name'))]");

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task Parse_CustomFunction_NestedWithBuiltIn_Works()
    {
        var upperFunction = new UpperCaseFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, null, new[] { upperFunction });

        var result = await expressionParser.ParseAsync("$[Length(ToUpper(Outputs('Greeting')))]");

        Assert.Equal(5d, result);
    }

    [Fact]
    public async Task Parse_CustomFunction_UsedInArithmetic_Works()
    {
        var lengthFunction = new LengthFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, null, new[] { lengthFunction });

        var result = await expressionParser.ParseAsync("$[Length(Outputs('Greeting')) + Outputs('a')]");

        Assert.Equal(15d, result); // 5 + 10
    }

    [Fact]
    public async Task Parse_CustomFunction_CaseInsensitive_Works()
    {
        var upperFunction = new UpperCaseFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, null, new[] { upperFunction });

        var result1 = await expressionParser.ParseAsync("$[ToUpper(Outputs('Greeting'))]");
        var result2 = await expressionParser.ParseAsync("$[toupper(Outputs('Greeting'))]");
        var result3 = await expressionParser.ParseAsync("$[TOUPPER(Outputs('Greeting'))]");

        Assert.Equal("HELLO", result1);
        Assert.Equal("HELLO", result2);
        Assert.Equal("HELLO", result3);
    }

    // ---------------- Combined Features Tests ----------------

    [Fact]
    public async Task Parse_SecretsAndCustomFunctions_Together_Works()
    {
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("AppName", "flowsynx")
        };

        var mockSecretProvider = new Mock<ISecretProvider>();
        mockSecretProvider
            .Setup(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets);

        var mockSecretFactory = new Mock<ISecretFactory>();
        mockSecretFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(mockSecretProvider.Object);

        var upperFunction = new UpperCaseFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, mockSecretFactory.Object, new[] { upperFunction });

        var result = await expressionParser.ParseAsync("$[ToUpper(Secrets('AppName'))]");

        Assert.Equal("FLOWSYNX", result);
    }

    [Fact]
    public async Task Parse_AllSourceResolvers_InSameExpression_Works()
    {
        var secrets = new List<KeyValuePair<string, string>>
        {
            new("Key", "secret")
        };

        var mockSecretProvider = new Mock<ISecretProvider>();
        mockSecretProvider
            .Setup(x => x.GetSecretsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(secrets);

        var mockSecretFactory = new Mock<ISecretFactory>();
        mockSecretFactory
            .Setup(x => x.GetDefaultProvider())
            .Returns(mockSecretProvider.Object);

        var concatFunction = new ConcatFunction();
        var expressionParser = new ExpressionParser(_outputs, _variables, mockSecretFactory.Object, new[] { concatFunction });

        var result = await expressionParser.ParseAsync("$[Concat(Outputs('Greeting'), ' ', Variables('Name'), ' ', Secrets('Key'))]");

        Assert.Equal("Hello World secret", result);
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

    // ---------- Nested Dynamic Index Tests ----------

    [Fact]
    public async Task Parse_DynamicRootKey_NestedProperty_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Outputs(Variables('DynamicKey')).Name]");
        Assert.Equal("Bob", result);
    }

    [Fact]
    public async Task Parse_DynamicRootKey_NestedArrayElementProperty_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Outputs(Variables('DynamicKey')).Friends[1].Name]");
        Assert.Equal("Max", result);
    }

    [Fact]
    public async Task Parse_DynamicRootKey_NestedArrayNumericIndex_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Outputs(Variables('DynamicKey')).Scores[2]]");
        Assert.Equal(3, (long)(result ?? -1));
    }

    [Fact]
    public async Task Parse_DynamicRootKey_UsedInsideArithmetic_Works()
    {
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Outputs(Variables('DynamicKey')).Scores[0] + Outputs(Variables('DynamicKey')).Scores[1]]");
        // 1 + 2 = 3
        Assert.Equal(3d, result);
    }

    [Fact]
    public async Task Parse_DynamicArrayIndex_ViaVariable_NotSupported_ReturnsNull()
    {
        // Bracket index parsing only supports numeric literals, so using a variable inside [] should yield null access result.
        var expressionParser = new ExpressionParser(_outputs, _variables);
        var result = await expressionParser.ParseAsync("$[Outputs('JsonObj').Friends[Variables('multiplier')].Name]"); // multiplier = 2 -> would target index 2, but not parsed as number
        Assert.Null(result);
    }

    // ---------- Test Helper Classes ----------

    private sealed class TestUser
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    // Custom function implementations for testing
    private class UpperCaseFunction : IFunctionEvaluator
    {
        public string Name => "ToUpper";
        
        public object? Evaluate(List<object?> args)
        {
            if (args.Count != 1)
                throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, "ToUpper() expects exactly 1 argument");
            
            return args[0]?.ToString()?.ToUpperInvariant();
        }
    }

    private class LengthFunction : IFunctionEvaluator
    {
        public string Name => "Length";
        
        public object? Evaluate(List<object?> args)
        {
            if (args.Count != 1)
                throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, "Length() expects exactly 1 argument");
            
            return (double)(args[0]?.ToString()?.Length ?? 0);
        }
    }

    private class ReverseFunction : IFunctionEvaluator
    {
        public string Name => "Reverse";
        
        public object? Evaluate(List<object?> args)
        {
            if (args.Count != 1)
                throw new FlowSynxException((int)ErrorCode.ExpressionParserKeyNotFound, "Reverse() expects exactly 1 argument");
            
            var str = args[0]?.ToString() ?? string.Empty;
            return new string(str.Reverse().ToArray());
        }
    }

    private class ConcatFunction : IFunctionEvaluator
    {
        public string Name => "Concat";
        
        public object? Evaluate(List<object?> args)
        {
            return string.Concat(args.Select(a => a?.ToString() ?? string.Empty));
        }
    }

    private class CustomMaxFunction : IFunctionEvaluator
    {
        public string Name => "Max";
        
        public object? Evaluate(List<object?> args)
        {
            // Custom implementation always returns 999 for testing override
            return 999d;
        }
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