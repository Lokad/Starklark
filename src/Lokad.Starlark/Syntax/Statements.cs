using System.Collections.Generic;
using Lokad.Parsing;

namespace Lokad.Starlark.Syntax;

public abstract record Statement(SourceSpan Span);

public sealed record ExpressionStatement(Expression Expression, SourceSpan Span) : Statement(Span);

public sealed record AssignmentStatement(AssignmentTarget Target, Expression Value, SourceSpan Span) : Statement(Span);

public abstract record AssignmentTarget(SourceSpan Span);

public sealed record NameTarget(string Name, SourceSpan Span) : AssignmentTarget(Span);

public sealed record IndexTarget(Expression Target, Expression Index, SourceSpan Span) : AssignmentTarget(Span);

public sealed record TupleTarget(IReadOnlyList<AssignmentTarget> Items, SourceSpan Span) : AssignmentTarget(Span);

public sealed record ListTarget(IReadOnlyList<AssignmentTarget> Items, SourceSpan Span) : AssignmentTarget(Span);

public sealed record AugmentedAssignmentStatement(
    AssignmentTarget Target,
    BinaryOperator Operator,
    Expression Value,
    SourceSpan Span) : Statement(Span);

public sealed record IfClause(Expression Condition, IReadOnlyList<Statement> Statements, SourceSpan Span);

public sealed record IfStatement(
    IReadOnlyList<IfClause> Clauses,
    IReadOnlyList<Statement> ElseStatements,
    SourceSpan Span) : Statement(Span);

public sealed record ForStatement(
    AssignmentTarget Target,
    Expression Iterable,
    IReadOnlyList<Statement> Body,
    SourceSpan Span) : Statement(Span);

public sealed record FunctionDefinitionStatement(
    string Name,
    IReadOnlyList<FunctionParameter> Parameters,
    IReadOnlyList<Statement> Body,
    SourceSpan Span) : Statement(Span);

public sealed record FunctionParameter(string Name, Expression? Default, ParameterKind Kind);

public enum ParameterKind
{
    Normal,
    VarArgs,
    KwArgs
}

public sealed record ReturnStatement(Expression? Value, SourceSpan Span) : Statement(Span);

public sealed record BreakStatement(SourceSpan Span) : Statement(Span);

public sealed record ContinueStatement(SourceSpan Span) : Statement(Span);

public sealed record PassStatement(SourceSpan Span) : Statement(Span);

public sealed record LoadBinding(string Name, string Alias);

public sealed record LoadStatement(string Module, IReadOnlyList<LoadBinding> Bindings, SourceSpan Span) : Statement(Span);
