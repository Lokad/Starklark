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

    [Fact]
    public void ParsesTupleLiteral()
    {
        var expr = StarlarkParser.ParseExpression("(1, 2)");

        var tuple = Assert.IsType<TupleExpression>(expr);
        Assert.Collection(
            tuple.Items,
            item => Assert.Equal(new LiteralExpression(1L), item),
            item => Assert.Equal(new LiteralExpression(2L), item));
    }

    [Fact]
    public void ParsesSingleItemTuple()
    {
        var expr = StarlarkParser.ParseExpression("(1,)");

        var tuple = Assert.IsType<TupleExpression>(expr);
        Assert.Collection(tuple.Items, item => Assert.Equal(new LiteralExpression(1L), item));
    }

    [Fact]
    public void ParsesTupleWithoutParentheses()
    {
        var expr = StarlarkParser.ParseExpression("1, 2");

        var tuple = Assert.IsType<TupleExpression>(expr);
        Assert.Collection(
            tuple.Items,
            item => Assert.Equal(new LiteralExpression(1L), item),
            item => Assert.Equal(new LiteralExpression(2L), item));
    }

    [Fact]
    public void ParsesDictLiteral()
    {
        var expr = StarlarkParser.ParseExpression("{\"a\": 1, \"b\": 2}");

        var dict = Assert.IsType<DictExpression>(expr);
        Assert.Collection(
            dict.Entries,
            entry =>
            {
                Assert.Equal(new LiteralExpression("a"), entry.Key);
                Assert.Equal(new LiteralExpression(1L), entry.Value);
            },
            entry =>
            {
                Assert.Equal(new LiteralExpression("b"), entry.Key);
                Assert.Equal(new LiteralExpression(2L), entry.Value);
            });
    }

    [Fact]
    public void ParsesIndexExpression()
    {
        var expr = StarlarkParser.ParseExpression("items[0]");

        var index = Assert.IsType<IndexExpression>(expr);
        Assert.Equal(new IdentifierExpression("items"), index.Target);
        Assert.Equal(new LiteralExpression(0L), index.Index);
    }

    [Fact]
    public void ParsesInExpression()
    {
        var expr = StarlarkParser.ParseExpression("1 in [1, 2]");

        var binary = Assert.IsType<BinaryExpression>(expr);
        Assert.Equal(BinaryOperator.In, binary.Operator);
    }

    [Fact]
    public void ParsesComparisonExpression()
    {
        var expr = StarlarkParser.ParseExpression("1 < 2");

        var binary = Assert.IsType<BinaryExpression>(expr);
        Assert.Equal(BinaryOperator.Less, binary.Operator);
    }

    [Fact]
    public void ParsesNotInExpression()
    {
        var expr = StarlarkParser.ParseExpression("1 not in [2]");

        var binary = Assert.IsType<BinaryExpression>(expr);
        Assert.Equal(BinaryOperator.NotIn, binary.Operator);
    }

    [Fact]
    public void ParsesFloorDivideExpression()
    {
        var expr = StarlarkParser.ParseExpression("10 // 3");

        var binary = Assert.IsType<BinaryExpression>(expr);
        Assert.Equal(BinaryOperator.FloorDivide, binary.Operator);
    }

    [Fact]
    public void ParsesConditionalExpression()
    {
        var expr = StarlarkParser.ParseExpression("1 if True else 0");

        var conditional = Assert.IsType<ConditionalExpression>(expr);
        Assert.Equal(new LiteralExpression(true), conditional.Condition);
        Assert.Equal(new LiteralExpression(1L), conditional.ThenExpression);
        Assert.Equal(new LiteralExpression(0L), conditional.ElseExpression);
    }

    [Fact]
    public void ParsesStringEscapes()
    {
        var expr = StarlarkParser.ParseExpression("\"a\\n\\t\"");

        Assert.Equal(new LiteralExpression("a\n\t"), expr);
    }

    [Fact]
    public void ParsesHexAndOctalEscapes()
    {
        var expr = StarlarkParser.ParseExpression("\"\\x41\\101\"");

        Assert.Equal(new LiteralExpression("AA"), expr);
    }
}
