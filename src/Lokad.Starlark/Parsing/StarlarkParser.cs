using System.Globalization;
using System.Linq;
using Lokad.Parsing;
using Lokad.Parsing.Error;
using Lokad.Parsing.Parser;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Parsing;

public sealed class StarlarkParser : GrammarParser<StarlarkParser, Token, ExpressionRoot>
{
    public StarlarkParser() : base(TokenNamer.Instance) { }

    public static Expression ParseExpression(string source)
    {
        var parser = new StarlarkParser();
        var tokens = parser.Tokens = MakeTokenReader().ReadAllTokens(source);

        if (tokens.HasInvalidTokens)
        {
            var t = tokens.Tokens.First(tok => tok.Token == Token.Error);
            tokens.LineOfPosition(t.Start, out var line, out var col);
            var location = new SourceLocation(t.Start, line, col);
            throw new StarlarkParseException(
                $"Invalid character: '{source[t.Start]}'.",
                new SourceSpan(location, t.Length));
        }

        try
        {
            return StreamParser(parser, tokens).Expression;
        }
        catch (ParseException ex)
        {
            throw new StarlarkParseException(
                $"Found `{ex.Token}` but expected {string.Concat(", ", ex.Expected)}.",
                ex.Location,
                ex);
        }
    }

    [Rule]
    public ExpressionRoot Root(
        [NT] Expression expr,
        [L] LineEnding[] trailing,
        [T(Token.EoS)] Token eos)
    {
        return new ExpressionRoot(expr);
    }

    [Rule]
    public Expression TrueLiteral([T(Token.True)] Token value) => new LiteralExpression(true);

    [Rule]
    public Expression FalseLiteral([T(Token.False)] Token value) => new LiteralExpression(false);

    [Rule]
    public Expression NumberLiteral([T(Token.Number)] string value)
    {
        if (value.Contains('.') || value.Contains('e') || value.Contains('E'))
        {
            return new LiteralExpression(double.Parse(value, CultureInfo.InvariantCulture));
        }

        return new LiteralExpression(long.Parse(value, CultureInfo.InvariantCulture));
    }

    [Rule]
    public Expression StringLiteral([T(Token.String)] string value)
    {
        var text = value.Length >= 2 ? value.Substring(1, value.Length - 2) : string.Empty;
        return new LiteralExpression(text);
    }

    [Rule]
    public Expression Identifier([T(Token.Id)] string name) => new IdentifierExpression(name);

    [Rule]
    public Expression Parenthesis(
        [T(Token.OpenParen)] Token open,
        [NT] Expression inner,
        [T(Token.CloseParen)] Token close)
    {
        return inner;
    }

    [Rule(Rank = 1)]
    public Expression Unary(
        [T(Token.Minus, Token.Not)] Token op,
        [NT(1)] Expression operand)
    {
        return new UnaryExpression(
            op == Token.Minus ? UnaryOperator.Negate : UnaryOperator.Not,
            operand);
    }

    public struct InfixRight
    {
        public BinaryOperator Operator { get; set; }
        public Expression Right { get; set; }
    }

    [Rule]
    public InfixRight AndThen(
        [T(Token.Plus, Token.Minus, Token.Star, Token.Slash, Token.Equal, Token.NotEqual, Token.And, Token.Or)] Token op,
        [NT(1)] Expression right)
    {
        return new InfixRight
        {
            Operator = op switch
            {
                Token.Plus => BinaryOperator.Add,
                Token.Minus => BinaryOperator.Subtract,
                Token.Star => BinaryOperator.Multiply,
                Token.Slash => BinaryOperator.Divide,
                Token.Equal => BinaryOperator.Equal,
                Token.NotEqual => BinaryOperator.NotEqual,
                Token.And => BinaryOperator.And,
                Token.Or => BinaryOperator.Or,
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            },
            Right = right
        };
    }

    [Rule(Rank = 2)]
    public Expression Binary(
        [NT(1)] Expression left,
        [L(Min = 1)] InfixRight[] right)
    {
        if (right.Length == 1)
        {
            return new BinaryExpression(left, right[0].Operator, right[0].Right);
        }

        var length = right.Length;
        for (var priority = BinaryOperatorExtensions.MaxPriority; length > 0; --priority)
        {
            var j = 0;
            for (var i = 0; i < length; ++i, ++j)
            {
                if (right[i].Operator.Priority() == priority)
                {
                    var myLeft = j == 0 ? left : right[j - 1].Right;
                    var expr = new BinaryExpression(myLeft, right[i].Operator, right[i].Right);

                    if (j == 0)
                    {
                        left = expr;
                    }
                    else
                    {
                        right[j - 1].Right = expr;
                    }

                    --j;
                }
                else
                {
                    right[j] = right[i];
                }
            }

            length = j;
        }

        return left;
    }

    [Rule]
    public LineEnding LineEnding([T(Token.EoL)] Token token) => new LineEnding();
}

public readonly record struct ExpressionRoot(Expression Expression);
public readonly record struct LineEnding;

public sealed class StarlarkParseException : Exception
{
    public SourceSpan Location { get; }

    public StarlarkParseException(string message, SourceSpan location, Exception? inner = null)
        : base(message, inner)
    {
        Location = location;
    }
}

public class TAttribute : TerminalAttribute
{
    public TAttribute(params Token[] read) : base(read.Select(t => (int)t)) { }
}

public class LAttribute : ListAttribute
{
    public LAttribute(int maxRank = -1) : base(maxRank) { }
}

public class NTAttribute : NonTerminalAttribute
{
    public NTAttribute(int maxRank = -1) : base(maxRank) { }
}
