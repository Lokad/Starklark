using System.Collections.Generic;
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
        return new ModuleRoot(new StarlarkModule(CollectStatements(lines)));
    }

    [Rule]
    public StatementLine BlankLine([T(Token.EoL)] Token eol)
    {
        return new StatementLine(null);
    }

    [Rule]
    public StatementLine SimpleStatementLine([NT] SimpleStatement statement, [T(Token.EoL)] Token eol)
    {
        return new StatementLine(statement.Statement);
    }

    [Rule]
    public StatementLine CompoundStatementLine([NT] CompoundStatement statement, [O(Token.EoL)] Token? eol)
    {
        return new StatementLine(statement.Statement);
    }

    [Rule]
    public SimpleStatement Assignment(
        [T(Token.Id)] string name,
        [T(Token.Assign)] Token assign,
        [NT] Expression value)
    {
        return new SimpleStatement(new AssignmentStatement(name, value));
    }

    [Rule]
    public SimpleStatement ExpressionStatement([NT] Expression expression)
    {
        return new SimpleStatement(new ExpressionStatement(expression));
    }

    [Rule]
    public CompoundStatement IfStatement(
        [T(Token.If)] Token keyword,
        [NT] Expression condition,
        [T(Token.Colon)] Token colon,
        [NT] Suite thenSuite,
        [NTO] ElseClause? elseClause)
    {
        var elseStatements = elseClause?.Statements ?? Array.Empty<Statement>();
        return new CompoundStatement(new IfStatement(condition, thenSuite.Statements, elseStatements));
    }

    [Rule]
    public ElseClause ElseClause(
        [T(Token.Else)] Token keyword,
        [T(Token.Colon)] Token colon,
        [NT] Suite suite)
    {
        return new ElseClause(suite.Statements);
    }

    [Rule]
    public Suite SingleLineSuite([NT] SimpleStatement statement)
    {
        return new Suite(new[] { statement.Statement });
    }

    [Rule]
    public Suite BlockSuite(
        [T(Token.EoL)] Token eol,
        [T(Token.Indent)] Token indent,
        [L] StatementLine[] lines,
        [T(Token.Dedent)] Token dedent)
    {
        return new Suite(CollectStatements(lines));
    }

    private static Statement[] CollectStatements(StatementLine[] lines)
    {
        return lines
            .Select(line => line.Statement)
            .Where(statement => statement != null)
            .Select(statement => statement!)
            .ToArray();
    }
}

public readonly record struct ModuleRoot(StarlarkModule Module);

public readonly record struct StatementLine(Statement? Statement);

public readonly record struct Suite(IReadOnlyList<Statement> Statements);

public readonly record struct ElseClause(IReadOnlyList<Statement> Statements);

public readonly record struct SimpleStatement(Statement Statement);

public readonly record struct CompoundStatement(Statement Statement);
