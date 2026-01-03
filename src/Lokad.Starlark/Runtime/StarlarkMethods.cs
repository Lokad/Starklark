using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lokad.Starlark.Runtime;

public static class StarlarkMethods
{
    public static StarlarkValue Bind(StarlarkValue target, string name)
    {
        return target switch
        {
            StarlarkString text => BindString(text, name),
            StarlarkBytes bytes => BindBytes(bytes, name),
            StarlarkList list => BindList(list, name),
            StarlarkDict dict => BindDict(dict, name),
            _ => throw new InvalidOperationException(
                $"Object of type '{target.TypeName}' has no attribute '{name}'.")
        };
    }

    private static StarlarkValue BindString(StarlarkString target, string name)
    {
        return name switch
        {
            "split" => new StarlarkBoundMethod(name, target, StringSplit),
            "rsplit" => new StarlarkBoundMethod(name, target, StringRSplit),
            "splitlines" => new StarlarkBoundMethod(name, target, StringSplitLines),
            "strip" => new StarlarkBoundMethod(name, target, StringStrip),
            "lstrip" => new StarlarkBoundMethod(name, target, StringLStrip),
            "rstrip" => new StarlarkBoundMethod(name, target, StringRStrip),
            "count" => new StarlarkBoundMethod(name, target, StringCount),
            "startswith" => new StarlarkBoundMethod(name, target, StringStartsWith),
            "endswith" => new StarlarkBoundMethod(name, target, StringEndsWith),
            "replace" => new StarlarkBoundMethod(name, target, StringReplace),
            "find" => new StarlarkBoundMethod(name, target, StringFind),
            "rfind" => new StarlarkBoundMethod(name, target, StringRFind),
            "index" => new StarlarkBoundMethod(name, target, StringIndex),
            "rindex" => new StarlarkBoundMethod(name, target, StringRIndex),
            "partition" => new StarlarkBoundMethod(name, target, StringPartition),
            "rpartition" => new StarlarkBoundMethod(name, target, StringRPartition),
            "join" => new StarlarkBoundMethod(name, target, StringJoin),
            "lower" => new StarlarkBoundMethod(name, target, StringLower),
            "upper" => new StarlarkBoundMethod(name, target, StringUpper),
            "title" => new StarlarkBoundMethod(name, target, StringTitle),
            "capitalize" => new StarlarkBoundMethod(name, target, StringCapitalize),
            "isalnum" => new StarlarkBoundMethod(name, target, StringIsAlnum),
            "isalpha" => new StarlarkBoundMethod(name, target, StringIsAlpha),
            "isdigit" => new StarlarkBoundMethod(name, target, StringIsDigit),
            "islower" => new StarlarkBoundMethod(name, target, StringIsLower),
            "isupper" => new StarlarkBoundMethod(name, target, StringIsUpper),
            "istitle" => new StarlarkBoundMethod(name, target, StringIsTitle),
            "isspace" => new StarlarkBoundMethod(name, target, StringIsSpace),
            "removeprefix" => new StarlarkBoundMethod(name, target, StringRemovePrefix),
            "removesuffix" => new StarlarkBoundMethod(name, target, StringRemoveSuffix),
            "elems" => new StarlarkBoundMethod(name, target, StringElems),
            "format" => new StarlarkBoundMethod(name, target, StringFormat),
            _ => throw new InvalidOperationException(
                $"Object of type '{target.TypeName}' has no attribute '{name}'.")
        };
    }

    private static StarlarkValue BindBytes(StarlarkBytes target, string name)
    {
        return name switch
        {
            "elems" => new StarlarkBoundMethod(name, target, BytesElems),
            _ => throw new InvalidOperationException(
                $"Object of type '{target.TypeName}' has no attribute '{name}'.")
        };
    }

    private static StarlarkValue BindList(StarlarkList target, string name)
    {
        return name switch
        {
            "append" => new StarlarkBoundMethod(name, target, ListAppend),
            "clear" => new StarlarkBoundMethod(name, target, ListClear),
            "extend" => new StarlarkBoundMethod(name, target, ListExtend),
            "insert" => new StarlarkBoundMethod(name, target, ListInsert),
            "remove" => new StarlarkBoundMethod(name, target, ListRemove),
            "pop" => new StarlarkBoundMethod(name, target, ListPop),
            "index" => new StarlarkBoundMethod(name, target, ListIndex),
            _ => throw new InvalidOperationException(
                $"Object of type '{target.TypeName}' has no attribute '{name}'.")
        };
    }

    private static StarlarkValue BindDict(StarlarkDict target, string name)
    {
        return name switch
        {
            "get" => new StarlarkBoundMethod(name, target, DictGet),
            "keys" => new StarlarkBoundMethod(name, target, DictKeys),
            "values" => new StarlarkBoundMethod(name, target, DictValues),
            "items" => new StarlarkBoundMethod(name, target, DictItems),
            "pop" => new StarlarkBoundMethod(name, target, DictPop),
            "popitem" => new StarlarkBoundMethod(name, target, DictPopItem),
            "clear" => new StarlarkBoundMethod(name, target, DictClear),
            "setdefault" => new StarlarkBoundMethod(name, target, DictSetDefault),
            "update" => new StarlarkBoundMethod(name, target, DictUpdate),
            _ => throw new InvalidOperationException(
                $"Object of type '{target.TypeName}' has no attribute '{name}'.")
        };
    }

