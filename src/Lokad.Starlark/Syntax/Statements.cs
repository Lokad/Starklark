using System.Collections.Generic;

namespace Lokad.Starlark.Syntax;

public abstract record Statement;

public sealed record ExpressionStatement(Expression Expression) : Statement;

public sealed record AssignmentStatement(AssignmentTarget Target, Expression Value) : Statement;

public abstract record AssignmentTarget;

public sealed record NameTarget(string Name) : AssignmentTarget;

public sealed record IndexTarget(Expression Target, Expression Index) : AssignmentTarget;

public sealed record TupleTarget(IReadOnlyList<AssignmentTarget> Items) : AssignmentTarget;

public sealed record ListTarget(IReadOnlyList<AssignmentTarget> Items) : AssignmentTarget;

public sealed record AugmentedAssignmentStatement(
    AssignmentTarget Target,
    BinaryOperator Operator,
    Expression Value) : Statement;

public sealed record IfClause(Expression Condition, IReadOnlyList<Statement> Statements);

public sealed record IfStatement(
    IReadOnlyList<IfClause> Clauses,
    IReadOnlyList<Statement> ElseStatements) : Statement;

public sealed record ForStatement(
    string Name,
    Expression Iterable,
    IReadOnlyList<Statement> Body) : Statement;

public sealed record FunctionDefinitionStatement(
    string Name,
    IReadOnlyList<FunctionParameter> Parameters,
    IReadOnlyList<Statement> Body) : Statement;

public sealed record FunctionParameter(string Name, Expression? Default);

public sealed record ReturnStatement(Expression? Value) : Statement;

public sealed record BreakStatement : Statement;

public sealed record ContinueStatement : Statement;

public sealed record PassStatement : Statement;

public sealed record LoadBinding(string Name, string Alias);

public sealed record LoadStatement(string Module, IReadOnlyList<LoadBinding> Bindings) : Statement;
