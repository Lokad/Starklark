using Lokad.Starlark.Parsing;
using Lokad.Starlark.Runtime;

namespace Lokad.Starlark;

public sealed class StarlarkInterpreter
{
    private readonly ExpressionEvaluator _evaluator = new ExpressionEvaluator();
    private readonly ModuleEvaluator _moduleEvaluator = new ModuleEvaluator();

    public StarlarkValue EvaluateExpression(string source, StarlarkEnvironment environment)
    {
        var expression = StarlarkParser.ParseExpression(source);
        return _evaluator.Evaluate(expression, environment);
    }

    public StarlarkValue? ExecuteModule(string source, StarlarkEnvironment environment)
    {
        var module = StarlarkModuleParser.ParseModule(source);
        return _moduleEvaluator.ExecuteModule(module, environment);
    }
}
