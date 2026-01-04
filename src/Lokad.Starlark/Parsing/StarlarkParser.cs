using System.Linq;
using Lokad.Parsing;
using Lokad.Parsing.Error;
using Lokad.Parsing.Parser;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Parsing;

public sealed class StarlarkParser : StarlarkGrammar<StarlarkParser, ExpressionRoot>
{
    public static Expression ParseExpression(string source)
    {
        var parser = new StarlarkParser();
        var tokens = MakeTokenReader().ReadAllTokens(source);

        if (tokens.HasInvalidTokens)
        {
            var t = tokens.Tokens.First(tok => tok.Token == Token.Error);
            tokens.LineOfPosition(t.Start, out var line, out var col);
            var location = new SourceLocation(t.Start, line, col);
            throw new StarlarkParseException(
                $"Invalid character: '{source[t.Start]}'.",
                new SourceSpan(location, t.Length));
        }

        tokens = TokenFiltering.DropLineTokensInsideBrackets(tokens);
        parser.Tokens = tokens;

        try
        {
            return StreamParser(parser, tokens).Expression;
        }
        catch (ParseException ex)
        {
            throw new StarlarkParseException(
                $"syntax error: found `{ex.Token}` but expected {string.Concat(", ", ex.Expected)}.",
                ex.Location,
                ex);
        }
    }

    [Rule]
    public ExpressionRoot Root(
        [NT] Expression expr,
        [L] LineEnding[] trailing,
        [T(Token.EoS)] Token eos)
    {
        return new ExpressionRoot(expr);
    }
}

public readonly record struct ExpressionRoot(Expression Expression);

public sealed class StarlarkParseException : Exception
{
    public SourceSpan Location { get; }

    public StarlarkParseException(string message, SourceSpan location, Exception? inner = null)
        : base(message, inner)
    {
        Location = location;
    }
}
