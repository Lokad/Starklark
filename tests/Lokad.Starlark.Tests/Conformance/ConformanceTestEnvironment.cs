using System;
using System.Collections.Generic;
using Lokad.Starlark.Runtime;

namespace Lokad.Starlark.Tests.Conformance;

public static class ConformanceTestEnvironment
{
    public static StarlarkEnvironment Create()
    {
        var environment = new StarlarkEnvironment();
        environment.AddFunction("assert_", AssertTrue);
        environment.AddFunction("assert_eq", AssertEqual);
        environment.AddFunction("assert_ne", AssertNotEqual);
        environment.AddFunction("print", Print);
        return environment;
    }

    private static StarlarkValue AssertTrue(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        EnsureNoKeywords(kwargs);
        EnsureArgCount(args, 1, "assert_");
        if (!args[0].IsTruthy)
        {
            throw new InvalidOperationException("assert_ failed.");
        }

        return StarlarkNone.Instance;
    }

    private static StarlarkValue AssertEqual(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        EnsureNoKeywords(kwargs);
        EnsureArgCount(args, 2, "assert_eq");
        if (!Equals(args[0], args[1]))
        {
            throw new InvalidOperationException(
                $"assert_eq failed: {StarlarkFormatting.ToString(args[0])} != {StarlarkFormatting.ToString(args[1])}.");
        }

        return StarlarkNone.Instance;
    }

    private static StarlarkValue AssertNotEqual(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        EnsureNoKeywords(kwargs);
        EnsureArgCount(args, 2, "assert_ne");
        if (Equals(args[0], args[1]))
        {
            throw new InvalidOperationException(
                $"assert_ne failed: {StarlarkFormatting.ToString(args[0])} == {StarlarkFormatting.ToString(args[1])}.");
        }

        return StarlarkNone.Instance;
    }

    private static StarlarkValue Print(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return StarlarkNone.Instance;
    }

    private static void EnsureArgCount(IReadOnlyList<StarlarkValue> args, int count, string name)
    {
        if (args.Count != count)
        {
            throw new InvalidOperationException($"{name} expects {count} arguments.");
        }
    }

    private static void EnsureNoKeywords(IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        if (kwargs.Count > 0)
        {
            throw new InvalidOperationException("Unexpected keyword arguments.");
        }
    }
}
