namespace Lokad.Starlark.Parsing;

internal static class StringLiteralParser
{
    public static object Parse(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return string.Empty;
        }

        var offset = 0;
        var isRaw = false;
        var isBytes = false;
        while (offset < token.Length && offset < 2)
        {
            var prefix = token[offset];
            if (prefix is 'r' or 'R')
            {
                isRaw = true;
            }
            else if (prefix is 'b' or 'B')
            {
                isBytes = true;
            }
            else
            {
                break;
            }

            offset++;
        }

        if (token.Length - offset < 2)
        {
            return string.Empty;
        }

        var quoteLength = 1;
        if (token.Length - offset >= 6)
        {
            var quoteCandidate = token.Substring(offset, 3);
            if (quoteCandidate == "\"\"\"" || quoteCandidate == "'''")
            {
                quoteLength = 3;
            }
        }

        var quote = token.Substring(offset, quoteLength);
        if (quote is not ("\"" or "'" or "\"\"\"" or "'''"))
        {
            return string.Empty;
        }

        var text = token.Substring(offset + quoteLength, token.Length - offset - (quoteLength * 2));
        if (isBytes)
        {
            return isRaw
                ? System.Text.Encoding.UTF8.GetBytes(text)
                : BytesEscapes.Unescape(text);
        }

        return isRaw ? text : StringEscapes.Unescape(text);
    }
}
