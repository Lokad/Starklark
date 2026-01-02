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
}
