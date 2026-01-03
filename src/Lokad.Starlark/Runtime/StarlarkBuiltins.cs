using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lokad.Starlark.Runtime;

public static class StarlarkBuiltins
{
    public static void Register(StarlarkEnvironment environment)
    {
        environment.AddFunction("len", Len, isBuiltin: true);
        environment.AddFunction("range", Range, isBuiltin: true);
        environment.AddFunction("list", List, isBuiltin: true);
        environment.AddFunction("tuple", Tuple, isBuiltin: true);
        environment.AddFunction("bool", Bool, isBuiltin: true);
        environment.AddFunction("any", Any, isBuiltin: true);
        environment.AddFunction("all", All, isBuiltin: true);
        environment.AddFunction("dict", Dict, isBuiltin: true);
        environment.AddFunction("str", Str, isBuiltin: true);
        environment.AddFunction("int", Int, isBuiltin: true);
        environment.AddFunction("float", Float, isBuiltin: true);
        environment.AddFunction("bytes", Bytes, isBuiltin: true);
        environment.AddFunction("type", Type, isBuiltin: true);
        environment.AddFunction("repr", Repr, isBuiltin: true);
        environment.AddFunction("sorted", Sorted, isBuiltin: true);
        environment.AddFunction("reversed", Reversed, isBuiltin: true);
        environment.AddFunction("min", Min, isBuiltin: true);
        environment.AddFunction("max", Max, isBuiltin: true);
        environment.AddFunction("enumerate", Enumerate, isBuiltin: true);
        environment.AddFunction("zip", Zip, isBuiltin: true);
        environment.AddFunction("dir", Dir, isBuiltin: true);
        environment.AddFunction("getattr", GetAttr, isBuiltin: true);
        environment.AddFunction("hasattr", HasAttr, isBuiltin: true);
        environment.AddFunction("fail", Fail, isBuiltin: true);
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
            StarlarkBytes bytes => new StarlarkInt(bytes.Bytes.Length),
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
        if (args.Count is < 1 or > 2)
        {
            throw new InvalidOperationException("int expects 1 or 2 arguments.");
        }

        var value = args[0];
        var baseValue = 10;
        var baseProvided = false;
        if (args.Count == 2)
        {
            baseValue = checked((int)RequireInt(args[1]));
            baseProvided = true;
        }

        if (kwargs.Count > 0)
        {
            if (kwargs.Count > 1 || !kwargs.TryGetValue("base", out var baseArg))
            {
                throw new InvalidOperationException("int only supports the 'base' keyword.");
            }

            if (args.Count == 2)
            {
                throw new InvalidOperationException("int base specified twice.");
            }

            baseValue = checked((int)RequireInt(baseArg));
            baseProvided = true;
        }

        if (baseValue != 0 && baseValue is < 2 or > 36)
        {
            throw new InvalidOperationException("int base must be between 2 and 36.");
        }

        if (value is StarlarkInt intValue)
        {
            if (baseProvided)
            {
                throw new InvalidOperationException("int base is only valid for string arguments.");
            }

            return intValue;
        }

        if (value is StarlarkBool boolValue)
        {
            if (baseProvided)
            {
                throw new InvalidOperationException("int base is only valid for string arguments.");
            }

            return new StarlarkInt(boolValue.Value ? 1 : 0);
        }

        if (value is StarlarkFloat floatValue)
        {
            if (baseProvided)
            {
                throw new InvalidOperationException("int base is only valid for string arguments.");
            }

            return new StarlarkInt((long)floatValue.Value);
        }

        if (value is StarlarkString text)
        {
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

    private static StarlarkValue Bytes(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1);
        var value = args[0];
        switch (value)
        {
            case StarlarkBytes bytes:
                return bytes;
            case StarlarkString text:
                return new StarlarkBytes(System.Text.Encoding.UTF8.GetBytes(text.Value));
            default:
                IEnumerable<StarlarkValue> items;
                try
                {
                    items = Enumerate(value);
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException(
                        "bytes expects a string, bytes, or iterable of int.");
                }

                var result = new List<byte>();
                foreach (var item in items)
                {
                    if (item is not StarlarkInt intValue)
                    {
                        throw new InvalidOperationException("bytes expects integers in the range 0-255.");
                    }

                    if (intValue.Value is < 0 or > 255)
                    {
                        throw new InvalidOperationException("bytes expects integers in the range 0-255.");
                    }

                    result.Add((byte)intValue.Value);
                }

                return new StarlarkBytes(result.ToArray());
        }
    }

    private static StarlarkValue Type(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1);
        return new StarlarkString(args[0].TypeName);
    }

