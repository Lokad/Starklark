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

    [Fact]
    public void ParsesTrailingNewline()
    {
        var expr = StarlarkParser.ParseExpression("1 + 2\n");

        var expected = new BinaryExpression(
            new LiteralExpression(1L),
            BinaryOperator.Add,
            new LiteralExpression(2L));

        Assert.Equal(expected, expr);
    }

    [Fact]
    public void ParsesNoneLiteral()
    {
        var expr = StarlarkParser.ParseExpression("None");

        Assert.Equal(new LiteralExpression(null!), expr);
    }

    [Fact]
    public void ParsesListLiteral()
    {
        var expr = StarlarkParser.ParseExpression("[1, 2]");

        var list = Assert.IsType<ListExpression>(expr);
        Assert.Collection(
            list.Items,
            item => Assert.Equal(new LiteralExpression(1L), item),
            item => Assert.Equal(new LiteralExpression(2L), item));
    }
}
