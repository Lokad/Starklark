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
    public SimpleStatement ReturnStatement(
        [T(Token.Return)] Token keyword,
        [NTO] Expression? value)
    {
        return new SimpleStatement(new ReturnStatement(value));
    }

    [Rule]
    public SimpleStatement BreakStatement([T(Token.Break)] Token keyword)
    {
        return new SimpleStatement(new BreakStatement());
    }

    [Rule]
    public SimpleStatement ContinueStatement([T(Token.Continue)] Token keyword)
    {
        return new SimpleStatement(new ContinueStatement());
    }

    [Rule]
    public SimpleStatement PassStatement([T(Token.Pass)] Token keyword)
    {
        return new SimpleStatement(new PassStatement());
    }

    [Rule]
    public CompoundStatement IfStatement(
        [T(Token.If)] Token keyword,
        [NT] Expression condition,
        [T(Token.Colon)] Token colon,
        [NT] Suite thenSuite,
        [L] ElifClause[] elifClauses,
        [NTO] ElseClause? elseClause)
    {
        var clauses = new List<IfClause>(1 + elifClauses.Length);
        clauses.Add(new IfClause(condition, thenSuite.Statements));
        for (var i = 0; i < elifClauses.Length; i++)
        {
            var clause = elifClauses[i];
            clauses.Add(new IfClause(clause.Condition, clause.Statements));
        }

        var elseStatements = elseClause?.Statements ?? Array.Empty<Statement>();
        return new CompoundStatement(new IfStatement(clauses, elseStatements));
    }

    [Rule]
    public ElifClause ElifClause(
        [T(Token.Elif)] Token keyword,
        [NT] Expression condition,
        [T(Token.Colon)] Token colon,
        [NT] Suite suite)
    {
        return new ElifClause(condition, suite.Statements);
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
    public CompoundStatement ForStatement(
        [T(Token.For)] Token keyword,
        [T(Token.Id)] string name,
        [T(Token.In)] Token inKeyword,
        [NT] Expression iterable,
        [T(Token.Colon)] Token colon,
        [NT] Suite suite)
    {
        return new CompoundStatement(new ForStatement(name, iterable, suite.Statements));
    }

    [Rule]
    public CompoundStatement FunctionDefinition(
        [T(Token.Def)] Token keyword,
        [T(Token.Id)] string name,
        [T(Token.OpenParen)] Token openParen,
        [L(Sep = Token.Comma)] string[] parameters,
        [T(Token.CloseParen)] Token closeParen,
        [T(Token.Colon)] Token colon,
        [NT] Suite suite)
    {
        return new CompoundStatement(new FunctionDefinitionStatement(name, parameters, suite.Statements));
    }

    [Rule]
    public string ParameterName([T(Token.Id)] string name) => name;

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

public readonly record struct ElifClause(Expression Condition, IReadOnlyList<Statement> Statements);

public readonly record struct ElseClause(IReadOnlyList<Statement> Statements);

public readonly record struct SimpleStatement(Statement Statement);

public readonly record struct CompoundStatement(Statement Statement);
