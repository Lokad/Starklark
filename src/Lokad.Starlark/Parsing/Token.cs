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
    [Any("in"), F(Id, true)] In,
    [Any("True"), F(Id, true)] True,
    [Any("False"), F(Id, true)] False,
    [Any("def"), F(Id, true)] Def,
    [Any("for"), F(Id, true)] For,
    [Any("if"), F(Id, true)] If,
    [Any("elif"), F(Id, true)] Elif,
    [Any("else"), F(Id, true)] Else,
    [Any("return"), F(Id, true)] Return,
    [Any("break"), F(Id, true)] Break,
    [Any("continue"), F(Id, true)] Continue,
    [Any("pass"), F(Id, true)] Pass,

    [Any("==")] Equal,
    [Any("!=")] NotEqual,
    [Any("<=")] LessEqual,
    [Any(">=")] GreaterEqual,
    [Any("<")] Less,
    [Any(">")] Greater,
    [Any("=")] Assign,
    [Any("+")] Plus,
    [Any("-")] Minus,
    [Any("*")] Star,
    [Any("/")] Slash,

    [Any("(")] OpenParen,
    [Any(")")] CloseParen,
    [Any("[")] OpenBracket,
    [Any("]")] CloseBracket,
    [Any("{")] OpenBrace,
    [Any("}")] CloseBrace,
    [Any(",")] Comma,
    [Any(":")] Colon,

    [Pattern("[A-Za-z_][A-Za-z0-9_]*")]
    Id,

    [Any("None"), F(Id, true)] None,

    [Pattern("[0-9]+(\\.[0-9]+)?", Start = "0123456789")]
    Number,

    [Pattern("\"([^\"\\\\\\n]|\\\\.)*\"", Start = "\"")]
    String,
}
