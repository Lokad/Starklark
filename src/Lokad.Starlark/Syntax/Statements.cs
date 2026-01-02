namespace Lokad.Starlark.Syntax;

public abstract record Statement;

public sealed record ExpressionStatement(Expression Expression) : Statement;

public sealed record AssignmentStatement(string Name, Expression Value) : Statement;
