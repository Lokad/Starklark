using Lokad.Starlark;
using Lokad.Starlark.Runtime;
using Xunit;

namespace Lokad.Starlark.Tests.Runtime;

public sealed class InterpreterTests
{
    [Fact]
    public void EvaluatesExpressionThroughInterpreter()
    {
        var interpreter = new StarlarkInterpreter();
        var result = interpreter.EvaluateExpression("1 + 2 * 3", new StarlarkEnvironment());

        Assert.Equal(new StarlarkInt(7), result);
    }
}
