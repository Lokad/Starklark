using System.Collections.Generic;
using System.Globalization;

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
        environment.AddFunction("dict", Dict);
        environment.AddFunction("str", Str);
        environment.AddFunction("int", Int);
        environment.AddFunction("float", Float);
        environment.AddFunction("type", Type);
    }

    private static StarlarkValue Len(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
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

    private static StarlarkValue Range(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
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

    private static StarlarkValue List(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count == 0)
        {
            return new StarlarkList(Array.Empty<StarlarkValue>());
        }

        ExpectArgCount(args, 1);
        var items = Enumerate(args[0]);
        return new StarlarkList(items);
    }

    private static StarlarkValue Tuple(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count == 0)
        {
            return new StarlarkTuple(Array.Empty<StarlarkValue>());
        }

        ExpectArgCount(args, 1);
        var items = new List<StarlarkValue>(Enumerate(args[0]));
        return new StarlarkTuple(items);
    }

    private static StarlarkValue Bool(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1);
        return new StarlarkBool(args[0].IsTruthy);
    }

    private static StarlarkValue Any(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
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

    private static StarlarkValue All(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
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

    private static StarlarkValue Dict(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        if (args.Count == 0 && kwargs.Count == 0)
        {
            return new StarlarkDict(Array.Empty<KeyValuePair<StarlarkValue, StarlarkValue>>());
        }

        if (args.Count > 1)
        {
            throw new InvalidOperationException("dict expects 0 or 1 positional arguments.");
        }

        var entries = new List<KeyValuePair<StarlarkValue, StarlarkValue>>();
        if (args.Count == 1 && args[0] is StarlarkDict dict)
        {
            entries.AddRange(dict.Entries);
        }
        else if (args.Count == 1)
        {
            foreach (var item in Enumerate(args[0]))
            {
                if (!TryGetPair(item, out var key, out var value))
                {
                    throw new InvalidOperationException("dict update sequence element has length 1; 2 is required.");
                }

                StarlarkHash.EnsureHashable(key);
                AddOrReplace(entries, key, value);
            }
        }

        foreach (var pair in kwargs)
        {
            var key = new StarlarkString(pair.Key);
            StarlarkHash.EnsureHashable(key);
            AddOrReplace(entries, key, pair.Value);
        }

        return new StarlarkDict(entries);
    }

    private static StarlarkValue Str(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1);
        return new StarlarkString(StarlarkFormatting.ToString(args[0]));
    }

    private static StarlarkValue Int(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 1 or > 2)
        {
            throw new InvalidOperationException("int expects 1 or 2 arguments.");
        }

        var value = args[0];
        if (value is StarlarkInt intValue)
        {
            return intValue;
        }

        if (value is StarlarkBool boolValue)
        {
            return new StarlarkInt(boolValue.Value ? 1 : 0);
        }

        if (value is StarlarkFloat floatValue)
        {
            return new StarlarkInt((long)floatValue.Value);
        }

        if (value is StarlarkString text)
        {
            var baseValue = 10;
            if (args.Count == 2)
            {
                baseValue = checked((int)RequireInt(args[1]));
                if (baseValue is < 2 or > 36)
                {
                    throw new InvalidOperationException("int base must be between 2 and 36.");
                }
            }

            if (!TryParseInt(text.Value, baseValue, out var parsed))
            {
                throw new InvalidOperationException($"Invalid int literal: '{text.Value}'.");
            }

            return new StarlarkInt(parsed);
        }

        throw new InvalidOperationException($"Expected int-compatible value, got '{value.TypeName}'.");
    }

    private static StarlarkValue Float(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1);
        var value = args[0];
        return value switch
        {
            StarlarkFloat floatValue => floatValue,
            StarlarkInt intValue => new StarlarkFloat(intValue.Value),
            StarlarkBool boolValue => new StarlarkFloat(boolValue.Value ? 1.0 : 0.0),
            StarlarkString text when double.TryParse(
                text.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed) => new StarlarkFloat(parsed),
            _ => throw new InvalidOperationException($"Expected float-compatible value, got '{value.TypeName}'.")
        };
    }

    private static StarlarkValue Type(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1);
        return new StarlarkString(args[0].TypeName);
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

    private static void ExpectNoKeywords(IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        if (kwargs.Count > 0)
        {
            throw new InvalidOperationException("Unexpected keyword arguments.");
        }
    }

    private static bool TryGetPair(StarlarkValue item, out StarlarkValue key, out StarlarkValue value)
    {
        if (item is StarlarkTuple tuple && tuple.Items.Count == 2)
        {
            key = tuple.Items[0];
            value = tuple.Items[1];
            return true;
        }

        if (item is StarlarkList list && list.Items.Count == 2)
        {
            key = list.Items[0];
            value = list.Items[1];
            return true;
        }

        key = StarlarkNone.Instance;
        value = StarlarkNone.Instance;
        return false;
    }

    private static void AddOrReplace(
        List<KeyValuePair<StarlarkValue, StarlarkValue>> entries,
        StarlarkValue key,
        StarlarkValue value)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (Equals(entries[i].Key, key))
            {
                entries[i] = new KeyValuePair<StarlarkValue, StarlarkValue>(key, value);
                return;
            }
        }

        entries.Add(new KeyValuePair<StarlarkValue, StarlarkValue>(key, value));
    }

    private static bool TryParseInt(string text, int baseValue, out long value)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            value = 0;
            return false;
        }

        var sign = 1;
        var startIndex = 0;
        if (trimmed[0] == '+' || trimmed[0] == '-')
        {
            sign = trimmed[0] == '-' ? -1 : 1;
            startIndex = 1;
        }

        try
        {
            var parsed = Convert.ToInt64(trimmed[startIndex..], baseValue);
            value = parsed * sign;
            return true;
        }
        catch (FormatException)
        {
            value = 0;
            return false;
        }
        catch (OverflowException)
        {
            value = 0;
            return false;
        }
    }
}