    private static StarlarkValue StringSplit(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        var text = ((StarlarkString)target).Value;
        if (args.Count > 2)
        {
            throw new InvalidOperationException("split expects 0 to 2 arguments.");
        }

        var (separator, maxsplit, hasSeparator) = ResolveSplitArguments(args);
        return new StarlarkList(
            hasSeparator
                ? SplitWithSeparator(text, separator!, (int)maxsplit, fromRight: false)
                : SplitOnWhitespace(text, (int)maxsplit, fromRight: false));
    }
    private static StarlarkValue StringRSplit(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        var text = ((StarlarkString)target).Value;
        if (args.Count > 2)
        {
            throw new InvalidOperationException("rsplit expects 0 to 2 arguments.");
        }

        var (separator, maxsplit, hasSeparator) = ResolveSplitArguments(args);
        return new StarlarkList(
            hasSeparator
                ? SplitWithSeparator(text, separator!, (int)maxsplit, fromRight: true)
                : SplitOnWhitespace(text, (int)maxsplit, fromRight: true));
    }
    private static StarlarkValue StringSplitLines(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count > 1)
        {
            throw new InvalidOperationException("splitlines expects 0 or 1 arguments.");
        }

        var keepEnds = args.Count == 1 && args[0].IsTruthy;
        var text = ((StarlarkString)target).Value;
        var result = new List<StarlarkValue>();

        var start = 0;
        var index = 0;
        while (index < text.Length)
        {
            var terminatorLength = 0;
            if (text[index] == '\n')
            {
                terminatorLength = 1;
            }
            else if (text[index] == '\r')
            {
                terminatorLength = index + 1 < text.Length && text[index + 1] == '\n' ? 2 : 1;
            }

            if (terminatorLength == 0)
            {
                index++;
                continue;
            }

            var lineEnd = index + (keepEnds ? terminatorLength : 0);
            result.Add(new StarlarkString(text.Substring(start, lineEnd - start)));
            index += terminatorLength;
            start = index;
        }

        if (start < text.Length)
        {
            result.Add(new StarlarkString(text.Substring(start)));
        }

