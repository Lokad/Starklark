using System.Collections.Generic;
using Lokad.Parsing.Lexer;

namespace Lokad.Starlark.Parsing;

internal static class TokenFiltering
{
    public static LexerResult<Token> DropLineTokensInsideBrackets(LexerResult<Token> tokens)
    {
        var filtered = new List<LexerToken<Token>>(tokens.Count);
        var depth = 0;
        var suppressedIndent = 0;
        var sourceTokens = tokens.Tokens;

        for (var i = 0; i < sourceTokens.Count; i++)
        {
            var token = sourceTokens[i];
            switch (token.Token)
            {
                case Token.OpenParen:
                case Token.OpenBracket:
                case Token.OpenBrace:
                    depth++;
                    filtered.Add(token);
                    continue;
                case Token.CloseParen:
                case Token.CloseBracket:
                case Token.CloseBrace:
                    if (depth > 0)
                    {
                        depth--;
                    }

                    filtered.Add(token);
                    continue;
                case Token.EoL:
                    if (depth > 0)
                    {
                        continue;
                    }

                    break;
                case Token.Indent:
                    if (depth > 0)
                    {
                        suppressedIndent++;
                        continue;
                    }

                    break;
                case Token.Dedent:
                    if (suppressedIndent > 0)
                    {
                        suppressedIndent--;
                        continue;
                    }

                    if (depth > 0)
                    {
                        continue;
                    }

                    break;
            }

            if (token.Token == Token.Comma && IsTrailingComma(sourceTokens, i, depth))
            {
                filtered.Add(new LexerToken<Token>(Token.TrailingComma, token.Start, token.Length));
            }
            else
            {
                filtered.Add(token);
            }
        }

        return new LexerResult<Token>(tokens.Buffer, filtered, tokens.Newlines, tokens.HasInvalidTokens);
    }

    private static bool IsTrailingComma(IReadOnlyList<LexerToken<Token>> tokens, int index, int depth)
    {
        for (var i = index + 1; i < tokens.Count; i++)
        {
            var token = tokens[i].Token;
            if (token == Token.EoL || token == Token.Indent || token == Token.Dedent)
            {
                continue;
            }

            if (depth > 0)
            {
                return token == Token.CloseParen
                    || token == Token.CloseBracket
                    || token == Token.CloseBrace;
            }

            return token == Token.Colon;
        }

        return false;
    }
}
