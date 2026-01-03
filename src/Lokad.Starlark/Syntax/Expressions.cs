using System.Collections.Generic;

namespace Lokad.Starlark.Syntax;

public abstract record Expression;

public sealed record LiteralExpression(object Value) : Expression;

public sealed record IdentifierExpression(string Name) : Expression;

public sealed record UnaryExpression(UnaryOperator Operator, Expression Operand) : Expression;

public sealed record BinaryExpression(Expression Left, BinaryOperator Operator, Expression Right) : Expression;

public sealed record CallExpression(Expression Callee, IReadOnlyList<CallArgument> Arguments) : Expression;

public sealed record CallArgument(CallArgumentKind Kind, string? Name, Expression Value);

public sealed record ListExpression(IReadOnlyList<Expression> Items) : Expression;

public sealed record TupleExpression(IReadOnlyList<Expression> Items) : Expression;

public sealed record DictExpression(IReadOnlyList<DictEntry> Entries) : Expression;

public sealed record DictEntry(Expression Key, Expression Value);

public abstract record IndexSpecifier;

public sealed record IndexValue(Expression Value) : IndexSpecifier;

public sealed record SliceIndex(Expression? Start, Expression? Stop, Expression? Step) : IndexSpecifier;

public sealed record IndexExpression(Expression Target, IndexSpecifier Index) : Expression;

public sealed record AttributeExpression(Expression Target, string Name) : Expression;

public sealed record ConditionalExpression(
    Expression Condition,
    Expression ThenExpression,
    Expression ElseExpression) : Expression;

public enum CallArgumentKind
{
    Positional,
    Keyword,
    Star,
    StarStar
}

public enum UnaryOperator
{
    Negate,
    Not
}

public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    FloorDivide,
    Modulo,
    Equal,
    NotEqual,
    In,
    NotIn,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,
    And,
    Or
}

public static class BinaryOperatorExtensions
{
    public const int MaxPriority = 3;

    public static int Priority(this BinaryOperator op)
    {
        return op switch
        {
            BinaryOperator.Multiply => 3,
            BinaryOperator.Divide => 3,
            BinaryOperator.FloorDivide => 3,
            BinaryOperator.Modulo => 3,
            BinaryOperator.Add => 2,
            BinaryOperator.Subtract => 2,
            BinaryOperator.Equal => 1,
            BinaryOperator.NotEqual => 1,
            BinaryOperator.In => 1,
            BinaryOperator.NotIn => 1,
            BinaryOperator.Less => 1,
            BinaryOperator.LessEqual => 1,
            BinaryOperator.Greater => 1,
            BinaryOperator.GreaterEqual => 1,
            BinaryOperator.And => 0,
            BinaryOperator.Or => 0,
            _ => 0
        };
    }
}
