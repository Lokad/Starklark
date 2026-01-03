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

public sealed record ListComprehensionExpression(
    Expression Body,
    IReadOnlyList<ComprehensionClause> Clauses) : Expression;

public sealed record DictComprehensionExpression(
    Expression Key,
    Expression Value,
    IReadOnlyList<ComprehensionClause> Clauses) : Expression;

public sealed record ComprehensionClause(
    ComprehensionClauseKind Kind,
    AssignmentTarget? Target,
    Expression? Iterable,
    Expression? Condition);

public enum ComprehensionClauseKind
{
    For,
    If
}

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
    Positive,
    Negate,
    BitwiseNot,
    Not
}

public enum BinaryOperator
{
    BitwiseOr,
    BitwiseXor,
    BitwiseAnd,
    ShiftLeft,
    ShiftRight,
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
    public const int MaxPriority = 8;

    public static int Priority(this BinaryOperator op)
    {
        return op switch
        {
            BinaryOperator.Multiply => 8,
            BinaryOperator.Divide => 8,
            BinaryOperator.FloorDivide => 8,
            BinaryOperator.Modulo => 8,
            BinaryOperator.Add => 7,
            BinaryOperator.Subtract => 7,
            BinaryOperator.ShiftLeft => 6,
            BinaryOperator.ShiftRight => 6,
            BinaryOperator.BitwiseAnd => 5,
            BinaryOperator.BitwiseXor => 4,
            BinaryOperator.BitwiseOr => 3,
            BinaryOperator.Equal => 2,
            BinaryOperator.NotEqual => 2,
            BinaryOperator.In => 2,
            BinaryOperator.NotIn => 2,
            BinaryOperator.Less => 2,
            BinaryOperator.LessEqual => 2,
            BinaryOperator.Greater => 2,
            BinaryOperator.GreaterEqual => 2,
            BinaryOperator.And => 1,
            BinaryOperator.Or => 0,
            _ => 0
        };
    }
}
