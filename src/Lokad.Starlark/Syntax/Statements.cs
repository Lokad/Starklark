using System.Collections.Generic;

namespace Lokad.Starlark.Syntax;

public abstract record Statement;

public sealed record ExpressionStatement(Expression Expression) : Statement;

public sealed record AssignmentStatement(string Name, Expression Value) : Statement;

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
    IReadOnlyList<string> Parameters,
    IReadOnlyList<Statement> Body) : Statement;

public sealed record ReturnStatement(Expression? Value) : Statement;

public sealed record BreakStatement : Statement;

public sealed record ContinueStatement : Statement;

public sealed record PassStatement : Statement;
