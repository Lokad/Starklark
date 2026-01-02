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
}
