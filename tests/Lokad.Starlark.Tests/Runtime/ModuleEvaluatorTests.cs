using System;
using System.Collections.Generic;
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

    [Fact]
    public void ExecutesLoadStatement()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();
        environment.AddModule(
            "math",
            new Dictionary<string, StarlarkValue>
            {
                ["pi"] = new StarlarkFloat(3.14),
                ["tau"] = new StarlarkFloat(6.28)
            });

        var result = interpreter.ExecuteModule("load(\"math\", \"pi\", circle=\"tau\")\npi + circle\n", environment);

        Assert.Equal(new StarlarkFloat(9.42), result);
    }

    [Fact]
    public void ExecutesTupleAssignment()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("a, b = 1, 2\na + b\n", environment);

        Assert.Equal(new StarlarkInt(3), result);
    }

    [Fact]
    public void ExecutesIndexAssignment()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("items = [1, 2]\nitems[0] = 3\nitems[0]\n", environment);

        Assert.Equal(new StarlarkInt(3), result);
    }

    [Fact]
    public void ExecutesAugmentedAssignment()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("x = 1\nx += 2\nx\n", environment);

        Assert.Equal(new StarlarkInt(3), result);
    }

    [Fact]
    public void ExecutesListAugmentedAssignment()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("items = [1]\nitems += [2, 3]\nitems[2]\n", environment);

        Assert.Equal(new StarlarkInt(3), result);
    }

    [Fact]
    public void ExecutesLenAndRangeBuiltins()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("len(list(range(4)))\n", environment);

        Assert.Equal(new StarlarkInt(4), result);
    }

    [Fact]
    public void ExecutesCoreConversionBuiltins()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "text = str([1, 2])\n" +
            "kind = type(1)\n" +
            "value = int(\"12\") + int(True) + int(1.9)\n" +
            "float_value = float(\"3.5\") + float(2)\n" +
            "(text, kind, value, float_value)\n",
            environment);

        Assert.Equal(
            new StarlarkTuple(
                new StarlarkValue[]
                {
                    new StarlarkString("[1, 2]"),
                    new StarlarkString("int"),
                    new StarlarkInt(14),
                    new StarlarkFloat(5.5)
                }),
            result);
    }

    [Fact]
    public void ExecutesDictBuiltin()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "items = dict([(\"a\", 1), (\"a\", 2), (\"b\", 3)])\n" +
            "items[\"a\"]\n",
            environment);

        Assert.Equal(new StarlarkInt(2), result);
    }

    [Fact]
    public void ExecutesStringPercentFormatting()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("\"%s %r\" % (\"hi\", \"hi\")\n", environment);

        Assert.Equal(new StarlarkString("hi \"hi\""), result);
    }

    [Fact]
    public void ExecutesLiteralPercentFormatting()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule("\"%%d %d\" % 1\n", environment);

        Assert.Equal(new StarlarkString("%d 1"), result);
    }

    [Fact]
    public void RaisesOnBadPercentFormattingArguments()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        Assert.Throws<InvalidOperationException>(
            () => interpreter.ExecuteModule("\"%d %d\" % 1\n", environment));
        Assert.Throws<InvalidOperationException>(
            () => interpreter.ExecuteModule("\"%d %d\" % (1, 2, 3)\n", environment));
    }
}
