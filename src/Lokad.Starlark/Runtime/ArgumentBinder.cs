using System.Collections.Generic;

namespace Lokad.Starlark.Runtime;

internal static class ArgumentBinder
{
    internal static void ExpectNoKeywords(IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        if (kwargs.Count > 0)
        {
            throw new InvalidOperationException("Unexpected keyword arguments.");
        }
    }

    internal static void ExpectExact(IReadOnlyList<StarlarkValue> args, int count)
    {
        if (args.Count != count)
        {
            throw new InvalidOperationException($"Expected {count} arguments, got {args.Count}.");
        }
    }

    internal static void ExpectExact(IReadOnlyList<StarlarkValue> args, int count, string name)
    {
        if (args.Count != count)
        {
            throw new InvalidOperationException($"{name} expects {count} arguments.");
        }
    }

    internal static void ExpectRange(IReadOnlyList<StarlarkValue> args, int min, int max, string name)
    {
        if (args.Count < min || args.Count > max)
        {
            throw new InvalidOperationException($"{name} expects {min} to {max} arguments.");
        }
    }
}
