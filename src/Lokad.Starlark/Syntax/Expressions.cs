using System.Collections.Generic;

namespace Lokad.Starlark.Syntax;

public abstract record Expression;

public sealed record LiteralExpression(object Value) : Expression;

public sealed record IdentifierExpression(string Name) : Expression;

public sealed record UnaryExpression(UnaryOperator Operator, Expression Operand) : Expression;

public sealed record BinaryExpression(Expression Left, BinaryOperator Operator, Expression Right) : Expression;

public sealed record CallExpression(Expression Callee, IReadOnlyList<Expression> Arguments) : Expression;

public sealed record ListExpression(IReadOnlyList<Expression> Items) : Expression;

public sealed record TupleExpression(IReadOnlyList<Expression> Items) : Expression;

public sealed record DictExpression(IReadOnlyList<DictEntry> Entries) : Expression;

public sealed record DictEntry(Expression Key, Expression Value);

public sealed record IndexExpression(Expression Target, Expression Index) : Expression;

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
    Equal,
    NotEqual,
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
            BinaryOperator.Add => 2,
            BinaryOperator.Subtract => 2,
            BinaryOperator.Equal => 1,
            BinaryOperator.NotEqual => 1,
            BinaryOperator.And => 0,
            BinaryOperator.Or => 0,
            _ => 0
        };
    }
}
