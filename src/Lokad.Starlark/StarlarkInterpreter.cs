using Lokad.Starlark.Parsing;
using Lokad.Starlark.Runtime;

namespace Lokad.Starlark;

public sealed class StarlarkInterpreter
{
    private readonly ExpressionEvaluator _evaluator = new ExpressionEvaluator();

    public StarlarkValue EvaluateExpression(string source, StarlarkEnvironment environment)
    {
        var expression = StarlarkParser.ParseExpression(source);
        return _evaluator.Evaluate(expression, environment);
    }
}
