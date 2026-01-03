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

        var expected = Bin(
            Lit(1L),
            BinaryOperator.Add,
            Bin(
                Lit(2L),
                BinaryOperator.Multiply,
                Lit(3L)));

        Assert.Equal(SyntaxNormalization.Normalize(expected), SyntaxNormalization.Normalize(expr));
    }

    [Fact]
    public void ParsesTrailingNewline()
    {
        var expr = StarlarkParser.ParseExpression("1 + 2\n");

        var expected = Bin(
            Lit(1L),
            BinaryOperator.Add,
            Lit(2L));

        Assert.Equal(SyntaxNormalization.Normalize(expected), SyntaxNormalization.Normalize(expr));
    }

    [Fact]
    public void ParsesNoneLiteral()
    {
        var expr = StarlarkParser.ParseExpression("None");

        Assert.Equal(SyntaxNormalization.Normalize(Lit(null!)), SyntaxNormalization.Normalize(expr));
    }

    [Fact]
    public void ParsesListLiteral()
    {
        var expr = StarlarkParser.ParseExpression("[1, 2]");

        var list = Assert.IsType<ListExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Collection(
            list.Items,
            item => Assert.Equal(Lit(1L), item),
            item => Assert.Equal(Lit(2L), item));
    }

    [Fact]
    public void ParsesTupleLiteral()
    {
        var expr = StarlarkParser.ParseExpression("(1, 2)");

        var tuple = Assert.IsType<TupleExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Collection(
            tuple.Items,
            item => Assert.Equal(Lit(1L), item),
            item => Assert.Equal(Lit(2L), item));
    }

    [Fact]
    public void ParsesSingleItemTuple()
    {
        var expr = StarlarkParser.ParseExpression("(1,)");

        var tuple = Assert.IsType<TupleExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Collection(tuple.Items, item => Assert.Equal(Lit(1L), item));
    }

    [Fact]
    public void ParsesTupleWithoutParentheses()
    {
        var expr = StarlarkParser.ParseExpression("1, 2");

        var tuple = Assert.IsType<TupleExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Collection(
            tuple.Items,
            item => Assert.Equal(Lit(1L), item),
            item => Assert.Equal(Lit(2L), item));
    }

    [Fact]
    public void ParsesDictLiteral()
    {
        var expr = StarlarkParser.ParseExpression("{\"a\": 1, \"b\": 2}");

        var dict = Assert.IsType<DictExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Collection(
            dict.Entries,
            entry =>
            {
                Assert.Equal(Lit("a"), entry.Key);
                Assert.Equal(Lit(1L), entry.Value);
            },
            entry =>
            {
                Assert.Equal(Lit("b"), entry.Key);
                Assert.Equal(Lit(2L), entry.Value);
            });
    }

    [Fact]
    public void ParsesIndexExpression()
    {
        var expr = StarlarkParser.ParseExpression("items[0]");

        var index = Assert.IsType<IndexExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Equal(Id("items"), index.Target);
        var indexValue = Assert.IsType<IndexValue>(index.Index);
        Assert.Equal(Lit(0L), indexValue.Value);
    }

    [Fact]
    public void ParsesSliceExpression()
    {
        var expr = StarlarkParser.ParseExpression("items[1:3]");

        var index = Assert.IsType<IndexExpression>(SyntaxNormalization.Normalize(expr));
        var slice = Assert.IsType<SliceIndex>(index.Index);
        Assert.Equal(Lit(1L), slice.Start);
        Assert.Equal(Lit(3L), slice.Stop);
        Assert.Null(slice.Step);
    }

    [Fact]
    public void ParsesInExpression()
    {
        var expr = StarlarkParser.ParseExpression("1 in [1, 2]");

        var binary = Assert.IsType<BinaryExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Equal(BinaryOperator.In, binary.Operator);
    }

    [Fact]
    public void ParsesComparisonExpression()
    {
        var expr = StarlarkParser.ParseExpression("1 < 2");

        var binary = Assert.IsType<BinaryExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Equal(BinaryOperator.Less, binary.Operator);
    }

    [Fact]
    public void ParsesNotInExpression()
    {
        var expr = StarlarkParser.ParseExpression("1 not in [2]");

        var binary = Assert.IsType<BinaryExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Equal(BinaryOperator.NotIn, binary.Operator);
    }

    [Fact]
    public void ParsesFloorDivideExpression()
    {
        var expr = StarlarkParser.ParseExpression("10 // 3");

        var binary = Assert.IsType<BinaryExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Equal(BinaryOperator.FloorDivide, binary.Operator);
    }

    [Fact]
    public void ParsesConditionalExpression()
    {
        var expr = StarlarkParser.ParseExpression("1 if True else 0");

        var conditional = Assert.IsType<ConditionalExpression>(SyntaxNormalization.Normalize(expr));
        Assert.Equal(Lit(true), conditional.Condition);
        Assert.Equal(Lit(1L), conditional.ThenExpression);
        Assert.Equal(Lit(0L), conditional.ElseExpression);
    }

    [Fact]
    public void ParsesStringEscapes()
    {
        var expr = StarlarkParser.ParseExpression("\"a\\n\\t\"");

        Assert.Equal(SyntaxNormalization.Normalize(Lit("a\n\t")), SyntaxNormalization.Normalize(expr));
    }

    [Fact]
    public void ParsesHexAndOctalEscapes()
    {
        var expr = StarlarkParser.ParseExpression("\"\\x41\\101\"");

        Assert.Equal(SyntaxNormalization.Normalize(Lit("AA")), SyntaxNormalization.Normalize(expr));
    }

    private static LiteralExpression Lit(object value) => new LiteralExpression(value, default);

    private static IdentifierExpression Id(string name) => new IdentifierExpression(name, default);

    private static BinaryExpression Bin(Expression left, BinaryOperator op, Expression right) =>
        new BinaryExpression(left, op, right, default);
}
