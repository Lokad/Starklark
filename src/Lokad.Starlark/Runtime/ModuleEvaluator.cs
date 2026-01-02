using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

public sealed class ModuleEvaluator
{
    private readonly ExpressionEvaluator _expressionEvaluator = new ExpressionEvaluator();

    public StarlarkValue? ExecuteModule(StarlarkModule module, StarlarkEnvironment environment)
    {
        StarlarkValue? lastValue = null;

        foreach (var statement in module.Statements)
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
                default:
                    throw new InvalidOperationException(
                        $"Unsupported statement type '{statement.GetType().Name}'.");
            }
        }

        return lastValue;
    }
}
