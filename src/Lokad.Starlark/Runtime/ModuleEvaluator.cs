using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

public sealed class ModuleEvaluator
{
    private readonly ExpressionEvaluator _expressionEvaluator = new ExpressionEvaluator();

    public StarlarkValue? ExecuteModule(StarlarkModule module, StarlarkEnvironment environment)
    {
        return ExecuteStatements(module.Statements, environment);
    }

    private StarlarkValue? ExecuteStatements(IReadOnlyList<Statement> statements, StarlarkEnvironment environment)
    {
        StarlarkValue? lastValue = null;

        foreach (var statement in statements)
        {
            switch (statement)
            {
                case AssignmentStatement assignment:
                    var value = _expressionEvaluator.Evaluate(assignment.Value, environment);
                    environment.Set(assignment.Name, value);
                    lastValue = null;
                    break;
                case ExpressionStatement expressionStatement:
                    lastValue = _expressionEvaluator.Evaluate(expressionStatement.Expression, environment);
                    break;
                case IfStatement ifStatement:
                    var condition = _expressionEvaluator.Evaluate(ifStatement.Condition, environment);
                    var branch = condition.IsTruthy
                        ? ifStatement.ThenStatements
                        : ifStatement.ElseStatements;
                    lastValue = ExecuteStatements(branch, environment);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported statement type '{statement.GetType().Name}'.");
            }
        }

        return lastValue;
    }
}
