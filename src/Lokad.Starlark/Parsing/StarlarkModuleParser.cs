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
        [NT] Expression target,
        [T(Token.Assign)] Pos<string> assign,
        [NT] Expression value)
    {
        var span = SpanBetween(target.Span, value.Span);
        return new SimpleStatement(new AssignmentStatement(ToAssignmentTarget(target), value, span));
    }

    [Rule]
    public SimpleStatement AugmentedAssignment(
        [NT] Expression target,
        [T(Token.PlusAssign, Token.MinusAssign, Token.StarAssign, Token.SlashAssign, Token.FloorDivideAssign, Token.PercentAssign, Token.AmpersandAssign, Token.PipeAssign, Token.CaretAssign, Token.ShiftLeftAssign, Token.ShiftRightAssign)] Pos<string> op,
        [NT] Expression value)
    {
        var operatorValue = op.Value switch
        {
            "+=" => BinaryOperator.Add,
            "-=" => BinaryOperator.Subtract,
            "*=" => BinaryOperator.Multiply,
            "/=" => BinaryOperator.Divide,
            "//=" => BinaryOperator.FloorDivide,
            "%=" => BinaryOperator.Modulo,
            "&=" => BinaryOperator.BitwiseAnd,
            "|=" => BinaryOperator.BitwiseOr,
            "^=" => BinaryOperator.BitwiseXor,
            "<<=" => BinaryOperator.ShiftLeft,
            ">>=" => BinaryOperator.ShiftRight,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op.Value, null)
        };

        return new SimpleStatement(
            new AugmentedAssignmentStatement(
                ToAssignmentTarget(target),
                operatorValue,
                value,
                SpanBetween(target.Span, value.Span)));
    }

    [Rule]
    public SimpleStatement ExpressionStatement([NT] Expression expression)
    {
        return new SimpleStatement(new ExpressionStatement(expression, expression.Span));
    }

    [Rule]
    public SimpleStatement ReturnStatement(
        [T(Token.Return)] Pos<string> keyword,
        [NTO] Expression? value)
    {
        var span = value == null ? keyword.Location : SpanBetween(keyword.Location, value.Span);
        return new SimpleStatement(new ReturnStatement(value, span));
    }

    [Rule]
    public SimpleStatement BreakStatement([T(Token.Break)] Pos<string> keyword)
    {
        return new SimpleStatement(new BreakStatement(keyword.Location));
    }

    [Rule]
    public SimpleStatement ContinueStatement([T(Token.Continue)] Pos<string> keyword)
    {
        return new SimpleStatement(new ContinueStatement(keyword.Location));
    }

    [Rule]
    public SimpleStatement PassStatement([T(Token.Pass)] Pos<string> keyword)
    {
        return new SimpleStatement(new PassStatement(keyword.Location));
    }

    [Rule]
    public SimpleStatement LoadStatement(
        [T(Token.Load)] Pos<string> keyword,
        [T(Token.OpenParen)] Pos<string> openParen,
        [T(Token.String)] Pos<string> module,
        [T(Token.Comma)] Pos<string> comma,
        [L(Sep = Token.Comma, Min = 1)] LoadBinding[] bindings,
        [T(Token.CloseParen)] Pos<string> closeParen)
    {
        return new SimpleStatement(
            new LoadStatement(
                ParseStringLiteral(module.Value),
                bindings,
                SpanBetween(keyword.Location, closeParen.Location)));
    }

    [Rule]
    public CompoundStatement IfStatement(
        [T(Token.If)] Pos<string> keyword,
        [NT] Expression condition,
        [T(Token.Colon)] Pos<string> colon,
        [NT] Suite thenSuite,
        [L] ElifClause[] elifClauses,
        [NTO] ElseClause? elseClause)
    {
        var clauses = new List<IfClause>(1 + elifClauses.Length);
        clauses.Add(new IfClause(condition, thenSuite.Statements, condition.Span));
        for (var i = 0; i < elifClauses.Length; i++)
        {
            var clause = elifClauses[i];
            clauses.Add(new IfClause(clause.Condition, clause.Statements, clause.Condition.Span));
        }

        var elseStatements = elseClause?.Statements ?? Array.Empty<Statement>();
        var spanEnd = clauses.Count > 0 ? clauses[^1].Span : keyword.Location;
        return new CompoundStatement(
            new IfStatement(clauses, elseStatements, SpanBetween(keyword.Location, spanEnd)));
    }

    [Rule]
    public ElifClause ElifClause(
        [T(Token.Elif)] Pos<string> keyword,
        [NT] Expression condition,
        [T(Token.Colon)] Pos<string> colon,
        [NT] Suite suite)
    {
        return new ElifClause(condition, suite.Statements);
    }

    [Rule]
    public ElseClause ElseClause(
        [T(Token.Else)] Pos<string> keyword,
        [T(Token.Colon)] Pos<string> colon,
        [NT] Suite suite)
    {
        return new ElseClause(suite.Statements);
    }

    [Rule]
    public CompoundStatement ForStatement(
        [T(Token.For)] Pos<string> keyword,
        [NT] AssignmentTarget target,
        [T(Token.In)] Pos<string> inKeyword,
        [NT] Expression iterable,
        [T(Token.Colon)] Pos<string> colon,
        [NT] Suite suite)
    {
        return new CompoundStatement(
            new ForStatement(
                target,
                iterable,
                suite.Statements,
                SpanBetween(keyword.Location, iterable.Span)));
    }

    [Rule]
    public CompoundStatement FunctionDefinition(
        [T(Token.Def)] Pos<string> keyword,
        [T(Token.Id)] Pos<string> name,
        [T(Token.OpenParen)] Pos<string> openParen,
        [L(Sep = Token.Comma)] FunctionParameter[] parameters,
        [T(Token.CloseParen)] Pos<string> closeParen,
        [T(Token.Colon)] Pos<string> colon,
        [NT] Suite suite)
    {
        ValidateParameters(parameters, name.Value);
        return new CompoundStatement(
            new FunctionDefinitionStatement(
                name.Value,
                parameters,
                suite.Statements,
                SpanBetween(keyword.Location, closeParen.Location)));
    }

    [Rule]
    public LoadBinding LoadBindingName([T(Token.String)] Pos<string> name)
    {
        var value = ParseStringLiteral(name.Value);
        return new LoadBinding(value, value);
    }

    [Rule]
    public LoadBinding LoadBindingAlias(
        [T(Token.Id)] Pos<string> alias,
        [T(Token.Assign)] Pos<string> assign,
        [T(Token.String)] Pos<string> name)
    {
        return new LoadBinding(ParseStringLiteral(name.Value), alias.Value);
    }

    private static string ParseStringLiteral(string value)
    {
        var parsed = StringLiteralParser.Parse(value);
        if (parsed is string text)
        {
            return text;
        }

        throw new InvalidOperationException("Expected string literal.");
    }

    [Rule]
    public Suite SingleLineSuite([NT] SimpleStatement statement)
    {
        return new Suite(new[] { statement.Statement });
    }

    [Rule]
    public Suite BlockSuite(
        [T(Token.EoL)] Pos<string> eol,
        [T(Token.Indent)] Pos<string> indent,
        [L] StatementLine[] lines,
        [T(Token.Dedent)] Pos<string> dedent)
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
