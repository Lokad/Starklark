using System.Collections.Generic;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

internal static class ModuleGlobalAnalyzer
{
    internal static IReadOnlySet<string> CollectGlobals(IReadOnlyList<Statement> statements)
    {
        var globals = new HashSet<string>(StringComparer.Ordinal);
        CollectFromStatements(statements, globals);
        return globals;
    }

    private static void CollectFromStatements(IReadOnlyList<Statement> statements, HashSet<string> globals)
    {
        for (var i = 0; i < statements.Count; i++)
        {
            var statement = statements[i];
            switch (statement)
            {
                case AssignmentStatement assignment:
                    CollectFromTarget(assignment.Target, globals);
                    break;
                case AugmentedAssignmentStatement assignment:
                    CollectFromTarget(assignment.Target, globals);
                    break;
                case ForStatement forStatement:
                    CollectFromTarget(forStatement.Target, globals);
                    CollectFromStatements(forStatement.Body, globals);
                    break;
                case IfStatement ifStatement:
                    foreach (var clause in ifStatement.Clauses)
                    {
                        CollectFromStatements(clause.Statements, globals);
                    }
                    CollectFromStatements(ifStatement.ElseStatements, globals);
                    break;
                case FunctionDefinitionStatement functionDefinition:
                    globals.Add(functionDefinition.Name);
                    break;
                case LoadStatement loadStatement:
                    foreach (var binding in loadStatement.Bindings)
                    {
                        globals.Add(binding.Alias);
                    }
                    break;
            }
        }
    }

    private static void CollectFromTarget(AssignmentTarget target, HashSet<string> globals)
    {
        switch (target)
        {
            case NameTarget nameTarget:
                globals.Add(nameTarget.Name);
                break;
            case TupleTarget tupleTarget:
                foreach (var item in tupleTarget.Items)
                {
                    CollectFromTarget(item, globals);
                }
                break;
            case ListTarget listTarget:
                foreach (var item in listTarget.Items)
                {
                    CollectFromTarget(item, globals);
                }
                break;
        }
    }
}