        return new StarlarkList(result);
    }
    private static StarlarkValue StringStrip(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return StripCore(((StarlarkString)target).Value, args, kwargs, TrimMode.Both);
    }
    private static StarlarkValue StringLStrip(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return StripCore(((StarlarkString)target).Value, args, kwargs, TrimMode.Left);
    }
    private static StarlarkValue StringRStrip(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return StripCore(((StarlarkString)target).Value, args, kwargs, TrimMode.Right);
    }
    private static StarlarkValue StringCount(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 1 or > 3)
        {
            throw new InvalidOperationException("count expects 1 to 3 arguments.");
        }

        var text = ((StarlarkString)target).Value;
        var needle = RequireString(args[0]);
        var (start, end) = ResolveStartEnd(text.Length, args, 1);
        var count = 0;
        var index = start;
        while (index <= end - needle.Length)
        {
            var found = text.IndexOf(needle, index, end - index, StringComparison.Ordinal);
            if (found < 0)
            {
                break;
            }

            count++;
            index = found + Math.Max(needle.Length, 1);
        }

        return new StarlarkInt(count);
    }
    private static StarlarkValue StringStartsWith(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return StartsEndsWith(target, args, kwargs, fromStart: true);
    }
    private static StarlarkValue StringEndsWith(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return StartsEndsWith(target, args, kwargs, fromStart: false);
    }
    private static StarlarkValue StringReplace(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 2 or > 3)
        {
            throw new InvalidOperationException("replace expects 2 or 3 arguments.");
        }

        var text = ((StarlarkString)target).Value;
        var oldValue = RequireString(args[0]);
        var newValue = RequireString(args[1]);
        var count = args.Count == 3 ? RequireInt(args[2]) : -1;
        if (count < 0)
        {
            return new StarlarkString(text.Replace(oldValue, newValue, StringComparison.Ordinal));
        }

        var builder = new StringBuilder();
        var index = 0;
        var replaced = 0;
        while (replaced < count)
        {
            var found = text.IndexOf(oldValue, index, StringComparison.Ordinal);
            if (found < 0)
            {
                break;
            }

            builder.Append(text, index, found - index);
            builder.Append(newValue);
            index = found + oldValue.Length;
            replaced++;
        }

        builder.Append(text, index, text.Length - index);
        return new StarlarkString(builder.ToString());
    }
    private static StarlarkValue StringFind(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return StringFindCore(target, args, kwargs, fromRight: false);
    }
    private static StarlarkValue StringRFind(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return StringFindCore(target, args, kwargs, fromRight: true);
    }
    private static StarlarkValue StringIndex(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        var index = FindSubstringIndex(((StarlarkString)target).Value, args, kwargs, fromRight: false, "index");
        if (index < 0)
        {
            throw new InvalidOperationException("substring not found.");
        }

        return new StarlarkInt(index);
    }
    private static StarlarkValue StringRIndex(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        var index = FindSubstringIndex(((StarlarkString)target).Value, args, kwargs, fromRight: true, "rindex");
        if (index < 0)
        {
            throw new InvalidOperationException("substring not found.");
        }

        return new StarlarkInt(index);
    }
    private static StarlarkValue StringPartition(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return PartitionCore(((StarlarkString)target).Value, args, kwargs, fromRight: false);
    }
    private static StarlarkValue StringRPartition(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        return PartitionCore(((StarlarkString)target).Value, args, kwargs, fromRight: true);
    }
    private static StarlarkValue StringJoin(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1, "join");
        var separator = ((StarlarkString)target).Value;
        var items = EnumerateIterable(args[0]).ToList();
        var parts = new string[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is not StarlarkString text)
            {
                throw new InvalidOperationException("join expects a list of strings.");
            }

            parts[i] = text.Value;
        }

        return new StarlarkString(string.Join(separator, parts));
    }
    private static StarlarkValue StringLower(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "lower");
        var text = ((StarlarkString)target).Value;
        return new StarlarkString(text.ToLowerInvariant());
    }
    private static StarlarkValue StringUpper(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "upper");
        var text = ((StarlarkString)target).Value;
        return new StarlarkString(text.ToUpperInvariant());
    }
    private static StarlarkValue StringCapitalize(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "capitalize");
        var text = ((StarlarkString)target).Value;
        if (text.Length == 0)
        {
            return new StarlarkString(text);
        }

        var span = text.AsSpan();
        var status = System.Text.Rune.DecodeFromUtf16(span, out var rune, out var consumed);
        if (status != System.Buffers.OperationStatus.Done)
        {
            rune = System.Text.Rune.ReplacementChar;
            consumed = 1;
        }

        var first = text.Substring(0, consumed).ToUpperInvariant();
        var rest = text.Substring(consumed).ToLowerInvariant();
        return new StarlarkString(first + rest);
    }
    private static StarlarkValue StringIsAlnum(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "isalnum");
        var text = ((StarlarkString)target).Value;
        if (text.Length == 0)
        {
            return new StarlarkBool(false);
        }

        foreach (var rune in text.EnumerateRunes())
        {
            if (!System.Text.Rune.IsLetterOrDigit(rune))
            {
                return new StarlarkBool(false);
            }
        }

        return new StarlarkBool(true);
    }
    private static StarlarkValue StringIsAlpha(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "isalpha");
        var text = ((StarlarkString)target).Value;
        if (text.Length == 0)
        {
            return new StarlarkBool(false);
        }

        foreach (var rune in text.EnumerateRunes())
        {
            if (!System.Text.Rune.IsLetter(rune))
            {
                return new StarlarkBool(false);
            }
        }

        return new StarlarkBool(true);
    }
    private static StarlarkValue StringIsDigit(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "isdigit");
        var text = ((StarlarkString)target).Value;
        if (text.Length == 0)
        {
            return new StarlarkBool(false);
        }

        foreach (var rune in text.EnumerateRunes())
        {
            if (!System.Text.Rune.IsDigit(rune))
            {
                return new StarlarkBool(false);
            }
        }

        return new StarlarkBool(true);
    }
    private static StarlarkValue StringIsLower(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "islower");
        var text = ((StarlarkString)target).Value;
        var hasCased = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (IsCased(rune))
            {
                hasCased = true;
                if (!System.Text.Rune.IsLower(rune))
                {
                    return new StarlarkBool(false);
                }
            }
        }

        return new StarlarkBool(hasCased);
    }
    private static StarlarkValue StringIsUpper(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "isupper");
        var text = ((StarlarkString)target).Value;
        var hasCased = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (IsCased(rune))
            {
                hasCased = true;
                if (!System.Text.Rune.IsUpper(rune) && !IsTitleCase(rune))
                {
                    return new StarlarkBool(false);
                }
            }
        }

        return new StarlarkBool(hasCased);
    }
    private static StarlarkValue StringIsTitle(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "istitle");
        var text = ((StarlarkString)target).Value;
        var hasCased = false;
        var expectsTitle = true;

        foreach (var rune in text.EnumerateRunes())
        {
            if (IsCased(rune))
            {
                hasCased = true;
                if (expectsTitle)
                {
                    if (!IsTitleCase(rune))
                    {
                        return new StarlarkBool(false);
                    }

                    expectsTitle = false;
                }
                else
                {
                    if (!System.Text.Rune.IsLower(rune))
                    {
                        return new StarlarkBool(false);
                    }
                }
            }
            else
            {
                expectsTitle = true;
            }
        }

        return new StarlarkBool(hasCased);
    }
    private static StarlarkValue StringIsSpace(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "isspace");
        var text = ((StarlarkString)target).Value;
        if (text.Length == 0)
        {
            return new StarlarkBool(false);
        }

        foreach (var rune in text.EnumerateRunes())
        {
            if (!System.Text.Rune.IsWhiteSpace(rune))
            {
                return new StarlarkBool(false);
            }
        }

        return new StarlarkBool(true);
    }
    private static StarlarkValue StringRemovePrefix(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1, "removeprefix");
        var text = ((StarlarkString)target).Value;
        var prefix = RequireString(args[0]);
        if (text.StartsWith(prefix, StringComparison.Ordinal))
        {
            return new StarlarkString(text.Substring(prefix.Length));
        }

        return new StarlarkString(text);
    }
    private static StarlarkValue StringRemoveSuffix(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1, "removesuffix");
        var text = ((StarlarkString)target).Value;
        var suffix = RequireString(args[0]);
        if (text.EndsWith(suffix, StringComparison.Ordinal))
        {
            return new StarlarkString(text.Substring(0, text.Length - suffix.Length));
        }

        return new StarlarkString(text);
    }
    private static StarlarkValue StringElems(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "elems");
        var text = ((StarlarkString)target).Value;
        return new StarlarkStringElems(text);
    }
    private static StarlarkValue StringTitle(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "title");
        var text = ((StarlarkString)target).Value;
        var builder = new StringBuilder(text.Length);
        var startOfWord = true;
        foreach (var rune in text.EnumerateRunes())
        {
            if (System.Text.Rune.IsLetter(rune))
            {
                var segment = rune.ToString();
                builder.Append(startOfWord ? segment.ToUpperInvariant() : segment.ToLowerInvariant());
                startOfWord = false;
            }
            else
            {
                builder.Append(rune.ToString());
                startOfWord = true;
            }
        }

        return new StarlarkString(builder.ToString());
    }
    private static StarlarkValue StringFormat(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        var text = ((StarlarkString)target).Value;
        return new StarlarkString(FormatBraces(text, args, kwargs));
    }
    private static StarlarkValue BytesElems(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "elems");
        var bytes = ((StarlarkBytes)target).Bytes;
        return new StarlarkBytesElems(bytes);
    }

    private static StarlarkValue ListAppend(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1, "append");
        var list = (StarlarkList)target;
        list.Items.Add(args[0]);
        list.MarkMutated();
        return StarlarkNone.Instance;
    }
    private static StarlarkValue ListExtend(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1, "extend");
        var list = (StarlarkList)target;
        var mutated = false;
        foreach (var item in EnumerateIterable(args[0]))
        {
            list.Items.Add(item);
            mutated = true;
        }

        if (mutated)
        {
            list.MarkMutated();
        }

        return StarlarkNone.Instance;
    }

    private static StarlarkValue ListClear(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "clear");
        var list = (StarlarkList)target;
        list.Items.Clear();
        list.MarkMutated();
        return StarlarkNone.Instance;
    }
    private static StarlarkValue ListInsert(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 2, "insert");
        var list = (StarlarkList)target;
        var index = NormalizeInsertIndex(list.Items.Count, RequireInt(args[0]));
        list.Items.Insert(index, args[1]);
        list.MarkMutated();
        return StarlarkNone.Instance;
    }
    private static StarlarkValue ListRemove(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1, "remove");
        var list = (StarlarkList)target;
        for (var i = 0; i < list.Items.Count; i++)
        {
            if (Equals(list.Items[i], args[0]))
            {
                list.Items.RemoveAt(i);
                list.MarkMutated();
                return StarlarkNone.Instance;
            }
        }

        throw new InvalidOperationException("Value not found in list.");
    }
    private static StarlarkValue ListPop(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count > 1)
        {
            throw new InvalidOperationException("pop expects 0 or 1 arguments.");
        }

        var list = (StarlarkList)target;
        if (list.Items.Count == 0)
        {
            throw new InvalidOperationException("pop from empty list.");
        }

        var index = args.Count == 0
            ? list.Items.Count - 1
            : NormalizeIndex(list.Items.Count, RequireInt(args[0]));
        var value = list.Items[index];
        list.Items.RemoveAt(index);
        list.MarkMutated();
        return value;
    }
    private static StarlarkValue ListIndex(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 1 or > 3)
        {
            throw new InvalidOperationException("index expects 1 to 3 arguments.");
        }

        var list = (StarlarkList)target;
        var value = args[0];
        var (start, end) = ResolveStartEnd(list.Items.Count, args, 1);
        for (var i = start; i < end; i++)
        {
            if (Equals(list.Items[i], value))
            {
                return new StarlarkInt(i);
            }
        }

        throw new InvalidOperationException("Value not found in list.");
    }

    private static StarlarkValue DictGet(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 1 or > 2)
        {
            throw new InvalidOperationException("get expects 1 or 2 arguments.");
        }

        var dict = (StarlarkDict)target;
        var key = args[0];
        StarlarkHash.EnsureHashable(key);
        foreach (var entry in dict.Entries)
        {
            if (Equals(entry.Key, key))
            {
                return entry.Value;
            }
        }

        return args.Count == 2 ? args[1] : StarlarkNone.Instance;
    }
    private static StarlarkValue DictKeys(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "keys");
        var dict = (StarlarkDict)target;
        var result = new List<StarlarkValue>(dict.Entries.Count);
        foreach (var entry in dict.Entries)
        {
            result.Add(entry.Key);
        }

        return new StarlarkList(result);
    }
    private static StarlarkValue DictValues(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "values");
        var dict = (StarlarkDict)target;
        var result = new List<StarlarkValue>(dict.Entries.Count);
        foreach (var entry in dict.Entries)
        {
            result.Add(entry.Value);
        }

        return new StarlarkList(result);
    }
    private static StarlarkValue DictItems(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "items");
        var dict = (StarlarkDict)target;
        var result = new List<StarlarkValue>(dict.Entries.Count);
        foreach (var entry in dict.Entries)
        {
            result.Add(new StarlarkTuple(new[] { entry.Key, entry.Value }));
        }

        return new StarlarkList(result);
    }
    private static StarlarkValue DictPop(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 1 or > 2)
        {
            throw new InvalidOperationException("pop expects 1 or 2 arguments.");
        }

        var dict = (StarlarkDict)target;
        var key = args[0];
        StarlarkHash.EnsureHashable(key);
        for (var i = 0; i < dict.Entries.Count; i++)
        {
            if (Equals(dict.Entries[i].Key, key))
            {
                var value = dict.Entries[i].Value;
                dict.Entries.RemoveAt(i);
                dict.MarkMutated();
                return value;
            }
        }

        if (args.Count == 2)
        {
            return args[1];
        }

        throw new KeyNotFoundException("Key not found in dict.");
    }
    private static StarlarkValue DictPopItem(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "popitem");
        var dict = (StarlarkDict)target;
        if (dict.Entries.Count == 0)
        {
            throw new InvalidOperationException("popitem from empty dict.");
        }

        var entry = dict.Entries[0];
        dict.Entries.RemoveAt(0);
        dict.MarkMutated();
        return new StarlarkTuple(new[] { entry.Key, entry.Value });
    }
    private static StarlarkValue DictClear(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 0, "clear");
        var dict = (StarlarkDict)target;
        dict.Entries.Clear();
        dict.MarkMutated();
        return StarlarkNone.Instance;
    }
    private static StarlarkValue DictSetDefault(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 1 or > 2)
        {
            throw new InvalidOperationException("setdefault expects 1 or 2 arguments.");
        }

        var dict = (StarlarkDict)target;
        var key = args[0];
        StarlarkHash.EnsureHashable(key);
        for (var i = 0; i < dict.Entries.Count; i++)
        {
            if (Equals(dict.Entries[i].Key, key))
            {
                return dict.Entries[i].Value;
            }
        }

        var value = args.Count == 2 ? args[1] : StarlarkNone.Instance;
        dict.Entries.Add(new KeyValuePair<StarlarkValue, StarlarkValue>(key, value));
        dict.MarkMutated();
        return value;
    }
    private static StarlarkValue DictUpdate(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        if (args.Count > 1)
        {
            throw new InvalidOperationException("update expects at most 1 positional argument.");
        }

        var dict = (StarlarkDict)target;
        var mutated = false;
        if (args.Count == 1)
        {
            if (args[0] is StarlarkDict other)
            {
                foreach (var entry in other.Entries)
                {
                    mutated |= AddOrReplace(dict.Entries, entry.Key, entry.Value);
                }
            }
            else
            {
                foreach (var item in EnumerateIterable(args[0]))
                {
                    if (!TryGetPair(item, out var key, out var value))
                    {
                        throw new InvalidOperationException("dict update sequence element has length 1; 2 is required.");
                    }

                    StarlarkHash.EnsureHashable(key);
                    mutated |= AddOrReplace(dict.Entries, key, value);
                }
            }
        }

        foreach (var pair in kwargs)
        {
            var key = new StarlarkString(pair.Key);
            mutated |= AddOrReplace(dict.Entries, key, pair.Value);
        }

        if (mutated)
        {
            dict.MarkMutated();
        }
        return StarlarkNone.Instance;
    }

    private static StarlarkValue StartsEndsWith(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs,
        bool fromStart)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 1 or > 3)
        {
            throw new InvalidOperationException("startswith expects 1 to 3 arguments.");
        }

        var text = ((StarlarkString)target).Value;
        var patterns = ExpandStringOrTuple(args[0]);
        var (start, end) = ResolveStartEnd(text.Length, args, 1);
        var slice = text.Substring(start, end - start);
        var match = patterns.Any(pattern =>
            fromStart
                ? slice.StartsWith(pattern, StringComparison.Ordinal)
                : slice.EndsWith(pattern, StringComparison.Ordinal));
        return new StarlarkBool(match);
    }

    private static StarlarkValue StringFindCore(
        StarlarkValue target,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs,
        bool fromRight)
    {
        var text = ((StarlarkString)target).Value;
        var index = FindSubstringIndex(text, args, kwargs, fromRight, fromRight ? "rfind" : "find");
        return new StarlarkInt(index);
    }

    private static int FindSubstringIndex(
        string text,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs,
        bool fromRight,
        string methodName)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count is < 1 or > 3)
        {
            throw new InvalidOperationException($"{methodName} expects 1 to 3 arguments.");
        }

        var needle = RequireString(args[0]);
        var (start, end) = ResolveStartEnd(text.Length, args, 1);
        if (end <= start)
        {
            return -1;
        }

        return fromRight
            ? text.LastIndexOf(needle, end - 1, end - start, StringComparison.Ordinal)
            : text.IndexOf(needle, start, end - start, StringComparison.Ordinal);
    }

    private static StarlarkValue PartitionCore(
        string text,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs,
        bool fromRight)
    {
        ExpectNoKeywords(kwargs);
        ExpectArgCount(args, 1, "partition");
        var sep = RequireString(args[0]);
        if (sep.Length == 0)
        {
            throw new InvalidOperationException("partition separator cannot be empty.");
        }

        var index = fromRight
            ? text.LastIndexOf(sep, StringComparison.Ordinal)
            : text.IndexOf(sep, StringComparison.Ordinal);
        if (index < 0)
        {
            var prefix = fromRight ? string.Empty : text;
            var suffix = fromRight ? text : string.Empty;
            return new StarlarkTuple(new StarlarkValue[]
            {
                new StarlarkString(prefix),
                new StarlarkString(string.Empty),
                new StarlarkString(suffix)
            });
        }

        return new StarlarkTuple(new StarlarkValue[]
        {
            new StarlarkString(text.Substring(0, index)),
            new StarlarkString(sep),
            new StarlarkString(text.Substring(index + sep.Length))
        });
    }

    private static StarlarkValue StripCore(
        string text,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs,
        TrimMode mode)
    {
        ExpectNoKeywords(kwargs);
        if (args.Count > 1)
        {
            throw new InvalidOperationException("strip expects 0 or 1 arguments.");
        }

        if (args.Count == 0)
        {
            return new StarlarkString(mode switch
            {
                TrimMode.Left => text.TrimStart(),
                TrimMode.Right => text.TrimEnd(),
                _ => text.Trim()
            });
        }

        var chars = RequireString(args[0]);
        if (chars.Length == 0)
        {
            throw new InvalidOperationException("strip argument cannot be empty.");
        }

        var charSet = chars.ToCharArray();
        return new StarlarkString(mode switch
        {
            TrimMode.Left => text.TrimStart(charSet),
            TrimMode.Right => text.TrimEnd(charSet),
            _ => text.Trim(charSet)
        });
    }

    private static string FormatBraces(
        string format,
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        var builder = new StringBuilder();
        var nextIndex = 0;
        var usedAuto = false;
        var usedIndex = false;

        for (var i = 0; i < format.Length; i++)
        {
            var ch = format[i];
            if (ch == '{')
            {
                if (i + 1 < format.Length && format[i + 1] == '{')
                {
                    builder.Append('{');
                    i++;
                    continue;
                }

                var end = format.IndexOf('}', i + 1);
                if (end < 0)
                {
                    throw new InvalidOperationException("Unmatched '{' in format string.");
                }

                var field = format.Substring(i + 1, end - i - 1);
                if (field.Contains('!') || field.Contains(':'))
                {
                    throw new InvalidOperationException("Format specifiers are not supported.");
                }

                StarlarkValue value;
                if (field.Length == 0)
                {
                    usedAuto = true;
                    if (usedIndex)
                    {
                        throw new InvalidOperationException("Cannot mix automatic and manual field numbering.");
                    }

                    if (nextIndex >= args.Count)
                    {
                        throw new InvalidOperationException("Not enough arguments for format.");
                    }

                    value = args[nextIndex++];
                }
                else if (IsAllDigits(field))
                {
                    usedIndex = true;
                    if (usedAuto)
                    {
                        throw new InvalidOperationException("Cannot mix automatic and manual field numbering.");
                    }

                    var index = int.Parse(field, CultureInfo.InvariantCulture);
                    if (index < 0 || index >= args.Count)
                    {
                        throw new InvalidOperationException("Tuple index out of range.");
                    }

                    value = args[index];
                }
                else if (IsValidIdentifier(field))
                {
                    if (!kwargs.TryGetValue(field, out value!))
                    {
                        throw new InvalidOperationException($"Missing argument '{field}'.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Invalid format field.");
                }

                builder.Append(StarlarkFormatting.ToString(value));
                i = end;
            }
            else if (ch == '}')
            {
                if (i + 1 < format.Length && format[i + 1] == '}')
                {
                    builder.Append('}');
                    i++;
                }
                else
                {
                    throw new InvalidOperationException("Single '}' in format string.");
                }
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static List<StarlarkValue> SplitWithSeparator(
        string text,
        string separator,
        int maxsplit,
        bool fromRight)
    {
        if (separator.Length == 0)
        {
            throw new InvalidOperationException("Split separator cannot be empty.");
        }

        var parts = new List<StarlarkValue>();
        if (maxsplit == 0)
        {
            parts.Add(new StarlarkString(text));
            return parts;
        }

        if (!fromRight)
        {
            var remaining = text;
            var splits = 0;
            while (maxsplit < 0 || splits < maxsplit)
            {
                var index = remaining.IndexOf(separator, StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }

                parts.Add(new StarlarkString(remaining.Substring(0, index)));
                remaining = remaining.Substring(index + separator.Length);
                splits++;
            }

            parts.Add(new StarlarkString(remaining));
            return parts;
        }

        var rightParts = new List<StarlarkValue>();
        var remainingRight = text;
        var rightSplits = 0;
        while (maxsplit < 0 || rightSplits < maxsplit)
        {
            var index = remainingRight.LastIndexOf(separator, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            rightParts.Add(new StarlarkString(remainingRight.Substring(index + separator.Length)));
            remainingRight = remainingRight.Substring(0, index);
            rightSplits++;
        }

        rightParts.Add(new StarlarkString(remainingRight));
        rightParts.Reverse();
        return rightParts;
    }

    private static (string? Separator, long MaxSplit, bool HasSeparator) ResolveSplitArguments(
        IReadOnlyList<StarlarkValue> args)
    {
        if (args.Count == 0)
        {
            return (null, -1, false);
        }

        if (args[0] is StarlarkNone)
        {
            var maxsplit = args.Count == 2 ? RequireInt(args[1]) : -1;
            return (null, maxsplit, false);
        }

        var separator = RequireString(args[0]);
        var limit = args.Count == 2 ? RequireInt(args[1]) : -1;
        return (separator, limit, true);
    }

    private static List<StarlarkValue> SplitOnWhitespace(
        string text,
        int maxsplit,
        bool fromRight)
    {
        var parts = new List<StarlarkValue>();
        if (fromRight)
        {
            var end = text.Length;
            while (end > 0 && char.IsWhiteSpace(text[end - 1]))
            {
                end--;
            }

            if (end == 0)
            {
                return parts;
            }

            if (maxsplit == 0)
            {
                parts.Add(new StarlarkString(text.Substring(0, end)));
                return parts;
            }

            var splits = 0;
            var index = end - 1;
            while (index >= 0)
            {
                if (char.IsWhiteSpace(text[index]))
                {
                    parts.Add(new StarlarkString(text.Substring(index + 1, end - index - 1)));
                    splits++;
                    if (maxsplit > 0 && splits >= maxsplit)
                    {
                        while (index >= 0 && char.IsWhiteSpace(text[index]))
                        {
                            index--;
                        }

                        if (index >= 0)
                        {
                            parts.Add(new StarlarkString(text.Substring(0, index + 1)));
                        }

                        parts.Reverse();
                        return parts;
                    }

                    while (index >= 0 && char.IsWhiteSpace(text[index]))
                    {
                        index--;
                    }

                    end = index + 1;
                    continue;
                }

                index--;
            }

            if (end > 0)
            {
                parts.Add(new StarlarkString(text.Substring(0, end)));
            }

            parts.Reverse();
            return parts;
        }

        var start = 0;
        while (start < text.Length && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        if (start >= text.Length)
        {
            return parts;
        }

        if (maxsplit == 0)
        {
            parts.Add(new StarlarkString(text.Substring(start)));
            return parts;
        }

        var splitCount = 0;
        var indexForward = start;
        while (indexForward < text.Length)
        {
            if (char.IsWhiteSpace(text[indexForward]))
            {
                parts.Add(new StarlarkString(text.Substring(start, indexForward - start)));
                splitCount++;
                if (maxsplit > 0 && splitCount >= maxsplit)
                {
                    while (indexForward < text.Length && char.IsWhiteSpace(text[indexForward]))
                    {
                        indexForward++;
                    }

                    if (indexForward < text.Length)
                    {
                        parts.Add(new StarlarkString(text.Substring(indexForward)));
                    }

                    return parts;
                }

                while (indexForward < text.Length && char.IsWhiteSpace(text[indexForward]))
                {
                    indexForward++;
                }

                start = indexForward;
                continue;
            }

            indexForward++;
        }

        if (start < text.Length)
        {
            parts.Add(new StarlarkString(text.Substring(start)));
        }

        return parts;
    }

    private static List<string> ExpandStringOrTuple(StarlarkValue value)
    {
        if (value is StarlarkString text)
        {
            return new List<string> { text.Value };
        }

        if (value is StarlarkTuple tuple)
        {
            var result = new List<string>(tuple.Items.Count);
            foreach (var item in tuple.Items)
            {
                if (item is not StarlarkString tupleString)
                {
                    throw new InvalidOperationException("Expected string in tuple.");
                }

                result.Add(tupleString.Value);
            }

            return result;
        }

        throw new InvalidOperationException("Expected string or tuple of strings.");
    }

    private static IEnumerable<StarlarkValue> EnumerateIterable(StarlarkValue value)
    {
        switch (value)
        {
            case StarlarkList list:
                return list.Items;
            case StarlarkTuple tuple:
                return tuple.Items;
            case StarlarkDict dict:
                return dict.Entries.Select(entry => entry.Key);
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

    private static bool IsCased(System.Text.Rune rune)
    {
        if (System.Text.Rune.IsUpper(rune) || System.Text.Rune.IsLower(rune))
        {
            return true;
        }

        return System.Globalization.UnicodeCategory.TitlecaseLetter ==
               System.Text.Rune.GetUnicodeCategory(rune);
    }

    private static bool IsTitleCase(System.Text.Rune rune)
    {
        return System.Text.Rune.IsUpper(rune) ||
               System.Globalization.UnicodeCategory.TitlecaseLetter ==
               System.Text.Rune.GetUnicodeCategory(rune);
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

    private static bool AddOrReplace(
        List<KeyValuePair<StarlarkValue, StarlarkValue>> entries,
        StarlarkValue key,
        StarlarkValue value)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (Equals(entries[i].Key, key))
            {
                entries[i] = new KeyValuePair<StarlarkValue, StarlarkValue>(key, value);
                return true;
            }
        }

        entries.Add(new KeyValuePair<StarlarkValue, StarlarkValue>(key, value));
        return true;
    }

    private static (int Start, int End) ResolveStartEnd(
        int length,
        IReadOnlyList<StarlarkValue> args,
        int offset)
    {
        var startValue = GetOptionalIndex(args, offset, 0);
        var endValue = GetOptionalIndex(args, offset + 1, length);
        var start = NormalizeIndex(length, startValue, clamp: true);
        var end = NormalizeIndex(length, endValue, clamp: true);
        if (end < start)
        {
            end = start;
        }

        return (start, end);
    }

    private static long GetOptionalIndex(IReadOnlyList<StarlarkValue> args, int index, long fallback)
    {
        if (index >= args.Count)
        {
            return fallback;
        }

        var value = args[index];
        if (value is StarlarkNone)
        {
            return fallback;
        }

        if (value is StarlarkInt intValue)
        {
            return intValue.Value;
        }

        throw new InvalidOperationException($"Expected int or None, got '{value.TypeName}'.");
    }

    private static int NormalizeInsertIndex(int length, long index)
    {
        var value = (int)Math.Clamp(index, int.MinValue, int.MaxValue);
        if (value < 0)
        {
            value += length;
        }

        if (value < 0)
        {
            return 0;
        }

        if (value > length)
        {
            return length;
        }

        return value;
    }

    private static int NormalizeIndex(int length, long index, bool clamp = false)
    {
        var value = (int)Math.Clamp(index, int.MinValue, int.MaxValue);
        if (value < 0)
        {
            value += length;
        }

        if (clamp)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > length)
            {
                return length;
            }

            return value;
        }

        if (value < 0 || value >= length)
        {
            throw new InvalidOperationException("Index out of range.");
        }

        return value;
    }

    private static bool IsAllDigits(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
            {
                return false;
            }
        }

        return value.Length > 0;
    }

    private static bool IsValidIdentifier(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (!char.IsLetter(value[0]) && value[0] != '_')
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var ch = value[i];
            if (!char.IsLetterOrDigit(ch) && ch != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static void ExpectArgCount(IReadOnlyList<StarlarkValue> args, int count, string name)
    {
        if (args.Count != count)
        {
            throw new InvalidOperationException($"{name} expects {count} arguments.");
        }
    }

    private static void ExpectNoKeywords(IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        if (kwargs.Count > 0)
        {
            throw new InvalidOperationException("Unexpected keyword arguments.");
        }
    }

    private static string RequireString(StarlarkValue value)
    {
        if (value is StarlarkString text)
        {
            return text.Value;
        }

        throw new InvalidOperationException($"Expected string, got '{value.TypeName}'.");
    }

    private static long RequireInt(StarlarkValue value)
    {
        if (value is StarlarkInt intValue)
        {
            return intValue.Value;
        }

        throw new InvalidOperationException($"Expected int, got '{value.TypeName}'.");
    }

    private static bool RequireBool(StarlarkValue value)
    {
        if (value is StarlarkBool boolValue)
        {
            return boolValue.Value;
        }

        throw new InvalidOperationException($"Expected bool, got '{value.TypeName}'.");
    }

    private enum TrimMode
    {
        Both,
        Left,
        Right
    }
}
