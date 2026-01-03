namespace Lokad.Starlark.Parsing;

internal static class StringEscapes
{
    public static string Unescape(string text)
    {
        if (text.IndexOf('\\') < 0)
        {
            return text;
        }

        var builder = new System.Text.StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch != '\\')
            {
                builder.Append(ch);
                continue;
            }

            if (i + 1 >= text.Length)
            {
                builder.Append('\\');
                break;
            }

            var next = text[++i];
            switch (next)
            {
                case '\\': builder.Append('\\'); break;
                case '"': builder.Append('"'); break;
                case '\'': builder.Append('\''); break;
                case 'n': builder.Append('\n'); break;
                case 'r': builder.Append('\r'); break;
                case 't': builder.Append('\t'); break;
                case 'a': builder.Append('\a'); break;
                case 'b': builder.Append('\b'); break;
                case 'f': builder.Append('\f'); break;
                case 'v': builder.Append('\v'); break;
                case 'x':
                    builder.Append(ReadHexEscape(text, ref i, 2));
                    break;
                case 'u':
                    builder.Append(ReadHexEscape(text, ref i, 4));
                    break;
                case 'U':
                    builder.Append(ReadHexEscape(text, ref i, 8));
                    break;
                default:
                    if (next is >= '0' and <= '7')
                    {
                        builder.Append(ReadOctalEscape(text, ref i, next));
                    }
                    else
                    {
                        builder.Append(next);
                    }
                    break;
            }
        }

        return builder.ToString();
    }

    private static char ReadHexEscape(string text, ref int index, int digits)
    {
        var value = 0;
        for (var i = 0; i < digits && index + 1 < text.Length; i++)
        {
            var digit = text[++index];
            var hex = digit switch
            {
                >= '0' and <= '9' => digit - '0',
                >= 'a' and <= 'f' => digit - 'a' + 10,
                >= 'A' and <= 'F' => digit - 'A' + 10,
                _ => 0
            };
            value = (value * 16) + hex;
        }

        return (char)value;
    }

    private static char ReadOctalEscape(string text, ref int index, char firstDigit)
    {
        var value = firstDigit - '0';
        for (var i = 0; i < 2 && index + 1 < text.Length; i++)
        {
            var digit = text[index + 1];
            if (digit < '0' || digit > '7')
            {
                break;
            }

            index++;
            value = (value * 8) + (digit - '0');
        }

        return (char)value;
    }
}
