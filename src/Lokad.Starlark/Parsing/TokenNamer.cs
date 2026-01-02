using Lokad.Parsing.Error;

namespace Lokad.Starlark.Parsing;

public sealed class TokenNamer : ITokenNamer<Token>
{
    public static readonly TokenNamer Instance = new TokenNamer();

    public bool IsFolded(Token token, ICollection<Token> others) => false;

    public string TokenName(Token token, ICollection<Token> others)
    {
        return token switch
        {
            Token.Error => "error",
            Token.EoS => "end-of-script",
            Token.EoL => "end-of-line",
            Token.Indent => "indent",
            Token.Dedent => "dedent",
            Token.Id => "identifier",
            Token.Number => "number",
            Token.String => "string",
            Token.True => "'True'",
            Token.False => "'False'",
            Token.And => "'and'",
            Token.Or => "'or'",
            Token.Not => "'not'",
            Token.If => "'if'",
            Token.Else => "'else'",
            Token.Equal => "'=='",
            Token.NotEqual => "'!='",
            Token.Assign => "'='",
            Token.Plus => "'+'",
            Token.Minus => "'-'",
            Token.Star => "'*'",
            Token.Slash => "'/'",
            Token.OpenParen => "'('",
            Token.CloseParen => "')'",
            Token.Comma => "','",
            Token.Colon => "':'",
            _ => token.ToString()
        };
    }
}
