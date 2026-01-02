using System.Linq;
using Lokad.Parsing;
using Lokad.Parsing.Error;
using Lokad.Parsing.Parser;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Parsing;

public sealed class StarlarkModuleParser : StarlarkGrammar<StarlarkModuleParser, ModuleRoot>
{
    public static StarlarkModule ParseModule(string source)
    {
        var parser = new StarlarkModuleParser();
        var tokens = parser.Tokens = MakeTokenReader().ReadAllTokens(source);

        if (tokens.HasInvalidTokens)
        {
            var t = tokens.Tokens.First(tok => tok.Token == Token.Error);
            tokens.LineOfPosition(t.Start, out var line, out var col);
            var location = new SourceLocation(t.Start, line, col);
            throw new StarlarkParseException(
                $"Invalid character: '{source[t.Start]}'.",
                new SourceSpan(location, t.Length));
        }

        try
        {
            return StreamParser(parser, tokens).Module;
        }
        catch (ParseException ex)
        {
            throw new StarlarkParseException(
                $"Found `{ex.Token}` but expected {string.Concat(", ", ex.Expected)}.",
                ex.Location,
                ex);
        }
    }

    [Rule]
    public ModuleRoot Root([L] StatementLine[] lines, [T(Token.EoS)] Token eos)
    {
        var statements = lines
            .Select(line => line.Statement)
            .Where(statement => statement != null)
            .Select(statement => statement!)
            .ToArray();

        return new ModuleRoot(new StarlarkModule(statements));
    }

    [Rule]
    public StatementLine StatementLine([NTO] Statement? statement, [T(Token.EoL)] Token eol)
    {
        return new StatementLine(statement);
    }

    [Rule]
    public Statement Assignment(
        [T(Token.Id)] string name,
        [T(Token.Assign)] Token assign,
        [NT] Expression value)
    {
        return new AssignmentStatement(name, value);
    }

    [Rule]
    public Statement ExpressionStatement([NT] Expression expression)
    {
        return new ExpressionStatement(expression);
    }
}

public readonly record struct ModuleRoot(StarlarkModule Module);

public readonly record struct StatementLine(Statement? Statement);
