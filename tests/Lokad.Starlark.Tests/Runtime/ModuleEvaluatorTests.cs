using Lokad.Starlark;
using Lokad.Starlark.Runtime;
using Xunit;

namespace Lokad.Starlark.Tests.Runtime;

public sealed class ModuleEvaluatorTests
{
    [Fact]
    public void ExecutesAssignmentsAndExpressions()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("x = 1\nx + 2\n", environment);

        Assert.Equal(new StarlarkInt(1), environment.Globals["x"]);
        Assert.Equal(new StarlarkInt(3), result);
    }

    [Fact]
    public void ExecutesIfStatement()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("x = 0\nif True:\n  x = 5\nx\n", environment);

        Assert.Equal(new StarlarkInt(5), environment.Globals["x"]);
        Assert.Equal(new StarlarkInt(5), result);
    }
}
