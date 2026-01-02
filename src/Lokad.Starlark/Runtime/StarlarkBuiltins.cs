using System.Collections.Generic;

namespace Lokad.Starlark.Runtime;

public static class StarlarkBuiltins
{
    public static void Register(StarlarkEnvironment environment)
    {
        environment.AddFunction("len", Len);
        environment.AddFunction("range", Range);
        environment.AddFunction("list", List);
        environment.AddFunction("tuple", Tuple);
        environment.AddFunction("bool", Bool);
        environment.AddFunction("any", Any);
        environment.AddFunction("all", All);
    }

    private static StarlarkValue Len(IReadOnlyList<StarlarkValue> args)
    {
        ExpectArgCount(args, 1);

        return args[0] switch
        {
            StarlarkString text => new StarlarkInt(text.Value.Length),
            StarlarkList list => new StarlarkInt(list.Items.Count),
            StarlarkTuple tuple => new StarlarkInt(tuple.Items.Count),
            StarlarkDict dict => new StarlarkInt(dict.Entries.Count),
            StarlarkRange range => new StarlarkInt(range.Count),
            _ => throw new InvalidOperationException($"Object of type '{args[0].TypeName}' has no len.")
        };
    }

    private static StarlarkValue Range(IReadOnlyList<StarlarkValue> args)
    {
        if (args.Count is < 1 or > 3)
        {
            throw new InvalidOperationException("range expects 1 to 3 arguments.");
        }

        var start = 0L;
        var stop = 0L;
        var step = 1L;

        if (args.Count == 1)
        {
            stop = RequireInt(args[0]);
        }
        else if (args.Count == 2)
        {
            start = RequireInt(args[0]);
            stop = RequireInt(args[1]);
        }
        else
        {
            start = RequireInt(args[0]);
            stop = RequireInt(args[1]);
            step = RequireInt(args[2]);
        }

        if (step == 0)
        {
            throw new InvalidOperationException("range step cannot be zero.");
        }

        return new StarlarkRange(start, stop, step);
    }

    private static StarlarkValue List(IReadOnlyList<StarlarkValue> args)
    {
        if (args.Count == 0)
        {
            return new StarlarkList(Array.Empty<StarlarkValue>());
        }

        ExpectArgCount(args, 1);
        var items = Enumerate(args[0]);
        return new StarlarkList(items);
    }

    private static StarlarkValue Tuple(IReadOnlyList<StarlarkValue> args)
    {
        if (args.Count == 0)
        {
            return new StarlarkTuple(Array.Empty<StarlarkValue>());
        }

        ExpectArgCount(args, 1);
        var items = new List<StarlarkValue>(Enumerate(args[0]));
        return new StarlarkTuple(items);
    }

    private static StarlarkValue Bool(IReadOnlyList<StarlarkValue> args)
    {
        ExpectArgCount(args, 1);
        return new StarlarkBool(args[0].IsTruthy);
    }

    private static StarlarkValue Any(IReadOnlyList<StarlarkValue> args)
    {
        ExpectArgCount(args, 1);
        foreach (var item in Enumerate(args[0]))
        {
            if (item.IsTruthy)
            {
                return new StarlarkBool(true);
            }
        }

        return new StarlarkBool(false);
    }

    private static StarlarkValue All(IReadOnlyList<StarlarkValue> args)
    {
        ExpectArgCount(args, 1);
        foreach (var item in Enumerate(args[0]))
        {
            if (!item.IsTruthy)
            {
                return new StarlarkBool(false);
            }
        }

        return new StarlarkBool(true);
    }

    private static long RequireInt(StarlarkValue value)
    {
        if (value is StarlarkInt intValue)
        {
            return intValue.Value;
        }

        throw new InvalidOperationException($"Expected int, got '{value.TypeName}'.");
    }

    private static IEnumerable<StarlarkValue> Enumerate(StarlarkValue value)
    {
        switch (value)
        {
            case StarlarkList list:
                return list.Items;
            case StarlarkTuple tuple:
                return tuple.Items;
            case StarlarkString text:
                return EnumerateString(text.Value);
            case StarlarkDict dict:
                return EnumerateDictKeys(dict);
            case StarlarkRange range:
                return EnumerateRange(range);
            default:
                throw new InvalidOperationException($"Object of type '{value.TypeName}' is not iterable.");
        }
    }

    private static IEnumerable<StarlarkValue> EnumerateString(string text)
    {
        foreach (var ch in text)
        {
            yield return new StarlarkString(ch.ToString());
        }
    }

    private static IEnumerable<StarlarkValue> EnumerateDictKeys(StarlarkDict dict)
    {
        foreach (var entry in dict.Entries)
        {
            yield return entry.Key;
        }
    }

    private static IEnumerable<StarlarkValue> EnumerateRange(StarlarkRange range)
    {
        if (range.Step > 0)
        {
            for (var i = range.Start; i < range.Stop; i += range.Step)
            {
                yield return new StarlarkInt(i);
            }
        }
        else
        {
            for (var i = range.Start; i > range.Stop; i += range.Step)
            {
                yield return new StarlarkInt(i);
            }
        }
    }

    private static void ExpectArgCount(IReadOnlyList<StarlarkValue> args, int count)
    {
        if (args.Count != count)
        {
            throw new InvalidOperationException($"Expected {count} arguments, got {args.Count}.");
        }
    }
}
