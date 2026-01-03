namespace Lokad.Starlark.Parsing;

internal static class StringLiteralParser
{
    public static string Parse(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return string.Empty;
        }

        var offset = 0;
        if (token.Length >= 2 && (token[0] == 'r' || token[0] == 'R'))
        {
            offset = 1;
        }

        if (token.Length - offset < 2)
        {
            return string.Empty;
        }

        var quote = token[offset];
        if (quote != '"' && quote != '\'')
        {
            return string.Empty;
        }

        var text = token.Substring(offset + 1, token.Length - offset - 2);
        return offset == 1 ? text : StringEscapes.Unescape(text);
    }
}
