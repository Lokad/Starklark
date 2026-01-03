using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lokad.Starlark.Runtime;

public static class StarlarkFormatting
{
    public static string ToString(StarlarkValue value) => Format(value, quoteStrings: false);

    public static string ToRepr(StarlarkValue value) => Format(value, quoteStrings: true);

    public static string FormatPercent(string format, StarlarkValue args)
    {
        var usesMapping = ContainsMappingSpec(format);
        var arguments = new FormatArguments(args, usesMapping);
        var builder = new StringBuilder();

        for (var i = 0; i < format.Length; i++)
        {
            var ch = format[i];
            if (ch != '%')
            {
                builder.Append(ch);
                continue;
            }

            if (i + 1 < format.Length && format[i + 1] == '%')
            {
                builder.Append('%');
                i++;
                continue;
            }

            i++;
            if (i >= format.Length)
            {
                throw new InvalidOperationException("Incomplete format specifier.");
            }

            string? key = null;
            if (format[i] == '(')
            {
                var start = i + 1;
                var end = format.IndexOf(')', start);
                if (end < 0)
                {
                    throw new InvalidOperationException("Incomplete format key.");
                }

                key = format[start..end];
                i = end + 1;
            }

            var flags = ParseFlags(format, ref i);
            var width = ParseWidth(format, ref i);
            var precision = ParsePrecision(format, ref i);

            if (i >= format.Length)
            {
                throw new InvalidOperationException("Incomplete format specifier.");
            }

            var specifier = format[i];
            var value = key == null ? arguments.Next() : arguments.Lookup(key);
            var formatted = FormatValue(value, specifier, precision, flags);
            builder.Append(ApplyWidth(formatted, width, flags));
        }

        arguments.EnsureAllConsumed();
        return builder.ToString();
    }

    private static string Format(StarlarkValue value, bool quoteStrings)
    {
        var state = new FormattingState();
        return Format(value, quoteStrings, state);
    }

    private static string Format(StarlarkValue value, bool quoteStrings, FormattingState state)
    {
        return value switch
        {
            StarlarkString text => quoteStrings ? Quote(text.Value) : text.Value,
            StarlarkBytes bytes => quoteStrings ? FormatBytesLiteral(bytes.Bytes) : DecodeBytes(bytes.Bytes),
            StarlarkBool boolean => boolean.Value ? "True" : "False",
            StarlarkNone => "None",
            StarlarkInt integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            StarlarkFloat number => number.Value.ToString("G", CultureInfo.InvariantCulture),
            StarlarkList list => FormatList(list, state),
            StarlarkTuple tuple => FormatTuple(tuple, state),
            StarlarkDict dict => FormatDict(dict, state),
            StarlarkSet set => FormatSet(set, state),
            StarlarkRange range => FormatRange(range),
            StarlarkStringElems elems => $"{Quote(elems.Value)}.elems()",
            StarlarkBytesElems elems => $"{FormatBytesLiteral(elems.Bytes)}.elems()",
            StarlarkFunction function => function.IsBuiltin
                ? $"<built-in function {function.Name}>"
                : $"<function {function.Name}>",
            StarlarkUserFunction function => $"<function {function.Name}>",
            StarlarkBoundMethod method => $"<bound method {method.Name}>",
            StarlarkCallable => "<function>",
            _ => value.TypeName
        };
    }

    private static string FormatList(StarlarkList list, FormattingState state)
    {
        state.Enter(list);
        var builder = new StringBuilder();
        builder.Append('[');
        for (var i = 0; i < list.Items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Format(list.Items[i], quoteStrings: true, state));
        }

