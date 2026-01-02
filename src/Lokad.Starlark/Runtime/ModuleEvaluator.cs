using System.Collections.Generic;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

public sealed class ModuleEvaluator
{
    private readonly ExpressionEvaluator _expressionEvaluator = new ExpressionEvaluator();

    public StarlarkValue? ExecuteModule(StarlarkModule module, StarlarkEnvironment environment)
    {
        try
        {
            return ExecuteStatements(module.Statements, environment);
        }
        catch (ReturnSignal)
        {
            throw new InvalidOperationException("Return statement is not allowed at module scope.");
        }
        catch (BreakSignal)
        {
            throw new InvalidOperationException("Break statement is not allowed at module scope.");
        }
        catch (ContinueSignal)
        {
            throw new InvalidOperationException("Continue statement is not allowed at module scope.");
        }
    }

    public StarlarkValue ExecuteFunctionBody(IReadOnlyList<Statement> statements, StarlarkEnvironment environment)
    {
        try
        {
            ExecuteStatements(statements, environment);
            return StarlarkNone.Instance;
        }
        catch (ReturnSignal signal)
        {
            return signal.Value;
        }
        catch (BreakSignal)
        {
            throw new InvalidOperationException("Break statement is only valid inside loops.");
        }
        catch (ContinueSignal)
        {
            throw new InvalidOperationException("Continue statement is only valid inside loops.");
        }
    }

    private StarlarkValue? ExecuteStatements(IReadOnlyList<Statement> statements, StarlarkEnvironment environment)
    {
        StarlarkValue? lastValue = null;

        foreach (var statement in statements)
        {
            switch (statement)
            {
                case AssignmentStatement assignment:
                    var value = _expressionEvaluator.Evaluate(assignment.Value, environment);
                    environment.Set(assignment.Name, value);
                    lastValue = null;
                    break;
                case ExpressionStatement expressionStatement:
                    lastValue = _expressionEvaluator.Evaluate(expressionStatement.Expression, environment);
                    break;
                case IfStatement ifStatement:
                    lastValue = ExecuteIfStatement(ifStatement, environment);
                    break;
                case ForStatement forStatement:
                    lastValue = ExecuteForStatement(forStatement, environment);
                    break;
                case FunctionDefinitionStatement functionDefinition:
                    var function = new StarlarkUserFunction(
                        functionDefinition.Name,
                        functionDefinition.Parameters,
                        functionDefinition.Body,
                        environment);
                    environment.Set(functionDefinition.Name, function);
                    lastValue = null;
                    break;
                case ReturnStatement returnStatement:
                    var returnValue = returnStatement.Value == null
                        ? StarlarkNone.Instance
                        : _expressionEvaluator.Evaluate(returnStatement.Value, environment);
                    throw new ReturnSignal(returnValue);
                case BreakStatement:
                    throw new BreakSignal();
                case ContinueStatement:
                    throw new ContinueSignal();
                case PassStatement:
                    lastValue = null;
                    break;
                case LoadStatement loadStatement:
                    ExecuteLoadStatement(loadStatement, environment);
                    lastValue = null;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported statement type '{statement.GetType().Name}'.");
            }
        }

        return lastValue;
    }

    private StarlarkValue? ExecuteIfStatement(IfStatement ifStatement, StarlarkEnvironment environment)
    {
        for (var i = 0; i < ifStatement.Clauses.Count; i++)
        {
            var clause = ifStatement.Clauses[i];
            var condition = _expressionEvaluator.Evaluate(clause.Condition, environment);
            if (condition.IsTruthy)
            {
                return ExecuteStatements(clause.Statements, environment);
            }
        }

        return ExecuteStatements(ifStatement.ElseStatements, environment);
    }

    private StarlarkValue? ExecuteForStatement(ForStatement forStatement, StarlarkEnvironment environment)
    {
        var iterable = _expressionEvaluator.Evaluate(forStatement.Iterable, environment);

        foreach (var item in Enumerate(iterable))
        {
            environment.Set(forStatement.Name, item);
            try
            {
                ExecuteStatements(forStatement.Body, environment);
            }
            catch (ContinueSignal)
            {
                continue;
            }
            catch (BreakSignal)
            {
                break;
            }
        }

        return null;
    }

    private static void ExecuteLoadStatement(LoadStatement loadStatement, StarlarkEnvironment environment)
    {
        if (!environment.Modules.TryGetValue(loadStatement.Module, out var module))
        {
            throw new KeyNotFoundException($"Module '{loadStatement.Module}' not found.");
        }

        foreach (var binding in loadStatement.Bindings)
        {
            if (!module.TryGetValue(binding.Name, out var value))
            {
                throw new KeyNotFoundException(
                    $"Symbol '{binding.Name}' not found in module '{loadStatement.Module}'.");
            }

            environment.Set(binding.Alias, value);
        }
    }

    private static IEnumerable<StarlarkValue> Enumerate(StarlarkValue iterable)
    {
        switch (iterable)
        {
            case StarlarkList list:
                foreach (var item in list.Items)
                {
                    yield return item;
                }
                yield break;
            case StarlarkTuple tuple:
                foreach (var item in tuple.Items)
                {
                    yield return item;
                }
                yield break;
            case StarlarkString text:
                foreach (var ch in text.Value)
                {
                    yield return new StarlarkString(ch.ToString());
                }
                yield break;
            case StarlarkDict dict:
                foreach (var entry in dict.Entries)
                {
                    yield return entry.Key;
                }
                yield break;
            default:
                throw new InvalidOperationException(
                    $"Type '{iterable.TypeName}' is not iterable.");
        }
    }

    private sealed class ReturnSignal : Exception
    {
        public ReturnSignal(StarlarkValue value)
        {
            Value = value;
        }

        public StarlarkValue Value { get; }
    }

    private sealed class BreakSignal : Exception;

    private sealed class ContinueSignal : Exception;
}