    private static StarlarkValue Repr(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1);
        return new StarlarkString(StarlarkFormatting.ToRepr(args[0]));
    }

    private static StarlarkValue Sorted(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        if (args.Count != 1)
        {
            throw new InvalidOperationException("sorted expects 1 argument.");
        }

        var keyFunction = ExtractKey(kwargs, out var reverse);
        var items = EnumerateIterable(args[0]).ToList();
        if (keyFunction != null)
        {
            var keyed = items
                .Select((item, index) => new SortKey(index, item, keyFunction.Call(new[] { item }, EmptyKeywords)))
                .ToList();
            keyed.Sort((left, right) =>
            {
                var compare = CompareValues(left.Key, right.Key);
                if (compare == 0)
                {
                    return left.Index.CompareTo(right.Index);
                }

                return compare;
            });

            if (reverse)
            {
                keyed.Reverse();
            }

            return new StarlarkList(keyed.Select(entry => entry.Value).ToList());
        }

        items.Sort(CompareValues);
        if (reverse)
        {
            items.Reverse();
        }

        return new StarlarkList(items);
    }

    private static StarlarkValue Reversed(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1);
        return args[0] switch
        {
            StarlarkList list => new StarlarkList(list.Items.AsEnumerable().Reverse().ToList()),
            StarlarkTuple tuple => new StarlarkList(tuple.Items.Reverse().ToList()),
            _ => throw new InvalidOperationException("reversed expects a list or tuple.")
        };
    }

    private static StarlarkValue Min(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return MinMax(args, kwargs, findMax: false, name: "min");
    }

    private static StarlarkValue Max(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return MinMax(args, kwargs, findMax: true, name: "max");
    }

    private static StarlarkValue Enumerate(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        if (args.Count is < 1 or > 2)
        {
            throw new InvalidOperationException("enumerate expects 1 or 2 arguments.");
        }

        if (kwargs.Count > 0 && !kwargs.ContainsKey("start"))
        {
            throw new InvalidOperationException("enumerate only supports the 'start' keyword.");
        }

        var start = args.Count == 2 ? RequireInt(args[1]) : 0;
        if (kwargs.TryGetValue("start", out var startValue))
        {
            if (args.Count == 2)
            {
                throw new InvalidOperationException("enumerate start specified twice.");
            }

            start = RequireInt(startValue);
        }

        var result = new List<StarlarkValue>();
        var index = start;
        foreach (var item in EnumerateIterable(args[0]))
        {
            result.Add(new StarlarkTuple(new StarlarkValue[] { new StarlarkInt(index), item }));
            index++;
        }

        return new StarlarkList(result);
    }

    private static StarlarkValue Zip(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count == 0)
        {
            return new StarlarkList(Array.Empty<StarlarkValue>());
        }

        var lists = args.Select(arg => EnumerateIterable(arg).ToList()).ToList();
        var length = lists.Min(list => list.Count);
        var result = new List<StarlarkValue>(length);

        for (var i = 0; i < length; i++)
        {
            var row = new StarlarkValue[lists.Count];
            for (var j = 0; j < lists.Count; j++)
            {
                row[j] = lists[j][i];
            }

            result.Add(new StarlarkTuple(row));
        }

        return new StarlarkList(result);
    }

    private static StarlarkValue Dir(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1);

        var names = GetDirMembers(args[0]);
        names.Sort(StringComparer.Ordinal);
        return new StarlarkList(names.Select(name => new StarlarkString(name)).ToList());
    }

    private static StarlarkValue GetAttr(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 2 or > 3)
        {
            throw new InvalidOperationException("getattr expects 2 or 3 arguments.");
        }

        if (args[1] is not StarlarkString name)
        {
            throw new InvalidOperationException("getattr expects a string attribute name.");
        }

        try
        {
            return StarlarkMethods.Bind(args[0], name.Value);
        }
        catch (InvalidOperationException)
        {
            if (args.Count == 3)
            {
                return args[2];
            }

            throw;
        }
    }

    private static StarlarkValue HasAttr(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 2);
        if (args[1] is not StarlarkString name)
        {
            throw new InvalidOperationException("hasattr expects a string attribute name.");
        }

        try
        {
            StarlarkMethods.Bind(args[0], name.Value);
            return new StarlarkBool(true);
        }
        catch (InvalidOperationException)
        {
            return new StarlarkBool(false);
        }
    }

    private static StarlarkValue Fail(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        var separator = " ";
        if (kwargs.Count > 0)
        {
            if (kwargs.Count > 1 || !kwargs.TryGetValue("sep", out var sepValue))
            {
                throw new InvalidOperationException("fail only supports the 'sep' keyword.");
            }

            separator = RequireString(sepValue);
        }

        if (args.Count == 0)
        {
            throw new InvalidOperationException("fail");
        }

        var parts = args.Select(arg => StarlarkFormatting.ToString(arg));
        throw new InvalidOperationException(string.Join(separator, parts));
    }

    private static readonly IReadOnlyDictionary<string, StarlarkValue> EmptyKeywords =
        new Dictionary<string, StarlarkValue>();

    private static StarlarkCallable? ExtractKey(
        IReadOnlyDictionary<string, StarlarkValue> kwargs,
        out bool reverse)
    {
        reverse = false;
        StarlarkCallable? key = null;

        foreach (var pair in kwargs)
        {
            if (pair.Key == "key")
            {
                if (pair.Value is not StarlarkCallable callable)
                {
                    throw new InvalidOperationException("key must be callable.");
                }

                key = callable;
            }
            else if (pair.Key == "reverse")
            {
                if (pair.Value is not StarlarkBool boolValue)
                {
                    throw new InvalidOperationException("reverse must be a bool.");
                }

                reverse = boolValue.Value;
            }
            else
            {
                throw new InvalidOperationException("Unexpected keyword arguments.");
            }
        }

        return key;
    }

    private static StarlarkValue MinMax(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs,
        bool findMax,
        string name)
    {
        if (args.Count == 0)
        {
            throw new InvalidOperationException($"{name} expects at least one argument.");
        }

        var keyFunction = ExtractKeyOnly(kwargs, name);
        var values = args.Count == 1
            ? EnumerateIterable(args[0]).ToList()
            : args.ToList();

        if (values.Count == 0)
        {
            throw new InvalidOperationException($"{name} expects at least one item.");
        }

        var best = values[0];
        var bestKey = keyFunction != null ? keyFunction.Call(new[] { best }, EmptyKeywords) : best;
        for (var i = 1; i < values.Count; i++)
        {
            var candidate = values[i];
            var candidateKey = keyFunction != null
                ? keyFunction.Call(new[] { candidate }, EmptyKeywords)
                : candidate;
            var compare = CompareValues(candidateKey, bestKey);
            if (findMax ? compare > 0 : compare < 0)
            {
                best = candidate;
                bestKey = candidateKey;
            }
        }

        return best;
    }

    private static StarlarkCallable? ExtractKeyOnly(
        IReadOnlyDictionary<string, StarlarkValue> kwargs,
        string name)
    {
        if (kwargs.Count == 0)
        {
            return null;
        }

        if (kwargs.Count > 1 || !kwargs.TryGetValue("key", out var keyValue))
        {
            throw new InvalidOperationException($"{name} only supports the 'key' keyword.");
        }

        if (keyValue is not StarlarkCallable callable)
        {
            throw new InvalidOperationException("key must be callable.");
        }

        return callable;
    }

    private static int CompareValues(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return leftInt.Value.CompareTo(rightInt.Value);
        }

        if (left is StarlarkFloat leftFloat && right is StarlarkFloat rightFloat)
        {
            if (double.IsNaN(leftFloat.Value) || double.IsNaN(rightFloat.Value))
            {
                throw new InvalidOperationException("Comparison with NaN is not supported.");
            }

            return leftFloat.Value.CompareTo(rightFloat.Value);
        }

        if (left is StarlarkInt leftNumber && right is StarlarkFloat rightNumber)
        {
            return StarlarkNumber.CompareIntFloat(leftNumber.Value, rightNumber.Value);
        }

        if (left is StarlarkFloat leftFloatNumber && right is StarlarkInt rightIntNumber)
        {
            return StarlarkNumber.CompareFloatInt(leftFloatNumber.Value, rightIntNumber.Value);
        }

        if (left is StarlarkString leftString && right is StarlarkString rightString)
        {
            return string.Compare(leftString.Value, rightString.Value, StringComparison.Ordinal);
        }

        if (left is StarlarkBytes leftBytes && right is StarlarkBytes rightBytes)
        {
            return CompareBytes(leftBytes.Bytes, rightBytes.Bytes);
        }

        if (left is StarlarkBool leftBool && right is StarlarkBool rightBool)
        {
            return leftBool.Value.CompareTo(rightBool.Value);
        }

        throw new InvalidOperationException(
            $"Comparison not supported between '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static bool TryGetNumber(StarlarkValue value, out double number, out bool isInt)
    {
        switch (value)
        {
            case StarlarkInt intValue:
                number = intValue.Value;
                isInt = true;
                return true;
            case StarlarkFloat floatValue:
                number = floatValue.Value;
                isInt = false;
                return true;
            default:
                number = 0;
                isInt = false;
                return false;
        }
    }

    private static List<string> GetDirMembers(StarlarkValue value)
    {
        return value switch
        {
            StarlarkString => new List<string>
            {
                "capitalize",
                "count",
                "elems",
                "endswith",
                "find",
                "format",
                "islower",
                "isspace",
                "istitle",
                "isupper",
                "join",
                "lower",
                "partition",
                "replace",
                "rfind",
                "rpartition",
                "rsplit",
                "split",
                "splitlines",
                "startswith",
                "strip",
                "title",
                "upper",
                "lstrip",
                "rstrip"
            },
            StarlarkBytes => new List<string> { "elems" },
            StarlarkList => new List<string>
            {
                "append",
                "clear",
                "extend",
                "index",
                "insert",
                "pop",
                "remove"
            },
            StarlarkDict => new List<string>
            {
                "clear",
                "get",
                "items",
                "keys",
                "pop",
                "popitem",
                "setdefault",
                "update",
                "values"
            },
            _ => new List<string>()
        };
    }

    private static IEnumerable<StarlarkValue> EnumerateIterable(StarlarkValue value)
    {
        return value switch
        {
            StarlarkList list => list.Items,
            StarlarkTuple tuple => tuple.Items,
            StarlarkDict dict => EnumerateDictKeys(dict),
            StarlarkRange range => EnumerateRange(range),
            _ => throw new InvalidOperationException($"Object of type '{value.TypeName}' is not iterable.")
        };
    }

    private readonly record struct SortKey(int Index, StarlarkValue Value, StarlarkValue Key);

    private static long RequireInt(StarlarkValue value)
    {
        if (value is StarlarkInt intValue)
        {
            return intValue.Value;
        }

        throw new InvalidOperationException($"Expected int, got '{value.TypeName}'.");
    }

    private static string RequireString(StarlarkValue value)
    {
        if (value is StarlarkString text)
        {
            return text.Value;
        }

        throw new InvalidOperationException($"Expected string, got '{value.TypeName}'.");
    }

    private static IEnumerable<StarlarkValue> Enumerate(StarlarkValue value)
    {
        switch (value)
        {
            case StarlarkList list:
                return list.Items;
            case StarlarkTuple tuple:
                return tuple.Items;
            case StarlarkDict dict:
                return EnumerateDictKeys(dict);
            case StarlarkRange range:
                return EnumerateRange(range);
            case StarlarkStringElems elems:
                return elems.Enumerate();
            case StarlarkBytesElems elems:
                return elems.Enumerate();
            default:
                throw new InvalidOperationException($"Object of type '{value.TypeName}' is not iterable.");
        }
    }

    private static int CompareBytes(byte[] left, byte[] right)
    {
        var length = left.Length < right.Length ? left.Length : right.Length;
        for (var i = 0; i < length; i++)
        {
            if (left[i] != right[i])
            {
                return left[i].CompareTo(right[i]);
            }
        }

        return left.Length.CompareTo(right.Length);
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
            var slice = trimmed[startIndex..];
            var resolvedBase = baseValue;
            if (resolvedBase == 0)
            {
                resolvedBase = InferBase(slice);
                if (resolvedBase == 0)
                {
                    value = 0;
                    return false;
                }
            }

            if (resolvedBase != 10 && slice.Length >= 2 && slice[0] == '0')
            {
                var prefix = char.ToLowerInvariant(slice[1]);
                if (prefix == 'x' || prefix == 'o' || prefix == 'b')
                {
                    slice = slice[2..];
                    if (slice.Length == 0)
                    {
                        value = 0;
                        return false;
                    }
                }
            }

            var parsed = Convert.ToInt64(slice, resolvedBase);
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

    private static int InferBase(string value)
    {
        if (value.All(ch => ch == '0'))
        {
            return 10;
        }

        if (value.Length < 2 || value[0] != '0')
        {
            return 10;
        }

        return char.ToLowerInvariant(value[1]) switch
        {
            'x' => 16,
            'o' => 8,
            'b' => 2,
            _ => 0
        };
    }
}
