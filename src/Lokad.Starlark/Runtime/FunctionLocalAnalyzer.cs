using System.Collections.Generic;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

internal static class FunctionLocalAnalyzer
{
    internal static IReadOnlySet<string> CollectLocals(
        IReadOnlyList<FunctionParameter> parameters,
        IReadOnlyList<Statement> statements)
    {
        var locals = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < parameters.Count; i++)
        {
            locals.Add(parameters[i].Name);
        }

        CollectFromStatements(statements, locals);
        return locals;
    }

    private static void CollectFromStatements(IReadOnlyList<Statement> statements, HashSet<string> locals)
    {
        for (var i = 0; i < statements.Count; i++)
        {
            var statement = statements[i];
            switch (statement)
            {
                case AssignmentStatement assignment:
                    CollectFromTarget(assignment.Target, locals);
                    break;
                case AugmentedAssignmentStatement assignment:
                    CollectFromTarget(assignment.Target, locals);
                    break;
                case ForStatement forStatement:
                    CollectFromTarget(forStatement.Target, locals);
                    CollectFromStatements(forStatement.Body, locals);
                    break;
                case IfStatement ifStatement:
                    foreach (var clause in ifStatement.Clauses)
                    {
                        CollectFromStatements(clause.Statements, locals);
                    }
                    CollectFromStatements(ifStatement.ElseStatements, locals);
                    break;
                case FunctionDefinitionStatement functionDefinition:
                    locals.Add(functionDefinition.Name);
                    break;
            }
        }
    }

    private static void CollectFromTarget(AssignmentTarget target, HashSet<string> locals)
    {
        switch (target)
        {
            case NameTarget nameTarget:
                locals.Add(nameTarget.Name);
                break;
            case TupleTarget tupleTarget:
                foreach (var item in tupleTarget.Items)
                {
                    CollectFromTarget(item, locals);
                }
                break;
            case ListTarget listTarget:
                foreach (var item in listTarget.Items)
                {
                    CollectFromTarget(item, locals);
                }
                break;
        }
    }
}
