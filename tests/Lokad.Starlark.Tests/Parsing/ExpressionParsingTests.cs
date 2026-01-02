using Lokad.Starlark.Parsing;
using Lokad.Starlark.Syntax;
using Xunit;

namespace Lokad.Starlark.Tests.Parsing;

public sealed class ExpressionParsingTests
{
    [Fact]
    public void ParsesBinaryPrecedence()
    {
        var expr = StarlarkParser.ParseExpression("1 + 2 * 3");

        var expected = new BinaryExpression(
            new LiteralExpression(1L),
            BinaryOperator.Add,
            new BinaryExpression(
                new LiteralExpression(2L),
                BinaryOperator.Multiply,
                new LiteralExpression(3L)));

        Assert.Equal(expected, expr);
    }
}
