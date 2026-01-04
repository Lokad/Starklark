using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lokad.Parsing;
using Lokad.Parsing.Parser;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Parsing;

public abstract class StarlarkGrammar<TSelf, TResult> : GrammarParser<TSelf, Token, TResult>
    where TSelf : StarlarkGrammar<TSelf, TResult>
{
    protected StarlarkGrammar() : base(TokenNamer.Instance) { }

    protected static SourceSpan SpanBetween(SourceSpan start, SourceSpan end) => start.MergeWith(end);

    protected static SourceSpan SpanBetween(Expression start, Expression end) => start.Span.MergeWith(end.Span);

    protected static SourceSpan SpanFromToken(Pos<string> token) => token.Location;

    [Rule]
    public Expression TrueLiteral([T(Token.True)] Pos<string> value) =>
        new LiteralExpression(true, value.Location);

    [Rule]
    public Expression FalseLiteral([T(Token.False)] Pos<string> value) =>
        new LiteralExpression(false, value.Location);

    [Rule]
    public Expression NoneLiteral([T(Token.None)] Pos<string> value) =>
        new LiteralExpression(null!, value.Location);

    [Rule]
    public Expression NumberLiteral([T(Token.Number)] Pos<string> value)
    {
        var text = value.Value;
        if (text.Contains('.') || text.Contains('e') || text.Contains('E'))
        {
            return new LiteralExpression(double.Parse(text, CultureInfo.InvariantCulture), value.Location);
        }

        var (baseValue, sliceStart) = text switch
        {
            [ '0', 'x' or 'X', .. ] => (16, 2),
            [ '0', 'o' or 'O', .. ] => (8, 2),
            [ '0', 'b' or 'B', .. ] => (2, 2),
            _ => (10, 0)
        };

        var literal = sliceStart == 0 ? text : text[sliceStart..];
        return new LiteralExpression(Convert.ToInt64(literal, baseValue), value.Location);
    }

    [Rule]
    public Expression StringLiteral([T(Token.String)] Pos<string> value)
    {
        return new LiteralExpression(StringLiteralParser.Parse(value.Value), value.Location);
    }

    [Rule]
    public Expression Identifier([T(Token.Id)] Pos<string> name) =>
        new IdentifierExpression(name.Value, name.Location);

    [Rule]
    public Expression Parenthesis(
        [T(Token.OpenParen)] Pos<string> open,
        [NT] Expression inner,
        [T(Token.CloseParen)] Pos<string> close)
    {
        return inner;
    }

    [Rule]
    public Expression EmptyTupleLiteral(
        [T(Token.OpenParen)] Pos<string> open,
        [T(Token.CloseParen)] Pos<string> close)
    {
        return new TupleExpression(Array.Empty<Expression>(), SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public Expression TupleLiteral(
        [T(Token.OpenParen)] Pos<string> open,
        [L(Sep = Token.Comma, Min = 2)] Expression[] items,
        [O(Token.TrailingComma)] Token? trailing,
        [T(Token.CloseParen)] Pos<string> close)
    {
        return new TupleExpression(items, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public Expression SingleTupleLiteral(
        [T(Token.OpenParen)] Pos<string> open,
        [NT] Expression item,
        [T(Token.Comma, Token.TrailingComma)] Pos<string> comma,
        [T(Token.CloseParen)] Pos<string> close)
    {
        return new TupleExpression(new[] { item }, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public Expression Call(
        [NT(0)] Expression callee,
        [T(Token.OpenParen)] Pos<string> open,
        [L(Sep = Token.Comma, Min = 1)] CallArgument[] args,
        [O(Token.TrailingComma)] Token? trailing,
        [L] LineEnding[] lineEndings,
        [T(Token.CloseParen)] Pos<string> close)
    {
        return new CallExpression(callee, args, SpanBetween(callee.Span, close.Location));
    }

    [Rule]
    public Expression CallEmpty(
        [NT(0)] Expression callee,
        [T(Token.OpenParen)] Pos<string> open,
        [L] LineEnding[] lineEndings,
        [T(Token.CloseParen)] Pos<string> close)
    {
        return new CallExpression(callee, Array.Empty<CallArgument>(), SpanBetween(callee.Span, close.Location));
    }

    [Rule]
    public CallArgument NamedArgument(
        [L] LineEnding[] lineEndings,
        [T(Token.Id)] Pos<string> name,
        [T(Token.Assign)] Pos<string> assign,
        [NT(3)] Expression value)
    {
        return new CallArgument(CallArgumentKind.Keyword, name.Value, value);
    }

    [Rule]
    public CallArgument PositionalArgument([L] LineEnding[] lineEndings, [NT(3)] Expression value)
    {
        return new CallArgument(CallArgumentKind.Positional, null, value);
    }

    [Rule]
    public CallArgument StarArgument(
        [L] LineEnding[] lineEndings,
        [T(Token.Star)] Pos<string> star,
        [NT(3)] Expression value)
    {
        return new CallArgument(CallArgumentKind.Star, null, value);
    }

    [Rule]
    public CallArgument StarStarArgument(
        [L] LineEnding[] lineEndings,
        [T(Token.StarStar)] Pos<string> star,
        [NT(3)] Expression value)
    {
        return new CallArgument(CallArgumentKind.StarStar, null, value);
    }

    [Rule]
    public Expression Attribute(
        [NT(0)] Expression target,
        [T(Token.Dot)] Pos<string> dot,
        [T(Token.Id)] Pos<string> name)
    {
        return new AttributeExpression(target, name.Value, SpanBetween(target.Span, name.Location));
    }

    [Rule]
    public Expression ListComprehension(
        [T(Token.OpenBracket)] Pos<string> open,
        [NT] Expression body,
        [NT] ComprehensionFor forClause,
        [L] ComprehensionQualifier[] qualifiers,
        [T(Token.CloseBracket)] Pos<string> close)
    {
        var clauses = new ComprehensionClause[qualifiers.Length + 1];
        clauses[0] = forClause.Clause;
        for (var i = 0; i < qualifiers.Length; i++)
        {
            clauses[i + 1] = qualifiers[i].Clause;
        }

        return new ListComprehensionExpression(body, clauses, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public Expression ListLiteralMultiple(
        [T(Token.OpenBracket)] Pos<string> open,
        [L(Sep = Token.Comma, Min = 2)] Expression[] items,
        [O(Token.TrailingComma)] Token? trailing,
        [T(Token.CloseBracket)] Pos<string> close)
    {
        return new ListExpression(items, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public Expression ListLiteralSingle(
        [T(Token.OpenBracket)] Pos<string> open,
        [NT] Expression item,
        [O(Token.TrailingComma)] Token? trailing,
        [T(Token.CloseBracket)] Pos<string> close)
    {
        return new ListExpression(new[] { item }, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public Expression ListLiteralEmpty(
        [T(Token.OpenBracket)] Pos<string> open,
        [T(Token.CloseBracket)] Pos<string> close)
    {
        return new ListExpression(Array.Empty<Expression>(), SpanBetween(open.Location, close.Location));
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
    public Expression DictComprehension(
        [T(Token.OpenBrace)] Pos<string> open,
        [NT(3)] Expression key,
        [T(Token.Colon)] Pos<string> colon,
        [NT(3)] Expression value,
        [NT] ComprehensionFor forClause,
        [L] ComprehensionQualifier[] qualifiers,
        [T(Token.CloseBrace)] Pos<string> close)
    {
        var clauses = new ComprehensionClause[qualifiers.Length + 1];
        clauses[0] = forClause.Clause;
        for (var i = 0; i < qualifiers.Length; i++)
        {
            clauses[i + 1] = qualifiers[i].Clause;
        }

        return new DictComprehensionExpression(key, value, clauses, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public Expression DictLiteral(
        [T(Token.OpenBrace)] Pos<string> open,
        [L(Sep = Token.Comma, Min = 1)] DictEntry[] entries,
        [O(Token.TrailingComma)] Token? trailing,
        [T(Token.CloseBrace)] Pos<string> close)
    {
        return new DictExpression(entries, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public Expression DictLiteralEmpty(
        [T(Token.OpenBrace)] Pos<string> open,
        [T(Token.CloseBrace)] Pos<string> close)
    {
        return new DictExpression(Array.Empty<DictEntry>(), SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public ComprehensionFor ComprehensionForClause(
        [T(Token.For)] Pos<string> keyword,
        [NT] AssignmentTarget target,
        [T(Token.In)] Pos<string> inKeyword,
        [NT(2)] Expression iterable)
    {
        return new ComprehensionFor(
            new ComprehensionClause(
                ComprehensionClauseKind.For,
                target,
                iterable,
                null,
                SpanBetween(keyword.Location, iterable.Span)));
    }

    [Rule]
    public ComprehensionQualifier ComprehensionQualifierFor(
        [T(Token.For)] Pos<string> keyword,
        [NT] AssignmentTarget target,
        [T(Token.In)] Pos<string> inKeyword,
        [NT(2)] Expression iterable)
    {
        return new ComprehensionQualifier(
            new ComprehensionClause(
                ComprehensionClauseKind.For,
                target,
                iterable,
                null,
                SpanBetween(keyword.Location, iterable.Span)));
    }

    [Rule]
    public ComprehensionQualifier ComprehensionQualifierIf(
        [T(Token.If)] Pos<string> keyword,
        [NT] Expression condition)
    {
        return new ComprehensionQualifier(
            new ComprehensionClause(
                ComprehensionClauseKind.If,
                null,
                null,
                condition,
                SpanBetween(keyword.Location, condition.Span)));
    }

    [Rule]
    public AssignmentTarget TargetName([T(Token.Id)] Pos<string> name)
    {
        return new NameTarget(name.Value, name.Location);
    }

    [Rule]
    public AssignmentTarget TargetIndex(
        [NT(0)] Expression target,
        [T(Token.OpenBracket)] Pos<string> open,
        [NT] Expression index,
        [T(Token.CloseBracket)] Pos<string> close)
    {
        return new IndexTarget(target, index, SpanBetween(target.Span, close.Location));
    }

    [Rule]
    public AssignmentTarget TargetTuple(
        [T(Token.OpenParen)] Pos<string> open,
        [L(Sep = Token.Comma, Min = 2)] AssignmentTarget[] items,
        [O(Token.TrailingComma)] Token? trailing,
        [T(Token.CloseParen)] Pos<string> close)
    {
        return new TupleTarget(items, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public AssignmentTarget TargetSingleTuple(
        [T(Token.OpenParen)] Pos<string> open,
        [NT] AssignmentTarget item,
        [T(Token.Comma, Token.TrailingComma)] Pos<string> comma,
        [T(Token.CloseParen)] Pos<string> close)
    {
        return new TupleTarget(new[] { item }, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public AssignmentTarget TargetList(
        [T(Token.OpenBracket)] Pos<string> open,
        [L(Sep = Token.Comma, Min = 1)] AssignmentTarget[] items,
        [O(Token.TrailingComma)] Token? trailing,
        [T(Token.CloseBracket)] Pos<string> close)
    {
        return new ListTarget(items, SpanBetween(open.Location, close.Location));
    }

    [Rule]
    public AssignmentTarget TargetTupleLoose(
        [L(Sep = Token.Comma, Min = 2)] AssignmentTarget[] items)
    {
        return new TupleTarget(items, SpanBetween(items[0].Span, items[^1].Span));
    }

    [Rule]
    public Expression Index(
        [NT(0)] Expression target,
        [T(Token.OpenBracket)] Pos<string> open,
        [NT] IndexSpecifier index,
        [T(Token.CloseBracket)] Pos<string> close)
    {
        return new IndexExpression(target, index, SpanBetween(target.Span, close.Location));
    }

    [Rule]
    public IndexSpecifier IndexValue([NT] Expression value)
    {
        return new IndexValue(value, value.Span);
    }

    [Rule]
    public IndexSpecifier SliceIndex(
        [NTO] Expression? start,
        [T(Token.Colon)] Pos<string> colon,
        [NTO] Expression? stop,
        [NTO] SliceStep? step)
    {
        var spanStart = start?.Span ?? colon.Location;
        var spanEnd = step?.Step?.Span ?? stop?.Span ?? colon.Location;
        return new SliceIndex(start, stop, step?.Step, SpanBetween(spanStart, spanEnd));
    }

    [Rule]
    public SliceStep SliceStep(
        [T(Token.Colon)] Pos<string> colon,
        [NTO] Expression? step)
    {
        var spanEnd = step?.Span ?? colon.Location;
        return new SliceStep(step, SpanBetween(colon.Location, spanEnd));
    }

    [Rule]
    public FunctionParameter ParameterName([L] LineEnding[] lineEndings, [T(Token.Id)] string name)
    {
        return new FunctionParameter(name, null, ParameterKind.Normal);
    }

    [Rule]
    public FunctionParameter ParameterDefault(
        [L] LineEnding[] lineEndings,
        [T(Token.Id)] string name,
        [T(Token.Assign)] Token assign,
        [NT(2)] Expression value)
    {
        return new FunctionParameter(name, value, ParameterKind.Normal);
    }

    [Rule]
    public FunctionParameter ParameterVarArgs(
        [L] LineEnding[] lineEndings,
        [T(Token.Star)] Token star,
        [T(Token.Id)] string name)
    {
        return new FunctionParameter(name, null, ParameterKind.VarArgs);
    }

    [Rule]
    public FunctionParameter ParameterKwArgs(
        [L] LineEnding[] lineEndings,
        [T(Token.StarStar)] Token star,
        [T(Token.Id)] string name)
    {
        return new FunctionParameter(name, null, ParameterKind.KwArgs);
    }

    [Rule(Rank = 1)]
    public Expression Unary(
        [T(Token.Plus, Token.Minus, Token.Tilde)] Pos<string> op,
        [NT(1)] Expression operand)
    {
        var unaryOperator = op.Value switch
        {
            "+" => UnaryOperator.Positive,
            "-" => UnaryOperator.Negate,
            "~" => UnaryOperator.BitwiseNot,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op.Value, null)
        };

        return new UnaryExpression(unaryOperator, operand, SpanBetween(op.Location, operand.Span));
    }

    public struct InfixRight
    {
        public BinaryOperator Operator { get; set; }
        public Expression Right { get; set; }
    }

    [Rule]
    public InfixRight AndThen(
        [T(Token.Plus, Token.Minus, Token.Star, Token.Slash, Token.FloorDivide, Token.Percent, Token.Equal, Token.NotEqual, Token.In, Token.Less, Token.LessEqual, Token.Greater, Token.GreaterEqual, Token.And, Token.Or, Token.Pipe, Token.Caret, Token.Ampersand, Token.ShiftLeft, Token.ShiftRight)] Token op,
        [NT(1)] Expression right)
    {
        return new InfixRight
        {
            Operator = op switch
            {
                Token.Pipe => BinaryOperator.BitwiseOr,
                Token.Caret => BinaryOperator.BitwiseXor,
                Token.Ampersand => BinaryOperator.BitwiseAnd,
                Token.ShiftLeft => BinaryOperator.ShiftLeft,
                Token.ShiftRight => BinaryOperator.ShiftRight,
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

    [Rule(Rank = 3)]
    public Expression NotExpression(
        [T(Token.Not)] Pos<string> keyword,
        [NT(3)] Expression operand)
    {
        return new UnaryExpression(UnaryOperator.Not, operand, SpanBetween(keyword.Location, operand.Span));
    }

    [Rule(Rank = 2)]
    public Expression Binary(
        [NT(1)] Expression left,
        [L(Min = 1)] InfixRight[] right)
    {
        if (right.Length == 1)
        {
            return new BinaryExpression(
                left,
                right[0].Operator,
                right[0].Right,
                SpanBetween(left.Span, right[0].Right.Span));
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
                    var expr = new BinaryExpression(
                        myLeft,
                        right[i].Operator,
                        right[i].Right,
                        SpanBetween(myLeft.Span, right[i].Right.Span));

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
        [T(Token.If)] Pos<string> keyword,
        [NT(2)] Expression condition,
        [T(Token.Else)] Pos<string> elseKeyword,
        [NT(3)] Expression elseExpression)
    {
        return new ConditionalExpression(
            condition,
            thenExpression,
            elseExpression,
            SpanBetween(thenExpression.Span, elseExpression.Span));
    }

    [Rule(Rank = 5)]
    public Expression Lambda(
        [T(Token.Lambda)] Pos<string> keyword,
        [L(Sep = Token.Comma)] FunctionParameter[] parameters,
        [O(Token.TrailingComma)] Token? trailing,
        [T(Token.Colon)] Pos<string> colon,
        [NT(5)] Expression body)
    {
        ValidateParameters(parameters, "lambda");
        return new LambdaExpression(parameters, body, SpanBetween(keyword.Location, body.Span));
    }

    [Rule(Rank = 4)]
    public Expression TupleExpression(
        [NT(2)] Expression first,
        [T(Token.Comma)] Pos<string> comma,
        [L(Sep = Token.Comma)] Expression[] rest)
    {
        var items = new Expression[rest.Length + 1];
        items[0] = first;
        for (var i = 0; i < rest.Length; i++)
        {
            items[i + 1] = rest[i];
        }

        return new TupleExpression(items, SpanBetween(first.Span, items[^1].Span));
    }

    [Rule(Rank = 4)]
    public Expression TupleExpressionTrailingComma(
        [NT(2)] Expression first,
        [T(Token.Comma)] Pos<string> comma,
        [L(End = Token.Comma, Min = 1)] Expression[] rest)
    {
        var items = new Expression[rest.Length + 1];
        items[0] = first;
        for (var i = 0; i < rest.Length; i++)
        {
            items[i + 1] = rest[i];
        }

        var spanEnd = rest.Length > 0 ? items[^1].Span : comma.Location;
        return new TupleExpression(items, SpanBetween(first.Span, spanEnd));
    }

    [Rule(Rank = 4)]
    public Expression TupleExpressionSingle(
        [NT(2)] Expression first,
        [T(Token.Comma)] Pos<string> comma)
    {
        return new TupleExpression(new[] { first }, SpanBetween(first.Span, comma.Location));
    }


    [Rule]
    public LineEnding LineEnding([T(Token.EoL)] Token token) => new LineEnding();

    internal static string UnescapeString(string text)
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

    protected static AssignmentTarget ToAssignmentTarget(Expression expression)
    {
        return expression switch
        {
            IdentifierExpression identifier => new NameTarget(identifier.Name, identifier.Span),
            IndexExpression index => ToIndexTarget(index),
            ListExpression list => new ListTarget(
                list.Items.Select(ToAssignmentTarget).ToArray(),
                list.Span),
            TupleExpression tuple => new TupleTarget(
                tuple.Items.Select(ToAssignmentTarget).ToArray(),
                tuple.Span),
            _ => throw new InvalidOperationException(
                $"Invalid assignment target '{expression.GetType().Name}'.")
        };
    }

    private static AssignmentTarget ToIndexTarget(IndexExpression index)
    {
        if (index.Index is not IndexValue indexValue)
        {
            throw new InvalidOperationException("Slice assignment is not supported.");
        }

        return new IndexTarget(index.Target, indexValue.Value, index.Span);
    }

    protected static void ValidateParameters(IReadOnlyList<FunctionParameter> parameters, string functionName)
    {
        var seenDefault = false;
        var seenVarArgs = false;
        var seenKwArgs = false;
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            if (parameter.Kind == ParameterKind.VarArgs)
            {
                if (seenVarArgs)
                {
                    throw new InvalidOperationException(
                        $"Multiple *args parameters in '{functionName}'.");
                }

                if (seenKwArgs)
                {
                    throw new InvalidOperationException(
                        $"*args must appear before **kwargs in '{functionName}'.");
                }

                if (parameter.Default != null)
                {
                    throw new InvalidOperationException(
                        $"*args parameter '{parameter.Name}' cannot have a default in '{functionName}'.");
                }

                seenVarArgs = true;
                continue;
            }

            if (parameter.Kind == ParameterKind.KwArgs)
            {
                if (seenKwArgs)
                {
                    throw new InvalidOperationException(
                        $"Multiple **kwargs parameters in '{functionName}'.");
                }

                if (parameter.Default != null)
                {
                    throw new InvalidOperationException(
                        $"**kwargs parameter '{parameter.Name}' cannot have a default in '{functionName}'.");
                }

                seenKwArgs = true;
                continue;
            }

            if (seenVarArgs || seenKwArgs)
            {
                throw new InvalidOperationException(
                    $"Parameter '{parameter.Name}' must appear before *args or **kwargs in '{functionName}'.");
            }

            if (parameter.Default != null)
            {
                seenDefault = true;
                continue;
            }

            if (seenDefault)
            {
                throw new InvalidOperationException(
                    $"Non-default parameter '{parameter.Name}' follows default parameter in '{functionName}'.");
            }
        }
    }
}

public readonly record struct LineEnding;

public readonly record struct SliceStep(Expression? Step, SourceSpan Span);

public readonly record struct ComprehensionFor(ComprehensionClause Clause);

public readonly record struct ComprehensionQualifier(ComprehensionClause Clause);

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
