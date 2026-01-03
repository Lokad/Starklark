using System.Collections.Generic;
using Lokad.Parsing;

namespace Lokad.Starlark.Syntax;

public abstract record Expression(SourceSpan Span);

public sealed record LiteralExpression(object Value, SourceSpan Span) : Expression(Span);

public sealed record IdentifierExpression(string Name, SourceSpan Span) : Expression(Span);

public sealed record UnaryExpression(UnaryOperator Operator, Expression Operand, SourceSpan Span) : Expression(Span);

public sealed record BinaryExpression(Expression Left, BinaryOperator Operator, Expression Right, SourceSpan Span) : Expression(Span);

public sealed record CallExpression(Expression Callee, IReadOnlyList<CallArgument> Arguments, SourceSpan Span) : Expression(Span);

public sealed record CallArgument(CallArgumentKind Kind, string? Name, Expression Value);

public sealed record ListExpression(IReadOnlyList<Expression> Items, SourceSpan Span) : Expression(Span);

public sealed record TupleExpression(IReadOnlyList<Expression> Items, SourceSpan Span) : Expression(Span);

public sealed record DictExpression(IReadOnlyList<DictEntry> Entries, SourceSpan Span) : Expression(Span);

public sealed record DictEntry(Expression Key, Expression Value);

public sealed record ListComprehensionExpression(
    Expression Body,
    IReadOnlyList<ComprehensionClause> Clauses,
    SourceSpan Span) : Expression(Span);

public sealed record DictComprehensionExpression(
    Expression Key,
    Expression Value,
    IReadOnlyList<ComprehensionClause> Clauses,
    SourceSpan Span) : Expression(Span);

public sealed record ComprehensionClause(
    ComprehensionClauseKind Kind,
    AssignmentTarget? Target,
    Expression? Iterable,
    Expression? Condition,
    SourceSpan Span);

public enum ComprehensionClauseKind
{
    For,
    If
}

public abstract record IndexSpecifier(SourceSpan Span);

public sealed record IndexValue(Expression Value, SourceSpan Span) : IndexSpecifier(Span);

public sealed record SliceIndex(Expression? Start, Expression? Stop, Expression? Step, SourceSpan Span) : IndexSpecifier(Span);

public sealed record IndexExpression(Expression Target, IndexSpecifier Index, SourceSpan Span) : Expression(Span);

public sealed record AttributeExpression(Expression Target, string Name, SourceSpan Span) : Expression(Span);

public sealed record ConditionalExpression(
    Expression Condition,
    Expression ThenExpression,
    Expression ElseExpression,
    SourceSpan Span) : Expression(Span);

public sealed record LambdaExpression(
    IReadOnlyList<FunctionParameter> Parameters,
    Expression Body,
    SourceSpan Span) : Expression(Span);

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
