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
    public void ExecutesTupleForTarget()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "total = 0\n" +
            "for x, y in [(1, 2), (3, 4)]:\n" +
            "  total = total + x + y\n" +
            "total\n",
            environment);

        Assert.Equal(new StarlarkInt(10), result);
    }

    [Fact]
    public void ExecutesNestedTupleForTarget()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "def f():\n" +
            "  res = []\n" +
            "  for (x, y), z in [([\"a\", \"b\"], 3), ([\"c\", \"d\"], 4)]:\n" +
            "    res.append((x, y, z))\n" +
            "  return res\n" +
            "f()\n",
            environment);

        Assert.Equal(
            new StarlarkList(new StarlarkValue[]
            {
                new StarlarkTuple(new StarlarkValue[]
                {
                    new StarlarkString("a"),
                    new StarlarkString("b"),
                    new StarlarkInt(3)
                }),
                new StarlarkTuple(new StarlarkValue[]
                {
                    new StarlarkString("c"),
                    new StarlarkString("d"),
                    new StarlarkInt(4)
                })
            }),
            result);
    }

    [Fact]
    public void ExecutesIndexForTarget()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "def g():\n" +
            "  a = {}\n" +
            "  for i, a[i] in [(\"one\", 1), (\"two\", 2)]:\n" +
            "    pass\n" +
            "  return a\n" +
            "g()\n",
            environment);

        Assert.Equal(
            new StarlarkDict(new[]
            {
                new KeyValuePair<StarlarkValue, StarlarkValue>(
                    new StarlarkString("one"),
                    new StarlarkInt(1)),
                new KeyValuePair<StarlarkValue, StarlarkValue>(
                    new StarlarkString("two"),
                    new StarlarkInt(2))
            }),
            result);
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

    [Fact]
    public void ExecutesStringMethods()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "\"a.b.c\".split(\".\")\n",
            environment);

        Assert.Equal(
            new StarlarkList(new StarlarkValue[]
            {
                new StarlarkString("a"),
                new StarlarkString("b"),
                new StarlarkString("c")
            }),
            result);
    }

    [Fact]
    public void ExecutesListMethods()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "items = [1]\n" +
            "items.append(2)\n" +
            "items.extend([3, 4])\n" +
            "items.pop()\n",
            environment);

        Assert.Equal(new StarlarkInt(4), result);
    }

    [Fact]
    public void ExecutesDictMethods()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "values = {\"a\": 1}\n" +
            "values.update(b=2)\n" +
            "values.get(\"b\")\n",
            environment);

        Assert.Equal(new StarlarkInt(2), result);
    }

    [Fact]
    public void ExecutesFormatWithKeywords()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "\"a{x}b{y}c{}\".format(1, x=2, y=3)\n",
            environment);

        Assert.Equal(new StarlarkString("a2b3c1"), result);
    }

    [Fact]
    public void ExecutesFunctionDefaults()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "def greet(name, suffix=\"!\"):\n" +
            "  return name + suffix\n" +
            "greet(\"hi\")\n",
            environment);

        Assert.Equal(new StarlarkString("hi!"), result);
    }

    [Fact]
    public void ExecutesVariadicFunctions()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "def f(*args, **kwargs):\n" +
            "  return (args, kwargs)\n" +
            "f(1, 2, x=3)\n",
            environment);

        Assert.Equal(
            new StarlarkTuple(new StarlarkValue[]
            {
                new StarlarkTuple(new StarlarkValue[]
                {
                    new StarlarkInt(1),
                    new StarlarkInt(2)
                }),
                new StarlarkDict(new[]
                {
                    new KeyValuePair<StarlarkValue, StarlarkValue>(
                        new StarlarkString("x"),
                        new StarlarkInt(3))
                })
            }),
            result);
    }

    [Fact]
    public void ExecutesListAndDictComprehensions()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "values = [2 * x for x in [1, 2, 3] if x > 1]\n" +
            "mapping = {x: x * x for x in range(3)}\n" +
            "(values, mapping)\n",
            environment);

        Assert.Equal(
            new StarlarkTuple(new StarlarkValue[]
            {
                new StarlarkList(new StarlarkValue[]
                {
                    new StarlarkInt(4),
                    new StarlarkInt(6)
                }),
                new StarlarkDict(new[]
                {
                    new KeyValuePair<StarlarkValue, StarlarkValue>(
                        new StarlarkInt(0),
                        new StarlarkInt(0)),
                    new KeyValuePair<StarlarkValue, StarlarkValue>(
                        new StarlarkInt(1),
                        new StarlarkInt(1)),
                    new KeyValuePair<StarlarkValue, StarlarkValue>(
                        new StarlarkInt(2),
                        new StarlarkInt(4))
                })
            }),
            result);
    }

    [Fact]
    public void RejectsStringIterationInForLoops()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        Assert.Throws<InvalidOperationException>(
            () => interpreter.ExecuteModule("for x in \"abc\":\n  pass\n", environment));
    }

    [Fact]
    public void RejectsStringIterationInComprehensions()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        Assert.Throws<InvalidOperationException>(
            () => interpreter.ExecuteModule("[x for x in \"abc\"]\n", environment));
    }

    [Fact]
    public void ExecutesSortedAndMinMax()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "items = sorted([3, 1, 2])\n" +
            "min_value = min(items)\n" +
            "max_value = max(items)\n" +
            "(items, min_value, max_value)\n",
            environment);

        Assert.Equal(
            new StarlarkTuple(new StarlarkValue[]
            {
                new StarlarkList(new StarlarkValue[]
                {
                    new StarlarkInt(1),
                    new StarlarkInt(2),
                    new StarlarkInt(3)
                }),
                new StarlarkInt(1),
                new StarlarkInt(3)
            }),
            result);
    }

    [Fact]
    public void ExecutesEnumerateAndZip()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "pairs = enumerate([\"a\", \"b\"], 1)\n" +
            "zipped = zip([1, 2], [3, 4])\n" +
            "(pairs, zipped)\n",
            environment);

        Assert.Equal(
            new StarlarkTuple(new StarlarkValue[]
            {
                new StarlarkList(new StarlarkValue[]
                {
                    new StarlarkTuple(new StarlarkValue[] { new StarlarkInt(1), new StarlarkString("a") }),
                    new StarlarkTuple(new StarlarkValue[] { new StarlarkInt(2), new StarlarkString("b") })
                }),
                new StarlarkList(new StarlarkValue[]
                {
                    new StarlarkTuple(new StarlarkValue[] { new StarlarkInt(1), new StarlarkInt(3) }),
                    new StarlarkTuple(new StarlarkValue[] { new StarlarkInt(2), new StarlarkInt(4) })
                })
            }),
            result);
    }

    [Fact]
    public void ExecutesDirAndAttributeBuiltins()
    {
        var interpreter = new StarlarkInterpreter();
        var environment = new StarlarkEnvironment();

        var result = interpreter.ExecuteModule(
            "names = dir({})\n" +
            "has_find = hasattr(\"\", \"find\")\n" +
            "value = getattr(\"\", \"missing\", 42)\n" +
            "(names[:3], has_find, value)\n",
            environment);

        Assert.Equal(
            new StarlarkTuple(new StarlarkValue[]
            {
                new StarlarkList(new StarlarkValue[]
                {
                    new StarlarkString("clear"),
                    new StarlarkString("get"),
                    new StarlarkString("items")
                }),
                new StarlarkBool(true),
                new StarlarkInt(42)
            }),
            result);
    }
}
