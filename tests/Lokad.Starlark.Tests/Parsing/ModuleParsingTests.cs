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
        var target = Assert.IsType<NameTarget>(assignment.Target);
        Assert.Equal("x", target.Name);
        Assert.Equal(new LiteralExpression(1L), assignment.Value);
    }

    [Fact]
    public void ParsesTupleAssignment()
    {
        var module = StarlarkModuleParser.ParseModule("a, b = 1, 2\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(statement);
        var target = Assert.IsType<TupleTarget>(assignment.Target);
        Assert.Equal(2, target.Items.Count);
    }

    [Fact]
    public void ParsesIndexAssignment()
    {
        var module = StarlarkModuleParser.ParseModule("items[0] = 1\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(statement);
        Assert.IsType<IndexTarget>(assignment.Target);
    }

    [Fact]
    public void ParsesAugmentedAssignment()
    {
        var module = StarlarkModuleParser.ParseModule("x += 1\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AugmentedAssignmentStatement>(statement);
        Assert.Equal(BinaryOperator.Add, assignment.Operator);
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
        var target = Assert.IsType<NameTarget>(forStatement.Target);
        Assert.Equal("x", target.Name);
    }

    [Fact]
    public void ParsesTupleForTarget()
    {
        var module = StarlarkModuleParser.ParseModule("for x, y in [(1, 2)]:\n  x\n");

        var statement = Assert.Single(module.Statements);
        var forStatement = Assert.IsType<ForStatement>(statement);
        var target = Assert.IsType<TupleTarget>(forStatement.Target);
        Assert.Equal(2, target.Items.Count);
    }

    [Fact]
    public void RejectsInvalidForTargets()
    {
        Assert.Throws<StarlarkParseException>(
            () => StarlarkModuleParser.ParseModule("for 1 in [1]:\n  pass\n"));
        Assert.Throws<StarlarkParseException>(
            () => StarlarkModuleParser.ParseModule("for x.y in [1]:\n  pass\n"));
        Assert.Throws<StarlarkParseException>(
            () => StarlarkModuleParser.ParseModule("for (x) in [1]:\n  pass\n"));
        Assert.Throws<StarlarkParseException>(
            () => StarlarkModuleParser.ParseModule("for x[1:] in [1]:\n  pass\n"));
        Assert.Throws<StarlarkParseException>(
            () => StarlarkModuleParser.ParseModule("for f() in [1]:\n  pass\n"));
    }

    [Fact]
    public void ParsesFunctionDefinition()
    {
        var module = StarlarkModuleParser.ParseModule("def add(a, b):\n  return a + b\n");

        var statement = Assert.Single(module.Statements);
        var function = Assert.IsType<FunctionDefinitionStatement>(statement);
        Assert.Equal("add", function.Name);
        Assert.Collection(
            function.Parameters,
            parameter =>
            {
                Assert.Equal("a", parameter.Name);
                Assert.Null(parameter.Default);
                Assert.Equal(ParameterKind.Normal, parameter.Kind);
            },
            parameter =>
            {
                Assert.Equal("b", parameter.Name);
                Assert.Null(parameter.Default);
                Assert.Equal(ParameterKind.Normal, parameter.Kind);
            });
        Assert.Single(function.Body);
    }

    [Fact]
    public void ParsesVariadicFunctionDefinition()
    {
        var module = StarlarkModuleParser.ParseModule("def f(*args, **kwargs):\n  return (args, kwargs)\n");

        var statement = Assert.Single(module.Statements);
        var function = Assert.IsType<FunctionDefinitionStatement>(statement);
        Assert.Collection(
            function.Parameters,
            parameter =>
            {
                Assert.Equal("args", parameter.Name);
                Assert.Equal(ParameterKind.VarArgs, parameter.Kind);
            },
            parameter =>
            {
                Assert.Equal("kwargs", parameter.Name);
                Assert.Equal(ParameterKind.KwArgs, parameter.Kind);
            });
    }

    [Fact]
    public void ParsesListComprehension()
    {
        var module = StarlarkModuleParser.ParseModule("items = [x for x in [1, 2]]\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(statement);
        var comprehension = Assert.IsType<ListComprehensionExpression>(assignment.Value);
        Assert.Single(comprehension.Clauses);
        var clause = comprehension.Clauses[0];
        Assert.Equal(ComprehensionClauseKind.For, clause.Kind);
        Assert.IsType<NameTarget>(clause.Target);
    }

    [Fact]
    public void ParsesDictComprehension()
    {
        var module = StarlarkModuleParser.ParseModule("items = {x: x for x in [1, 2]}\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(statement);
        var comprehension = Assert.IsType<DictComprehensionExpression>(assignment.Value);
        Assert.Single(comprehension.Clauses);
    }

    [Fact]
    public void ParsesLoadStatement()
    {
        var module = StarlarkModuleParser.ParseModule("load(\"math\", \"sin\", cosine=\"cos\")\n");

        var statement = Assert.Single(module.Statements);
        var load = Assert.IsType<LoadStatement>(statement);
        Assert.Equal("math", load.Module);
        Assert.Collection(
            load.Bindings,
            binding =>
            {
                Assert.Equal("sin", binding.Name);
                Assert.Equal("sin", binding.Alias);
            },
            binding =>
            {
                Assert.Equal("cos", binding.Name);
                Assert.Equal("cosine", binding.Alias);
            });
    }
}
