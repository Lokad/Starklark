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
        [T(Token.Assign)] Token assign,
        [NT] Expression value)
    {
        return new SimpleStatement(new AssignmentStatement(ToAssignmentTarget(target), value));
    }

    [Rule]
    public SimpleStatement AugmentedAssignment(
        [NT] Expression target,
        [T(Token.PlusAssign, Token.MinusAssign, Token.StarAssign, Token.SlashAssign, Token.FloorDivideAssign, Token.PercentAssign, Token.AmpersandAssign, Token.PipeAssign, Token.CaretAssign, Token.ShiftLeftAssign, Token.ShiftRightAssign)] Token op,
        [NT] Expression value)
    {
        var operatorValue = op switch
        {
            Token.PlusAssign => BinaryOperator.Add,
            Token.MinusAssign => BinaryOperator.Subtract,
            Token.StarAssign => BinaryOperator.Multiply,
            Token.SlashAssign => BinaryOperator.Divide,
            Token.FloorDivideAssign => BinaryOperator.FloorDivide,
            Token.PercentAssign => BinaryOperator.Modulo,
            Token.AmpersandAssign => BinaryOperator.BitwiseAnd,
            Token.PipeAssign => BinaryOperator.BitwiseOr,
            Token.CaretAssign => BinaryOperator.BitwiseXor,
            Token.ShiftLeftAssign => BinaryOperator.ShiftLeft,
            Token.ShiftRightAssign => BinaryOperator.ShiftRight,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };

        return new SimpleStatement(
            new AugmentedAssignmentStatement(ToAssignmentTarget(target), operatorValue, value));
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
    public SimpleStatement LoadStatement(
        [T(Token.Load)] Token keyword,
        [T(Token.OpenParen)] Token openParen,
        [T(Token.String)] string module,
        [T(Token.Comma)] Token comma,
        [L(Sep = Token.Comma)] LoadBinding[] bindings,
        [T(Token.CloseParen)] Token closeParen)
    {
        return new SimpleStatement(
            new LoadStatement(ParseStringLiteral(module), bindings));
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
        [NT] AssignmentTarget target,
        [T(Token.In)] Token inKeyword,
        [NT] Expression iterable,
        [T(Token.Colon)] Token colon,
        [NT] Suite suite)
    {
        return new CompoundStatement(new ForStatement(target, iterable, suite.Statements));
    }

    [Rule]
    public CompoundStatement FunctionDefinition(
        [T(Token.Def)] Token keyword,
        [T(Token.Id)] string name,
        [T(Token.OpenParen)] Token openParen,
        [L(Sep = Token.Comma)] FunctionParameter[] parameters,
        [T(Token.CloseParen)] Token closeParen,
        [T(Token.Colon)] Token colon,
        [NT] Suite suite)
    {
        ValidateParameters(parameters, name);
        return new CompoundStatement(new FunctionDefinitionStatement(name, parameters, suite.Statements));
    }

    [Rule]
    public FunctionParameter ParameterName([T(Token.Id)] string name)
    {
        return new FunctionParameter(name, null, ParameterKind.Normal);
    }

    [Rule]
    public FunctionParameter ParameterDefault(
        [T(Token.Id)] string name,
        [T(Token.Assign)] Token assign,
        [NT(2)] Expression value)
    {
        return new FunctionParameter(name, value, ParameterKind.Normal);
    }

    [Rule]
    public FunctionParameter ParameterVarArgs(
        [T(Token.Star)] Token star,
        [T(Token.Id)] string name)
    {
        return new FunctionParameter(name, null, ParameterKind.VarArgs);
    }

    [Rule]
    public FunctionParameter ParameterKwArgs(
        [T(Token.StarStar)] Token star,
        [T(Token.Id)] string name)
    {
        return new FunctionParameter(name, null, ParameterKind.KwArgs);
    }

    [Rule]
    public LoadBinding LoadBindingName([T(Token.String)] string name)
    {
        var value = ParseStringLiteral(name);
        return new LoadBinding(value, value);
    }

    [Rule]
    public LoadBinding LoadBindingAlias(
        [T(Token.Id)] string alias,
        [T(Token.Assign)] Token assign,
        [T(Token.String)] string name)
    {
        return new LoadBinding(ParseStringLiteral(name), alias);
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

    private static void ValidateParameters(IReadOnlyList<FunctionParameter> parameters, string functionName)
    {
        var seenDefault = false;
        var seenVarArgs = false;
        var seenKwArgs = false;
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            if (parameter.Kind == ParameterKind.VarArgs)
            {
                if (seenVarArgs)
                {
                    throw new InvalidOperationException(
                        $"Multiple *args parameters in '{functionName}'.");
                }

                if (seenKwArgs)
                {
                    throw new InvalidOperationException(
                        $"*args must appear before **kwargs in '{functionName}'.");
                }

                if (parameter.Default != null)
                {
                    throw new InvalidOperationException(
                        $"*args parameter '{parameter.Name}' cannot have a default in '{functionName}'.");
                }

                seenVarArgs = true;
                continue;
            }

            if (parameter.Kind == ParameterKind.KwArgs)
            {
                if (seenKwArgs)
                {
                    throw new InvalidOperationException(
                        $"Multiple **kwargs parameters in '{functionName}'.");
                }

                if (parameter.Default != null)
                {
                    throw new InvalidOperationException(
                        $"**kwargs parameter '{parameter.Name}' cannot have a default in '{functionName}'.");
                }

                seenKwArgs = true;
                continue;
            }

            if (seenVarArgs || seenKwArgs)
            {
                throw new InvalidOperationException(
                    $"Parameter '{parameter.Name}' must appear before *args or **kwargs in '{functionName}'.");
            }

            if (parameter.Default != null)
            {
                seenDefault = true;
                continue;
            }

            if (seenDefault)
            {
                throw new InvalidOperationException(
                    $"Non-default parameter '{parameter.Name}' follows default parameter in '{functionName}'.");
            }
        }
    }
}

public readonly record struct ModuleRoot(StarlarkModule Module);

public readonly record struct StatementLine(Statement? Statement);

public readonly record struct Suite(IReadOnlyList<Statement> Statements);

public readonly record struct ElifClause(Expression Condition, IReadOnlyList<Statement> Statements);

public readonly record struct ElseClause(IReadOnlyList<Statement> Statements);

public readonly record struct SimpleStatement(Statement Statement);

public readonly record struct CompoundStatement(Statement Statement);
