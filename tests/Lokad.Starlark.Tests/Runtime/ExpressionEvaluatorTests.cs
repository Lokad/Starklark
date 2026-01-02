using Lokad.Starlark.Parsing;
using Lokad.Starlark.Runtime;
using Lokad.Starlark.Syntax;
using Xunit;

namespace Lokad.Starlark.Tests.Runtime;

public sealed class ExpressionEvaluatorTests
{
    [Fact]
    public void EvaluatesIntegerArithmetic()
    {
        var expr = StarlarkParser.ParseExpression("1 + 2 * 3");
        var evaluator = new ExpressionEvaluator();
        var result = evaluator.Evaluate(expr, new StarlarkEnvironment());

        Assert.Equal(new StarlarkInt(7), result);
    }

    [Fact]
    public void EvaluatesDivisionAsFloat()
    {
        var expr = StarlarkParser.ParseExpression("1 / 2");
        var evaluator = new ExpressionEvaluator();
        var result = evaluator.Evaluate(expr, new StarlarkEnvironment());

        Assert.Equal(new StarlarkFloat(0.5), result);
    }

    [Fact]
    public void EvaluatesFunctionCall()
    {
        var expr = StarlarkParser.ParseExpression("add(1, 2)");
        var environment = new StarlarkEnvironment();
        environment.AddFunction(
            "add",
            args => new StarlarkInt(((StarlarkInt)args[0]).Value + ((StarlarkInt)args[1]).Value));

        var evaluator = new ExpressionEvaluator();
        var result = evaluator.Evaluate(expr, environment);

        Assert.Equal(new StarlarkInt(3), result);
    }

    [Fact]
    public void EvaluatesNoneLiteral()
    {
        var expr = StarlarkParser.ParseExpression("None");
        var evaluator = new ExpressionEvaluator();
        var result = evaluator.Evaluate(expr, new StarlarkEnvironment());

        Assert.Same(StarlarkNone.Instance, result);
    }

    [Fact]
    public void EvaluatesListLiteral()
    {
        var expr = StarlarkParser.ParseExpression("[1, 2]");
        var evaluator = new ExpressionEvaluator();
        var result = evaluator.Evaluate(expr, new StarlarkEnvironment());

        var list = Assert.IsType<StarlarkList>(result);
        Assert.Collection(
            list.Items,
            item => Assert.Equal(new StarlarkInt(1), item),
            item => Assert.Equal(new StarlarkInt(2), item));
    }

    [Fact]
    public void EvaluatesTupleLiteral()
    {
        var expr = StarlarkParser.ParseExpression("(1, 2)");
        var evaluator = new ExpressionEvaluator();
        var result = evaluator.Evaluate(expr, new StarlarkEnvironment());

        var tuple = Assert.IsType<StarlarkTuple>(result);
        Assert.Collection(
            tuple.Items,
            item => Assert.Equal(new StarlarkInt(1), item),
            item => Assert.Equal(new StarlarkInt(2), item));
    }

    [Fact]
    public void EvaluatesDictLiteral()
    {
        var expr = StarlarkParser.ParseExpression("{\"a\": 1, \"b\": 2}");
        var evaluator = new ExpressionEvaluator();
        var result = evaluator.Evaluate(expr, new StarlarkEnvironment());

        var dict = Assert.IsType<StarlarkDict>(result);
        Assert.Collection(
            dict.Entries,
            entry =>
            {
                Assert.Equal(new StarlarkString("a"), entry.Key);
                Assert.Equal(new StarlarkInt(1), entry.Value);
            },
            entry =>
            {
                Assert.Equal(new StarlarkString("b"), entry.Key);
                Assert.Equal(new StarlarkInt(2), entry.Value);
            });
    }

    [Fact]
    public void EvaluatesIndexExpression()
    {
        var expr = StarlarkParser.ParseExpression("[1, 2][0]");
        var evaluator = new ExpressionEvaluator();
        var result = evaluator.Evaluate(expr, new StarlarkEnvironment());

        Assert.Equal(new StarlarkInt(1), result);
    }
}
