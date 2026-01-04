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
        var assignment = Assert.IsType<AssignmentStatement>(SyntaxNormalization.Normalize(statement));
        var target = Assert.IsType<NameTarget>(assignment.Target);
        Assert.Equal("x", target.Name);
        Assert.Equal(Lit(1L), assignment.Value);
    }

    [Fact]
    public void ParsesSemicolonSeparatedStatements()
    {
        var module = StarlarkModuleParser.ParseModule("x = 1; y = 2\n");

        Assert.Equal(2, module.Statements.Count);
    }

    [Fact]
    public void ParsesTrailingSemicolonStatement()
    {
        var module = StarlarkModuleParser.ParseModule("x = 1; y = 2;\n");

        Assert.Equal(2, module.Statements.Count);
        Assert.IsType<AssignmentStatement>(SyntaxNormalization.Normalize(module.Statements[0]));
        Assert.IsType<AssignmentStatement>(SyntaxNormalization.Normalize(module.Statements[1]));
    }

    [Fact]
    public void ParsesTupleAssignment()
    {
        var module = StarlarkModuleParser.ParseModule("a, b = 1, 2\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(SyntaxNormalization.Normalize(statement));
        var target = Assert.IsType<TupleTarget>(assignment.Target);
        Assert.Equal(2, target.Items.Count);
    }

    [Fact]
    public void ParsesIndexAssignment()
    {
        var module = StarlarkModuleParser.ParseModule("items[0] = 1\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(SyntaxNormalization.Normalize(statement));
        Assert.IsType<IndexTarget>(assignment.Target);
    }

    [Fact]
    public void ParsesAugmentedAssignment()
    {
        var module = StarlarkModuleParser.ParseModule("x += 1\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AugmentedAssignmentStatement>(SyntaxNormalization.Normalize(statement));
        Assert.Equal(BinaryOperator.Add, assignment.Operator);
    }

    [Fact]
    public void ParsesExpressionStatement()
    {
        var module = StarlarkModuleParser.ParseModule("1 + 2\n");

        var statement = Assert.Single(module.Statements);
        var expressionStatement = Assert.IsType<ExpressionStatement>(SyntaxNormalization.Normalize(statement));
        Assert.Equal(
            Bin(Lit(1L), BinaryOperator.Add, Lit(2L)),
            expressionStatement.Expression);
    }

    [Fact]
    public void ParsesInlineIfStatement()
    {
        var module = StarlarkModuleParser.ParseModule("if True: x = 1\n");

        var statement = Assert.Single(module.Statements);
        var ifStatement = Assert.IsType<IfStatement>(SyntaxNormalization.Normalize(statement));
        var clause = Assert.Single(ifStatement.Clauses);
        Assert.Equal(Lit(true), clause.Condition);
        Assert.Single(clause.Statements);
        Assert.Empty(ifStatement.ElseStatements);
    }

    [Fact]
    public void ParsesIndentedIfStatement()
    {
        var module = StarlarkModuleParser.ParseModule("if True:\n  x = 1\n");

        var statement = Assert.Single(module.Statements);
        var ifStatement = Assert.IsType<IfStatement>(SyntaxNormalization.Normalize(statement));
        var clause = Assert.Single(ifStatement.Clauses);
        Assert.Equal(Lit(true), clause.Condition);
        Assert.Single(clause.Statements);
        Assert.Empty(ifStatement.ElseStatements);
    }

    [Fact]
    public void ParsesElifChain()
    {
        var module = StarlarkModuleParser.ParseModule("if True:\n  x = 1\nelif False:\n  x = 2\nelse:\n  x = 3\n");

        var statement = Assert.Single(module.Statements);
        var ifStatement = Assert.IsType<IfStatement>(SyntaxNormalization.Normalize(statement));
        Assert.Equal(2, ifStatement.Clauses.Count);
        Assert.Single(ifStatement.ElseStatements);
    }

    [Fact]
    public void ParsesForStatement()
    {
        var module = StarlarkModuleParser.ParseModule("for x in [1, 2]:\n  x\n");

        var statement = Assert.Single(module.Statements);
        var forStatement = Assert.IsType<ForStatement>(SyntaxNormalization.Normalize(statement));
        var target = Assert.IsType<NameTarget>(forStatement.Target);
        Assert.Equal("x", target.Name);
    }

    [Fact]
    public void ParsesTupleForTarget()
    {
        var module = StarlarkModuleParser.ParseModule("for x, y in [(1, 2)]:\n  x\n");

        var statement = Assert.Single(module.Statements);
        var forStatement = Assert.IsType<ForStatement>(SyntaxNormalization.Normalize(statement));
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
        var function = Assert.IsType<FunctionDefinitionStatement>(SyntaxNormalization.Normalize(statement));
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
        var function = Assert.IsType<FunctionDefinitionStatement>(SyntaxNormalization.Normalize(statement));
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
    public void ParsesFunctionDefinitionWithTrailingComma()
    {
        var module = StarlarkModuleParser.ParseModule("def add(a, b,):\n  return a + b\n");

        var statement = Assert.Single(module.Statements);
        var function = Assert.IsType<FunctionDefinitionStatement>(SyntaxNormalization.Normalize(statement));
        Assert.Equal(2, function.Parameters.Count);
    }

    [Fact]
    public void ParsesListComprehension()
    {
        var module = StarlarkModuleParser.ParseModule("items = [x for x in [1, 2]]\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(SyntaxNormalization.Normalize(statement));
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
        var assignment = Assert.IsType<AssignmentStatement>(SyntaxNormalization.Normalize(statement));
        var comprehension = Assert.IsType<DictComprehensionExpression>(assignment.Value);
        Assert.Single(comprehension.Clauses);
    }

    [Fact]
    public void ParsesLoadStatementWithTrailingComma()
    {
        var module = StarlarkModuleParser.ParseModule("load(\"math\", \"sin\",)\n");

        var statement = Assert.Single(module.Statements);
        var load = Assert.IsType<LoadStatement>(SyntaxNormalization.Normalize(statement));
        Assert.Equal("math", load.Module);
        var binding = Assert.Single(load.Bindings);
        Assert.Equal("sin", binding.Name);
    }

    [Fact]
    public void ParsesIndentedBracketedList()
    {
        var module = StarlarkModuleParser.ParseModule("values = [\n  1,\n  2,\n]\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(SyntaxNormalization.Normalize(statement));
        var list = Assert.IsType<ListExpression>(assignment.Value);
        Assert.Equal(2, list.Items.Count);
    }

    [Fact]
    public void ParsesIndentedCallArguments()
    {
        var module = StarlarkModuleParser.ParseModule("result = f(\n  1,\n  2,\n)\n");

        var statement = Assert.Single(module.Statements);
        var assignment = Assert.IsType<AssignmentStatement>(SyntaxNormalization.Normalize(statement));
        var call = Assert.IsType<CallExpression>(assignment.Value);
        Assert.Equal(2, call.Arguments.Count);
    }

    [Fact]
    public void ParsesLoadStatement()
    {
        var module = StarlarkModuleParser.ParseModule("load(\"math\", \"sin\", cosine=\"cos\")\n");

        var statement = Assert.Single(module.Statements);
        var load = Assert.IsType<LoadStatement>(SyntaxNormalization.Normalize(statement));
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

    private static LiteralExpression Lit(object value) => new LiteralExpression(value, default);

    private static BinaryExpression Bin(Expression left, BinaryOperator op, Expression right) =>
        new BinaryExpression(left, op, right, default);
}
