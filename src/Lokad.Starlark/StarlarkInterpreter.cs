using Lokad.Starlark.Parsing;
using Lokad.Starlark.Runtime;

namespace Lokad.Starlark;

public sealed class StarlarkInterpreter
{
    private readonly ExpressionEvaluator _evaluator = new ExpressionEvaluator();
    private readonly ModuleEvaluator _moduleEvaluator = new ModuleEvaluator();

    public StarlarkValue EvaluateExpression(string source, StarlarkEnvironment environment)
    {
        return EvaluateExpression(source, environment, null);
    }

    public StarlarkValue EvaluateExpression(
        string source,
        StarlarkEnvironment environment,
        StarlarkExecutionOptions? options)
    {
        var guard = new ExecutionGuard(options);
        var previous = environment.SwapGuard(guard);
        try
        {
            var expression = StarlarkParser.ParseExpression(source);
            return _evaluator.Evaluate(expression, environment);
        }
        finally
        {
            environment.SwapGuard(previous);
        }
    }

    public StarlarkValue? ExecuteModule(string source, StarlarkEnvironment environment)
    {
        return ExecuteModule(source, environment, null);
    }

    public StarlarkValue? ExecuteModule(
        string source,
        StarlarkEnvironment environment,
        StarlarkExecutionOptions? options)
    {
        var guard = new ExecutionGuard(options);
        var previous = environment.SwapGuard(guard);
        try
        {
            var module = StarlarkModuleParser.ParseModule(source);
            return _moduleEvaluator.ExecuteModule(module, environment);
        }
        finally
        {
            environment.SwapGuard(previous);
        }
    }
}
