using System.Globalization;
using Lokad.Parsing;
using Lokad.Parsing.Parser;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Parsing;

public abstract class StarlarkGrammar<TSelf, TResult> : GrammarParser<TSelf, Token, TResult>
    where TSelf : StarlarkGrammar<TSelf, TResult>
{
    protected StarlarkGrammar() : base(TokenNamer.Instance) { }

    [Rule]
    public Expression TrueLiteral([T(Token.True)] Token value) => new LiteralExpression(true);

    [Rule]
    public Expression FalseLiteral([T(Token.False)] Token value) => new LiteralExpression(false);

    [Rule]
    public Expression NoneLiteral([T(Token.None)] Token value) => new LiteralExpression(null!);

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
        text = UnescapeString(text);
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

    [Rule]
    public Expression TupleLiteral(
        [T(Token.OpenParen)] Token open,
        [L(Sep = Token.Comma, Min = 2)] Expression[] items,
        [T(Token.CloseParen)] Token close)
    {
        return new TupleExpression(items);
    }

    [Rule]
    public Expression SingleTupleLiteral(
        [T(Token.OpenParen)] Token open,
        [NT] Expression item,
        [T(Token.Comma)] Token comma,
        [T(Token.CloseParen)] Token close)
    {
        return new TupleExpression(new[] { item });
    }

    [Rule]
    public Expression Call(
        [NT(0)] Expression callee,
        [T(Token.OpenParen)] Token open,
        [L(Sep = Token.Comma)] Expression[] args,
        [T(Token.CloseParen)] Token close)
    {
        return new CallExpression(callee, args);
    }

    [Rule]
    public Expression ListLiteral(
        [T(Token.OpenBracket)] Token open,
        [L(Sep = Token.Comma)] Expression[] items,
        [T(Token.CloseBracket)] Token close)
    {
        return new ListExpression(items);
    }

    [Rule]
    public DictEntry DictEntry(
        [NT(3)] Expression key,
        [T(Token.Colon)] Token colon,
        [NT(3)] Expression value)
    {
        return new DictEntry(key, value);
    }

    [Rule]
    public Expression DictLiteral(
        [T(Token.OpenBrace)] Token open,
        [L(Sep = Token.Comma)] DictEntry[] entries,
        [T(Token.CloseBrace)] Token close)
    {
        return new DictExpression(entries);
    }

    [Rule]
    public Expression Index(
        [NT(0)] Expression target,
        [T(Token.OpenBracket)] Token open,
        [NT] Expression index,
        [T(Token.CloseBracket)] Token close)
    {
        return new IndexExpression(target, index);
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
        [T(Token.Plus, Token.Minus, Token.Star, Token.Slash, Token.FloorDivide, Token.Percent, Token.Equal, Token.NotEqual, Token.In, Token.Less, Token.LessEqual, Token.Greater, Token.GreaterEqual, Token.And, Token.Or)] Token op,
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
                Token.FloorDivide => BinaryOperator.FloorDivide,
                Token.Percent => BinaryOperator.Modulo,
                Token.Equal => BinaryOperator.Equal,
                Token.NotEqual => BinaryOperator.NotEqual,
                Token.In => BinaryOperator.In,
                Token.Less => BinaryOperator.Less,
                Token.LessEqual => BinaryOperator.LessEqual,
                Token.Greater => BinaryOperator.Greater,
                Token.GreaterEqual => BinaryOperator.GreaterEqual,
                Token.And => BinaryOperator.And,
                Token.Or => BinaryOperator.Or,
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            },
            Right = right
        };
    }

    [Rule]
    public InfixRight NotInThen(
        [T(Token.Not)] Token notKeyword,
        [T(Token.In)] Token inKeyword,
        [NT(1)] Expression right)
    {
        return new InfixRight
        {
            Operator = BinaryOperator.NotIn,
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

    [Rule(Rank = 3)]
    public Expression Conditional(
        [NT(2)] Expression thenExpression,
        [T(Token.If)] Token keyword,
        [NT(2)] Expression condition,
        [T(Token.Else)] Token elseKeyword,
        [NT(3)] Expression elseExpression)
    {
        return new ConditionalExpression(condition, thenExpression, elseExpression);
    }

    [Rule(Rank = 4)]
    public Expression TupleExpression(
        [NT(2)] Expression first,
        [T(Token.Comma)] Token comma,
        [L(Sep = Token.Comma)] Expression[] rest)
    {
        var items = new Expression[rest.Length + 1];
        items[0] = first;
        for (var i = 0; i < rest.Length; i++)
        {
            items[i + 1] = rest[i];
        }

        return new TupleExpression(items);
    }


    [Rule]
    public LineEnding LineEnding([T(Token.EoL)] Token token) => new LineEnding();

    protected static string UnescapeString(string text)
    {
        if (text.IndexOf('\\') < 0)
        {
            return text;
        }

        var builder = new System.Text.StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch != '\\')
            {
                builder.Append(ch);
                continue;
            }

            if (i + 1 >= text.Length)
            {
                builder.Append('\\');
                break;
            }

            var next = text[++i];
            switch (next)
            {
                case '\\': builder.Append('\\'); break;
                case '"': builder.Append('"'); break;
                case '\'': builder.Append('\''); break;
                case 'n': builder.Append('\n'); break;
                case 'r': builder.Append('\r'); break;
                case 't': builder.Append('\t'); break;
                case 'a': builder.Append('\a'); break;
                case 'b': builder.Append('\b'); break;
                case 'f': builder.Append('\f'); break;
                case 'v': builder.Append('\v'); break;
                case 'x':
                    builder.Append(ReadHexEscape(text, ref i, 2));
                    break;
                case 'u':
                    builder.Append(ReadHexEscape(text, ref i, 4));
                    break;
                case 'U':
                    builder.Append(ReadHexEscape(text, ref i, 8));
                    break;
                default:
                    if (next is >= '0' and <= '7')
                    {
                        builder.Append(ReadOctalEscape(text, ref i, next));
                    }
                    else
                    {
                        builder.Append(next);
                    }
                    break;
            }
        }

        return builder.ToString();
    }

    private static char ReadHexEscape(string text, ref int index, int digits)
    {
        var value = 0;
        for (var j = 0; j < digits; j++)
        {
            if (index + 1 >= text.Length)
            {
                break;
            }

            var ch = text[index + 1];
            var digit = ch switch
            {
                >= '0' and <= '9' => ch - '0',
                >= 'a' and <= 'f' => 10 + (ch - 'a'),
                >= 'A' and <= 'F' => 10 + (ch - 'A'),
                _ => -1
            };

            if (digit < 0)
            {
                break;
            }

            value = (value << 4) + digit;
            index++;
        }

        return (char)value;
    }

    private static char ReadOctalEscape(string text, ref int index, char firstDigit)
    {
        var value = firstDigit - '0';
        var count = 1;

        while (count < 3 && index + 1 < text.Length)
        {
            var ch = text[index + 1];
            if (ch is < '0' or > '7')
            {
                break;
            }

            value = (value << 3) + (ch - '0');
            index++;
            count++;
        }

        return (char)value;
    }
}

public readonly record struct LineEnding;

public class TAttribute : TerminalAttribute
{
    public TAttribute(params Token[] read) : base(read.Select(t => (int)t)) { }
}

public class OAttribute : TerminalAttribute
{
    public OAttribute(params Token[] read) : base(read.Select(t => (int)t), true) { }
}

public class LAttribute : ListAttribute
{
    public LAttribute(int maxRank = -1) : base(maxRank) { }

    public Token Sep
    {
        get => (Token)(Separator ?? 0);
        set => Separator = (int)value;
    }

    public Token End
    {
        get => (Token)(Terminator ?? 0);
        set => Terminator = (int)value;
    }
}

public class NTAttribute : NonTerminalAttribute
{
    public NTAttribute(int maxRank = -1) : base(maxRank) { }
}

public class NTOAttribute : NonTerminalAttribute
{
    public NTOAttribute(int maxRank = -1) : base(maxRank, true) { }
}
