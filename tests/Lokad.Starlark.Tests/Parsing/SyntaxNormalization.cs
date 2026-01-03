using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Parsing;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Tests.Parsing;

internal static class SyntaxNormalization
{
    public static Expression Normalize(Expression expression)
    {
        return expression switch
        {
            LiteralExpression literal => new LiteralExpression(literal.Value, default),
            IdentifierExpression identifier => new IdentifierExpression(identifier.Name, default),
            UnaryExpression unary => new UnaryExpression(
                unary.Operator,
                Normalize(unary.Operand),
                default),
            BinaryExpression binary => new BinaryExpression(
                Normalize(binary.Left),
                binary.Operator,
                Normalize(binary.Right),
                default),
            CallExpression call => new CallExpression(
                Normalize(call.Callee),
                call.Arguments.Select(Normalize).ToArray(),
                default),
            ListExpression list => new ListExpression(list.Items.Select(Normalize).ToArray(), default),
            TupleExpression tuple => new TupleExpression(tuple.Items.Select(Normalize).ToArray(), default),
            DictExpression dict => new DictExpression(
                dict.Entries.Select(entry => new DictEntry(Normalize(entry.Key), Normalize(entry.Value))).ToArray(),
                default),
            ListComprehensionExpression comprehension => new ListComprehensionExpression(
                Normalize(comprehension.Body),
                comprehension.Clauses.Select(Normalize).ToArray(),
                default),
            DictComprehensionExpression comprehension => new DictComprehensionExpression(
                Normalize(comprehension.Key),
                Normalize(comprehension.Value),
                comprehension.Clauses.Select(Normalize).ToArray(),
                default),
            IndexExpression index => new IndexExpression(
                Normalize(index.Target),
                Normalize(index.Index),
                default),
            AttributeExpression attribute => new AttributeExpression(
                Normalize(attribute.Target),
                attribute.Name,
                default),
            ConditionalExpression conditional => new ConditionalExpression(
                Normalize(conditional.Condition),
                Normalize(conditional.ThenExpression),
                Normalize(conditional.ElseExpression),
                default),
            LambdaExpression lambda => new LambdaExpression(
                lambda.Parameters.Select(Normalize).ToArray(),
                Normalize(lambda.Body),
                default),
            _ => throw new InvalidOperationException(
                $"Unsupported expression type '{expression.GetType().Name}'.")
        };
    }

    public static Statement Normalize(Statement statement)
    {
        return statement switch
        {
            ExpressionStatement expressionStatement => new ExpressionStatement(
                Normalize(expressionStatement.Expression),
                default),
            AssignmentStatement assignment => new AssignmentStatement(
                Normalize(assignment.Target),
                Normalize(assignment.Value),
                default),
            AugmentedAssignmentStatement augmented => new AugmentedAssignmentStatement(
                Normalize(augmented.Target),
                augmented.Operator,
                Normalize(augmented.Value),
                default),
            IfStatement ifStatement => new IfStatement(
                ifStatement.Clauses.Select(Normalize).ToArray(),
                ifStatement.ElseStatements.Select(Normalize).ToArray(),
                default),
            ForStatement forStatement => new ForStatement(
                Normalize(forStatement.Target),
                Normalize(forStatement.Iterable),
                forStatement.Body.Select(Normalize).ToArray(),
                default),
            FunctionDefinitionStatement definition => new FunctionDefinitionStatement(
                definition.Name,
                definition.Parameters.Select(Normalize).ToArray(),
                definition.Body.Select(Normalize).ToArray(),
                default),
            ReturnStatement returnStatement => new ReturnStatement(
                returnStatement.Value == null ? null : Normalize(returnStatement.Value),
                default),
            BreakStatement => new BreakStatement(default),
            ContinueStatement => new ContinueStatement(default),
            PassStatement => new PassStatement(default),
            LoadStatement loadStatement => new LoadStatement(
                loadStatement.Module,
                loadStatement.Bindings,
                default),
            _ => throw new InvalidOperationException(
                $"Unsupported statement type '{statement.GetType().Name}'.")
        };
    }

    public static AssignmentTarget Normalize(AssignmentTarget target)
    {
        return target switch
        {
            NameTarget name => new NameTarget(name.Name, default),
            IndexTarget index => new IndexTarget(
                Normalize(index.Target),
                Normalize(index.Index),
                default),
            TupleTarget tuple => new TupleTarget(tuple.Items.Select(Normalize).ToArray(), default),
            ListTarget list => new ListTarget(list.Items.Select(Normalize).ToArray(), default),
            _ => throw new InvalidOperationException(
                $"Unsupported assignment target type '{target.GetType().Name}'.")
        };
    }

    private static CallArgument Normalize(CallArgument argument)
    {
        return new CallArgument(argument.Kind, argument.Name, Normalize(argument.Value));
    }

    private static ComprehensionClause Normalize(ComprehensionClause clause)
    {
        return new ComprehensionClause(
            clause.Kind,
            clause.Target == null ? null : Normalize(clause.Target),
            clause.Iterable == null ? null : Normalize(clause.Iterable),
            clause.Condition == null ? null : Normalize(clause.Condition),
            default);
    }

    private static IndexSpecifier Normalize(IndexSpecifier index)
    {
        return index switch
        {
            IndexValue value => new IndexValue(Normalize(value.Value), default),
            SliceIndex slice => new SliceIndex(
                slice.Start == null ? null : Normalize(slice.Start),
                slice.Stop == null ? null : Normalize(slice.Stop),
                slice.Step == null ? null : Normalize(slice.Step),
                default),
            _ => throw new InvalidOperationException(
                $"Unsupported index specifier type '{index.GetType().Name}'.")
        };
    }

    private static FunctionParameter Normalize(FunctionParameter parameter)
    {
        return new FunctionParameter(
            parameter.Name,
            parameter.Default == null ? null : Normalize(parameter.Default),
            parameter.Kind);
    }

    private static IfClause Normalize(IfClause clause)
    {
        return new IfClause(
            Normalize(clause.Condition),
            clause.Statements.Select(Normalize).ToArray(),
            default);
    }
}
