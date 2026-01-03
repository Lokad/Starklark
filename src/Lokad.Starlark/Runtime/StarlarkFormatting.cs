using System.Globalization;
using System.Text;

namespace Lokad.Starlark.Runtime;

public static class StarlarkFormatting
{
    public static string ToString(StarlarkValue value) => Format(value, quoteStrings: false);

    public static string ToRepr(StarlarkValue value) => Format(value, quoteStrings: true);

    private static string Format(StarlarkValue value, bool quoteStrings)
    {
        return value switch
        {
            StarlarkString text => quoteStrings ? Quote(text.Value) : text.Value,
            StarlarkBool boolean => boolean.Value ? "True" : "False",
            StarlarkNone => "None",
            StarlarkInt integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            StarlarkFloat number => number.Value.ToString("G", CultureInfo.InvariantCulture),
            StarlarkList list => FormatList(list.Items),
            StarlarkTuple tuple => FormatTuple(tuple.Items),
            StarlarkDict dict => FormatDict(dict.Entries),
            StarlarkRange range => FormatRange(range),
            StarlarkFunction function => $"<function {function.Name}>",
            StarlarkUserFunction function => $"<function {function.Name}>",
            StarlarkCallable => "<function>",
            _ => value.TypeName
        };
    }

    private static string FormatList(IReadOnlyList<StarlarkValue> items)
    {
        var builder = new StringBuilder();
        builder.Append('[');
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(ToRepr(items[i]));
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string FormatTuple(IReadOnlyList<StarlarkValue> items)
    {
        var builder = new StringBuilder();
        builder.Append('(');
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(ToRepr(items[i]));
        }

        if (items.Count == 1)
        {
            builder.Append(',');
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static string FormatDict(IReadOnlyList<KeyValuePair<StarlarkValue, StarlarkValue>> entries)
    {
        var builder = new StringBuilder();
        builder.Append('{');
        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(ToRepr(entries[i].Key));
            builder.Append(": ");
            builder.Append(ToRepr(entries[i].Value));
        }

        builder.Append('}');
        return builder.ToString();
    }

    private static string FormatRange(StarlarkRange range)
    {
        return range.Step == 1
            ? $"range({range.Start}, {range.Stop})"
            : $"range({range.Start}, {range.Stop}, {range.Step})";
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
}
