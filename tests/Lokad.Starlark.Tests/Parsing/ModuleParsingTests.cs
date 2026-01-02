using Lokad.Starlark.Parsing;
using Lokad.Starlark.Syntax;
using Xunit;

namespace Lokad.Starlark.Tests.Parsing;

public sealed class ModuleParsingTests
{
    [Fact]
    public void ParsesAssignmentStatement()
    {
        var module = StarlarkModuleParser.ParseModule("x = 1\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(statement);
        Assert.Equal("x", assignment.Name);
        Assert.Equal(new LiteralExpression(1L), assignment.Value);
    }

    [Fact]
    public void ParsesExpressionStatement()
    {
        var module = StarlarkModuleParser.ParseModule("1 + 2\n");

        var statement = Assert.Single(module.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(statement);
        Assert.Equal(
            new BinaryExpression(
                new LiteralExpression(1L),
                BinaryOperator.Add,
                new LiteralExpression(2L)),
            expressionStatement.Expression);
    }

    [Fact]
    public void ParsesInlineIfStatement()
    {
        var module = StarlarkModuleParser.ParseModule("if True: x = 1\n");

        var statement = Assert.Single(module.Statements);
        var ifStatement = Assert.IsType<IfStatement>(statement);
        var clause = Assert.Single(ifStatement.Clauses);
        Assert.Equal(new LiteralExpression(true), clause.Condition);
        Assert.Single(clause.Statements);
        Assert.Empty(ifStatement.ElseStatements);
    }

    [Fact]
    public void ParsesIndentedIfStatement()
    {
        var module = StarlarkModuleParser.ParseModule("if True:\n  x = 1\n");

        var statement = Assert.Single(module.Statements);
        var ifStatement = Assert.IsType<IfStatement>(statement);
        var clause = Assert.Single(ifStatement.Clauses);
        Assert.Equal(new LiteralExpression(true), clause.Condition);
        Assert.Single(clause.Statements);
        Assert.Empty(ifStatement.ElseStatements);
    }

    [Fact]
    public void ParsesElifChain()
    {
        var module = StarlarkModuleParser.ParseModule("if True:\n  x = 1\nelif False:\n  x = 2\nelse:\n  x = 3\n");

        var statement = Assert.Single(module.Statements);
        var ifStatement = Assert.IsType<IfStatement>(statement);
        Assert.Equal(2, ifStatement.Clauses.Count);
        Assert.Single(ifStatement.ElseStatements);
    }

    [Fact]
    public void ParsesForStatement()
    {
        var module = StarlarkModuleParser.ParseModule("for x in [1, 2]:\n  x\n");

        var statement = Assert.Single(module.Statements);
        var forStatement = Assert.IsType<ForStatement>(statement);
        Assert.Equal("x", forStatement.Name);
    }

    [Fact]
    public void ParsesFunctionDefinition()
    {
        var module = StarlarkModuleParser.ParseModule("def add(a, b):\n  return a + b\n");

        var statement = Assert.Single(module.Statements);
        var function = Assert.IsType<FunctionDefinitionStatement>(statement);
        Assert.Equal("add", function.Name);
        Assert.Equal(new[] { "a", "b" }, function.Parameters);
        Assert.Single(function.Body);
    }
}
