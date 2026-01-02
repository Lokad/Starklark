using Lokad.Parsing.Lexer;

namespace Lokad.Starlark.Parsing;

public sealed class FAttribute : FromAttribute
{
    public FAttribute(Token parent, bool isPrivate = false) : base((int)parent, isPrivate) { }
}

[Tokens(Comments = "#[^\\n]*", EscapeNewlines = true)]
public enum Token
{
    [Error] Error,
    [End] EoS,
    [EndOfLine] EoL,
    [Indent] Indent,
    [Dedent] Dedent,

    [Any("and"), F(Id, true)] And,
    [Any("or"), F(Id, true)] Or,
    [Any("not"), F(Id, true)] Not,
    [Any("True"), F(Id, true)] True,
    [Any("False"), F(Id, true)] False,

    [Any("==")] Equal,
    [Any("!=")] NotEqual,
    [Any("=")] Assign,
    [Any("+")] Plus,
    [Any("-")] Minus,
    [Any("*")] Star,
    [Any("/")] Slash,

    [Any("(")] OpenParen,
    [Any(")")] CloseParen,
    [Any(",")] Comma,
    [Any(":")] Colon,

    [Pattern("[A-Za-z_][A-Za-z0-9_]*")]
    Id,

    [Any("if"), F(Id, true)] If,
    [Any("else"), F(Id, true)] Else,
    [Any("None"), F(Id, true)] None,

    [Pattern("[0-9]+(\\.[0-9]+)?", Start = "0123456789")]
    Number,

    [Pattern("\"([^\"\\\\\\n]|\\\\.)*\"", Start = "\"")]
    String,
}