        builder.Append(']');
        state.Exit(list);
        return builder.ToString();
    }

    private static string FormatTuple(StarlarkTuple tuple, FormattingState state)
    {
        state.Enter(tuple);
        var builder = new StringBuilder();
        builder.Append('(');
        for (var i = 0; i < tuple.Items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Format(tuple.Items[i], quoteStrings: true, state));
        }

        if (tuple.Items.Count == 1)
        {
            builder.Append(',');
        }

        builder.Append(')');
        state.Exit(tuple);
        return builder.ToString();
    }

    private static string FormatDict(StarlarkDict dict, FormattingState state)
    {
        state.Enter(dict);
        var builder = new StringBuilder();
        builder.Append('{');
        for (var i = 0; i < dict.Entries.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Format(dict.Entries[i].Key, quoteStrings: true, state));
            builder.Append(": ");
            builder.Append(Format(dict.Entries[i].Value, quoteStrings: true, state));
        }

        builder.Append('}');
        state.Exit(dict);
        return builder.ToString();
    }

    private static string FormatSet(StarlarkSet set, FormattingState state)
    {
        if (set.Items.Count == 0)
        {
            return "set()";
        }

        state.Enter(set);
        var builder = new StringBuilder();
        builder.Append("set([");
        for (var i = 0; i < set.Items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Format(set.Items[i], quoteStrings: true, state));
        }

        builder.Append("])");
        state.Exit(set);
        return builder.ToString();
    }

    private static string FormatRange(StarlarkRange range)
    {
        return range.Step == 1
            ? $"range({range.Start}, {range.Stop})"
            : $"range({range.Start}, {range.Stop}, {range.Step})";
    }

    private static string DecodeBytes(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    private static string FormatBytesLiteral(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length + 3);
        builder.Append("b\"");
        foreach (var b in bytes)
        {
            switch (b)
            {
                case (byte)'\\':
                    builder.Append("\\\\");
                    break;
                case (byte)'"':
                    builder.Append("\\\"");
                    break;
                case (byte)'\n':
                    builder.Append("\\n");
                    break;
                case (byte)'\r':
                    builder.Append("\\r");
                    break;
                case (byte)'\t':
                    builder.Append("\\t");
                    break;
                case (byte)'\a':
                    builder.Append("\\a");
                    break;
                case (byte)'\b':
                    builder.Append("\\b");
                    break;
                case (byte)'\f':
                    builder.Append("\\f");
                    break;
                case (byte)'\v':
                    builder.Append("\\v");
                    break;
                default:
                    if (b is >= 0x20 and <= 0x7E)
                    {
                        builder.Append((char)b);
                    }
                    else
                    {
                        builder.Append("\\x");
                        builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                    }
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (ch < ' ')
                    {
                        builder.Append("\\x");
                        builder.Append(((int)ch).ToString("x2", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(ch);
                    }
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static bool ContainsMappingSpec(string format)
    {
        for (var i = 0; i < format.Length - 1; i++)
        {
            if (format[i] != '%')
            {
                continue;
            }

            if (format[i + 1] == '%')
            {
                i++;
                continue;
            }

            if (format[i + 1] == '(')
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatValue(
        StarlarkValue value,
        char specifier,
        int? precision,
        FormatFlags flags)
    {
        return specifier switch
        {
            's' => ToString(value),
            'r' => ToRepr(value),
            'd' or 'i' => FormatInteger(value, precision, flags),
            'o' => FormatIntegerBase(value, 8, false, precision, flags),
            'x' => FormatIntegerBase(value, 16, false, precision, flags),
            'X' => FormatIntegerBase(value, 16, true, precision, flags),
            'f' => FormatFloat(value, precision ?? 6, flags, "F"),
            'g' => FormatFloat(value, precision ?? 6, flags, "G"),
            'c' => FormatChar(value),
            _ => throw new InvalidOperationException($"Unsupported format specifier '{specifier}'.")
        };
    }

    private static string FormatInteger(StarlarkValue value, int? precision, FormatFlags flags)
    {
        if (value is not StarlarkInt intValue)
        {
            throw new InvalidOperationException("Format specifier 'd' requires an integer.");
        }

        var number = intValue.Value;
        var sign = ResolveSign(number, flags);
        var digits = Math.Abs(number).ToString(CultureInfo.InvariantCulture);
        digits = ApplyPrecision(digits, precision);
        return sign + digits;
    }

    private static string FormatIntegerBase(
        StarlarkValue value,
        int radix,
        bool upper,
        int? precision,
        FormatFlags flags)
    {
        if (value is not StarlarkInt intValue)
        {
            throw new InvalidOperationException("Format specifier requires an integer.");
        }

        var number = intValue.Value;
        var sign = ResolveSign(number, flags);
        var digits = Convert.ToString(Math.Abs(number), radix);
        if (digits == null)
        {
            digits = "0";
        }

        if (upper)
        {
            digits = digits.ToUpperInvariant();
        }

        digits = ApplyPrecision(digits, precision);
        return sign + digits;
    }

    private static string FormatFloat(StarlarkValue value, int precision, FormatFlags flags, string format)
    {
        double number = value switch
        {
            StarlarkFloat floatValue => floatValue.Value,
            StarlarkInt intValue => intValue.Value,
            _ => throw new InvalidOperationException("Format specifier requires a number.")
        };

        var sign = ResolveSign(number, flags);
        var digits = number < 0 ? -number : number;
        var text = digits.ToString($"{format}{precision}", CultureInfo.InvariantCulture);
        return sign + text;
    }

    private static string FormatChar(StarlarkValue value)
    {
        if (value is StarlarkString textValue)
        {
            if (textValue.Value.Length != 1)
            {
                throw new InvalidOperationException("Format specifier 'c' requires a single character.");
            }

            return textValue.Value;
        }

        if (value is StarlarkInt intValue)
        {
            var codePoint = intValue.Value;
            if (codePoint < 0 || codePoint > 0x10FFFF)
            {
                throw new InvalidOperationException("Format specifier 'c' requires a valid Unicode code point.");
            }

            if (codePoint >= 0xD800 && codePoint <= 0xDFFF)
            {
                throw new InvalidOperationException("Format specifier 'c' requires a valid Unicode code point.");
            }

            return char.ConvertFromUtf32((int)codePoint);
        }

        throw new InvalidOperationException("Format specifier 'c' requires a character.");
    }

    private static string ResolveSign(double number, FormatFlags flags)
    {
        if (number < 0)
        {
            return "-";
        }

        if (flags.ShowSign)
        {
            return "+";
        }

        return flags.SpaceSign ? " " : string.Empty;
    }

    private static string ApplyPrecision(string digits, int? precision)
    {
        if (precision == null)
        {
            return digits;
        }

        if (precision.Value <= digits.Length)
        {
            return digits;
        }

        return new string('0', precision.Value - digits.Length) + digits;
    }

    private static string ApplyWidth(string text, int? width, FormatFlags flags)
    {
        if (width == null || text.Length >= width.Value)
        {
            return text;
        }

        var padChar = flags.ZeroPad ? '0' : ' ';
        var padding = new string(padChar, width.Value - text.Length);
        if (flags.LeftAdjust)
        {
            return text + padding;
        }

        if (padChar == '0' && (text.StartsWith("-") || text.StartsWith("+") || text.StartsWith(" ")))
        {
            return text[0] + padding + text[1..];
        }

        return padding + text;
    }

    private static FormatFlags ParseFlags(string format, ref int index)
    {
        var flags = new FormatFlags();
        while (index < format.Length)
        {
            switch (format[index])
            {
                case '-':
                    flags.LeftAdjust = true;
                    index++;
                    continue;
                case '+':
                    flags.ShowSign = true;
                    index++;
                    continue;
                case ' ':
                    flags.SpaceSign = true;
                    index++;
                    continue;
                case '0':
                    flags.ZeroPad = true;
                    index++;
                    continue;
                case '#':
                    flags.Alternate = true;
                    index++;
                    continue;
            }

            break;
        }

        return flags;
    }

    private static int? ParseWidth(string format, ref int index)
    {
        var start = index;
        while (index < format.Length && char.IsDigit(format[index]))
        {
            index++;
        }

        if (index == start)
        {
            return null;
        }

        return int.Parse(format[start..index], CultureInfo.InvariantCulture);
    }

    private static int? ParsePrecision(string format, ref int index)
    {
        if (index >= format.Length || format[index] != '.')
        {
            return null;
        }

        index++;
        var start = index;
        while (index < format.Length && char.IsDigit(format[index]))
        {
            index++;
        }

        if (start == index)
        {
            return 0;
        }

        return int.Parse(format[start..index], CultureInfo.InvariantCulture);
    }

    private sealed class FormatArguments
    {
        private readonly IReadOnlyList<StarlarkValue> _sequence;
        private readonly StarlarkDict? _mapping;
        private int _index;
        private readonly bool _isSingle;

        public FormatArguments(StarlarkValue args, bool usesMapping)
        {
            if (usesMapping)
            {
                if (args is not StarlarkDict dict)
                {
                    throw new InvalidOperationException("Format requires a mapping.");
                }

                _sequence = Array.Empty<StarlarkValue>();
                _mapping = dict;
                return;
            }

            switch (args)
            {
                case StarlarkTuple tuple:
                    _sequence = tuple.Items;
                    _isSingle = false;
                    break;
                case StarlarkList list:
                    _sequence = list.Items;
                    _isSingle = false;
                    break;
                default:
                    _sequence = new[] { args };
                    _isSingle = true;
                    break;
            }
        }

        public StarlarkValue Next()
        {
            if (_mapping != null)
            {
                throw new InvalidOperationException("Format requires a mapping.");
            }

            if (_index >= _sequence.Count)
            {
                throw new InvalidOperationException("Not enough arguments for format string.");
            }

            return _sequence[_index++];
        }

        public StarlarkValue Lookup(string key)
        {
            if (_mapping == null)
            {
                throw new InvalidOperationException("Format requires a mapping.");
            }

            var lookupKey = new StarlarkString(key);
            foreach (var entry in _mapping.Entries)
            {
                if (Equals(entry.Key, lookupKey))
                {
                    return entry.Value;
                }
            }

            throw new KeyNotFoundException($"Key '{key}' not found in format mapping.");
        }

        public void EnsureAllConsumed()
        {
            if (_mapping != null)
            {
                return;
            }

            if (_index < _sequence.Count)
            {
                throw new InvalidOperationException(
                    _isSingle
                        ? "Not all arguments converted during string formatting."
                        : "Too many arguments for format string.");
            }
        }
    }

    private struct FormatFlags
    {
        public bool LeftAdjust;
        public bool ShowSign;
        public bool SpaceSign;
        public bool ZeroPad;
        public bool Alternate;
    }

    private sealed class FormattingState
    {
        private readonly HashSet<object> _active =
            new HashSet<object>(ReferenceEqualityComparer.Instance);

        public void Enter(object value)
        {
            if (!_active.Add(value))
            {
                throw new InvalidOperationException("maximum recursion");
            }
        }

        public void Exit(object value)
        {
            _active.Remove(value);
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
