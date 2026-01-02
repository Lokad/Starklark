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

    [Fact]
    public void ExecutesForStatement()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("total = 0\nfor x in [1, 2, 3]:\n  total = total + x\ntotal\n", environment);

        Assert.Equal(new StarlarkInt(6), environment.Globals["total"]);
        Assert.Equal(new StarlarkInt(6), result);
    }

    [Fact]
    public void ExecutesFunctionDefinition()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("def add(a, b):\n  return a + b\nadd(2, 3)\n", environment);

        Assert.Equal(new StarlarkInt(5), result);
    }
}
