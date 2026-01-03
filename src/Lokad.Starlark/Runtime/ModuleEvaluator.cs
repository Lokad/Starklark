using System.Collections.Generic;
using System.Linq;
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
                    AssignTarget(assignment.Target, value, environment);
                    lastValue = null;
                    break;
                case AugmentedAssignmentStatement augmentedAssignment:
                    ExecuteAugmentedAssignment(augmentedAssignment, environment);
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
                    var (names, defaults, varArgsName, kwArgsName) = EvaluateDefaults(functionDefinition, environment);
                    var function = new StarlarkUserFunction(
                        functionDefinition.Name,
                        names,
                        defaults,
                        varArgsName,
                        kwArgsName,
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
            AssignTarget(forStatement.Target, item, environment);
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

    private static (
        IReadOnlyList<string> Names,
        IReadOnlyList<StarlarkValue?> Defaults,
        string? VarArgsName,
        string? KwArgsName) EvaluateDefaults(
        FunctionDefinitionStatement definition,
        StarlarkEnvironment environment)
    {
        var names = new List<string>(definition.Parameters.Count);
        var defaults = new List<StarlarkValue?>(definition.Parameters.Count);
        var evaluator = new ExpressionEvaluator();
        string? varArgsName = null;
        string? kwArgsName = null;

        for (var i = 0; i < definition.Parameters.Count; i++)
        {
            var parameter = definition.Parameters[i];
            if (parameter.Kind == ParameterKind.VarArgs)
            {
                varArgsName = parameter.Name;
                continue;
            }

            if (parameter.Kind == ParameterKind.KwArgs)
            {
                kwArgsName = parameter.Name;
                continue;
            }

            names.Add(parameter.Name);
            if (parameter.Default != null)
            {
                defaults.Add(evaluator.Evaluate(parameter.Default, environment));
            }
            else
            {
                defaults.Add(null);
            }
        }

        return (names, defaults, varArgsName, kwArgsName);
    }

    private void AssignTarget(AssignmentTarget target, StarlarkValue value, StarlarkEnvironment environment)
    {
        switch (target)
        {
            case NameTarget nameTarget:
                environment.Set(nameTarget.Name, value);
                break;
            case IndexTarget indexTarget:
                AssignIndexTarget(indexTarget, value, environment);
                break;
            case TupleTarget tupleTarget:
                AssignSequenceTargets(tupleTarget.Items, value, environment);
                break;
            case ListTarget listTarget:
                AssignSequenceTargets(listTarget.Items, value, environment);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported assignment target '{target.GetType().Name}'.");
        }
    }

    private void ExecuteAugmentedAssignment(AugmentedAssignmentStatement assignment, StarlarkEnvironment environment)
    {
        switch (assignment.Target)
        {
            case NameTarget nameTarget:
                ExecuteAugmentedNameAssignment(nameTarget, assignment, environment);
                break;
            case IndexTarget indexTarget:
                ExecuteAugmentedIndexAssignment(indexTarget, assignment, environment);
                break;
            default:
                throw new InvalidOperationException(
                    $"Augmented assignment not supported for '{assignment.Target.GetType().Name}'.");
        }
    }

    private void ExecuteAugmentedNameAssignment(
        NameTarget target,
        AugmentedAssignmentStatement assignment,
        StarlarkEnvironment environment)
    {
        if (!environment.TryGet(target.Name, out var existing))
        {
            throw new KeyNotFoundException($"Undefined identifier '{target.Name}'.");
        }

        var right = _expressionEvaluator.Evaluate(assignment.Value, environment);
        if (existing is StarlarkList list && assignment.Operator == BinaryOperator.Add)
        {
            AppendList(list, right);
            environment.Set(target.Name, list);
            return;
        }

        var result = ApplyBinaryOperator(assignment.Operator, existing, right);
        environment.Set(target.Name, result);
    }

    private void ExecuteAugmentedIndexAssignment(
        IndexTarget target,
        AugmentedAssignmentStatement assignment,
        StarlarkEnvironment environment)
    {
        var container = _expressionEvaluator.Evaluate(target.Target, environment);
        var index = _expressionEvaluator.Evaluate(target.Index, environment);
        var existing = GetIndexedValue(container, index);
        var right = _expressionEvaluator.Evaluate(assignment.Value, environment);
        var result = ApplyBinaryOperator(assignment.Operator, existing, right);

        switch (container)
        {
            case StarlarkList list:
                AssignListIndex(list, index, result);
                break;
            case StarlarkDict dict:
                AssignDictIndex(dict, index, result);
                break;
            default:
                throw new InvalidOperationException(
                    $"Index assignment not supported for '{container.TypeName}'.");
        }
    }

    private static void AppendList(StarlarkList list, StarlarkValue value)
    {
        switch (value)
        {
            case StarlarkList rightList:
                list.Items.AddRange(rightList.Items);
                if (rightList.Items.Count > 0)
                {
                    list.MarkMutated();
                }
                break;
            case StarlarkTuple rightTuple:
                list.Items.AddRange(rightTuple.Items);
                if (rightTuple.Items.Count > 0)
                {
                    list.MarkMutated();
                }
                break;
            default:
                throw new InvalidOperationException(
                    $"Operator '+=' not supported for '{list.TypeName}' and '{value.TypeName}'.");
        }
    }

    private static StarlarkValue GetIndexedValue(StarlarkValue container, StarlarkValue index)
    {
        return container switch
        {
            StarlarkList list => IndexList(list, index),
            StarlarkTuple tuple => IndexTuple(tuple, index),
            StarlarkString text => IndexString(text, index),
            StarlarkDict dict => IndexDict(dict, index),
            _ => throw new InvalidOperationException(
                $"Indexing not supported for '{container.TypeName}'.")
        };
    }

    private static StarlarkValue IndexList(StarlarkList list, StarlarkValue index)
    {
        var position = RequireIndex(index);
        var resolved = ResolveIndex(position, list.Items.Count);
        return list.Items[resolved];
    }

    private static StarlarkValue IndexTuple(StarlarkTuple tuple, StarlarkValue index)
    {
        var position = RequireIndex(index);
        var resolved = ResolveIndex(position, tuple.Items.Count);
        return tuple.Items[resolved];
    }

    private static StarlarkValue IndexString(StarlarkString text, StarlarkValue index)
    {
        var position = RequireIndex(index);
        var resolved = ResolveIndex(position, text.Value.Length);
        return new StarlarkString(text.Value[resolved].ToString());
    }

    private static StarlarkValue IndexDict(StarlarkDict dict, StarlarkValue key)
    {
        StarlarkHash.EnsureHashable(key);
        foreach (var entry in dict.Entries)
        {
            if (Equals(entry.Key, key))
            {
                return entry.Value;
            }
        }

        throw new KeyNotFoundException("Key not found in dict.");
    }

    private static int RequireIndex(StarlarkValue index)
    {
        if (index is StarlarkInt intValue)
        {
            return checked((int)intValue.Value);
        }

        throw new InvalidOperationException(
            $"Index must be an int, got '{index.TypeName}'.");
    }

    private static int ResolveIndex(int position, int length)
    {
        var resolved = position < 0 ? length + position : position;
        if (resolved < 0 || resolved >= length)
        {
            throw new IndexOutOfRangeException("Index out of range.");
        }

        return resolved;
    }

    private static StarlarkValue ApplyBinaryOperator(
        BinaryOperator op,
        StarlarkValue left,
        StarlarkValue right)
    {
        return op switch
        {
            BinaryOperator.Add => Add(left, right),
            BinaryOperator.Subtract => Subtract(left, right),
            BinaryOperator.Multiply => Multiply(left, right),
            BinaryOperator.Divide => Divide(left, right),
            BinaryOperator.FloorDivide => FloorDivide(left, right),
            BinaryOperator.Modulo => Modulo(left, right),
            _ => throw new InvalidOperationException(
                $"Operator '{op}' not supported for augmented assignment.")
        };
    }

    private static StarlarkValue Add(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkString leftString && right is StarlarkString rightString)
        {
            return new StarlarkString(leftString.Value + rightString.Value);
        }

        if (left is StarlarkList leftList && right is StarlarkList rightList)
        {
            var items = new List<StarlarkValue>(leftList.Items.Count + rightList.Items.Count);
            items.AddRange(leftList.Items);
            items.AddRange(rightList.Items);
            return new StarlarkList(items);
        }

        if (left is StarlarkTuple leftTuple && right is StarlarkTuple rightTuple)
        {
            return new StarlarkTuple(leftTuple.Items.Concat(rightTuple.Items).ToArray());
        }

        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value + rightInt.Value);
        }

        if (TryGetNumber(left, out var leftNumber, out var leftIsInt)
            && TryGetNumber(right, out var rightNumber, out var rightIsInt))
        {
            return leftIsInt && rightIsInt
                ? new StarlarkInt((long)(leftNumber + rightNumber))
                : new StarlarkFloat(leftNumber + rightNumber);
        }

        throw new InvalidOperationException(
            $"Operator '+' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue Subtract(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value - rightInt.Value);
        }

        if (TryGetNumber(left, out var leftNumber, out var leftIsInt)
            && TryGetNumber(right, out var rightNumber, out var rightIsInt))
        {
            return leftIsInt && rightIsInt
                ? new StarlarkInt((long)(leftNumber - rightNumber))
                : new StarlarkFloat(leftNumber - rightNumber);
        }

        throw new InvalidOperationException(
            $"Operator '-' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue Multiply(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value * rightInt.Value);
        }

        if (left is StarlarkString leftString && right is StarlarkInt rightCount)
        {
            return new StarlarkString(RepeatString(leftString.Value, rightCount.Value));
        }

        if (left is StarlarkInt leftCount && right is StarlarkString rightString)
        {
            return new StarlarkString(RepeatString(rightString.Value, leftCount.Value));
        }

        if (left is StarlarkList leftList && right is StarlarkInt listCount)
        {
            return new StarlarkList(RepeatList(leftList.Items, listCount.Value));
        }

        if (left is StarlarkInt listCountLeft && right is StarlarkList rightList)
        {
            return new StarlarkList(RepeatList(rightList.Items, listCountLeft.Value));
        }

        if (left is StarlarkTuple leftTuple && right is StarlarkInt tupleCount)
        {
            return new StarlarkTuple(RepeatList(leftTuple.Items, tupleCount.Value));
        }

        if (left is StarlarkInt tupleCountLeft && right is StarlarkTuple rightTuple)
        {
            return new StarlarkTuple(RepeatList(rightTuple.Items, tupleCountLeft.Value));
        }

        if (TryGetNumber(left, out var leftNumber, out var leftIsInt)
            && TryGetNumber(right, out var rightNumber, out var rightIsInt))
        {
            return leftIsInt && rightIsInt
                ? new StarlarkInt((long)(leftNumber * rightNumber))
                : new StarlarkFloat(leftNumber * rightNumber);
        }

        throw new InvalidOperationException(
            $"Operator '*' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue Divide(StarlarkValue left, StarlarkValue right)
    {
        if (TryGetNumber(left, out var leftNumber, out _)
            && TryGetNumber(right, out var rightNumber, out _))
        {
            return new StarlarkFloat(leftNumber / rightNumber);
        }

        throw new InvalidOperationException(
            $"Operator '/' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue FloorDivide(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            if (rightInt.Value == 0)
            {
                throw new DivideByZeroException("Division by zero.");
            }

            var quotient = leftInt.Value / rightInt.Value;
            var remainder = leftInt.Value % rightInt.Value;
            if (remainder != 0 && ((leftInt.Value < 0) ^ (rightInt.Value < 0)))
            {
                quotient -= 1;
            }

            return new StarlarkInt(quotient);
        }

        if (TryGetNumber(left, out var leftNumber, out _)
            && TryGetNumber(right, out var rightNumber, out _))
        {
            if (rightNumber == 0)
            {
                throw new DivideByZeroException("Division by zero.");
            }

            return new StarlarkFloat(Math.Floor(leftNumber / rightNumber));
        }

        throw new InvalidOperationException(
            $"Operator '//' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue Modulo(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkString leftString)
        {
            return new StarlarkString(StarlarkFormatting.FormatPercent(leftString.Value, right));
        }

        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            if (rightInt.Value == 0)
            {
                throw new DivideByZeroException("Division by zero.");
            }

            var quotient = leftInt.Value / rightInt.Value;
            var remainder = leftInt.Value % rightInt.Value;
            if (remainder != 0 && ((leftInt.Value < 0) ^ (rightInt.Value < 0)))
            {
                quotient -= 1;
            }

            var result = leftInt.Value - quotient * rightInt.Value;
            return new StarlarkInt(result);
        }

        if (TryGetNumber(left, out var leftNumber, out _)
            && TryGetNumber(right, out var rightNumber, out _))
        {
            if (rightNumber == 0)
            {
                throw new DivideByZeroException("Division by zero.");
            }

            var quotient = Math.Floor(leftNumber / rightNumber);
            return new StarlarkFloat(leftNumber - quotient * rightNumber);
        }

        throw new InvalidOperationException(
            $"Operator '%' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static bool TryGetNumber(StarlarkValue value, out double number, out bool isInt)
    {
        switch (value)
        {
            case StarlarkInt intValue:
                number = intValue.Value;
                isInt = true;
                return true;
            case StarlarkFloat floatValue:
                number = floatValue.Value;
                isInt = false;
                return true;
            default:
                number = 0;
                isInt = false;
                return false;
        }
    }

    private static string RepeatString(string value, long count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        if (count > int.MaxValue)
        {
            throw new InvalidOperationException("Repeat count is too large.");
        }

        var builder = new System.Text.StringBuilder(value.Length * (int)count);
        for (var i = 0; i < count; i++)
        {
            builder.Append(value);
        }

        return builder.ToString();
    }

    private static List<StarlarkValue> RepeatList(IReadOnlyList<StarlarkValue> items, long count)
    {
        if (count <= 0)
        {
            return new List<StarlarkValue>();
        }

        if (count > int.MaxValue)
        {
            throw new InvalidOperationException("Repeat count is too large.");
        }

        var total = checked(items.Count * (int)count);
        var result = new List<StarlarkValue>(total);
        for (var i = 0; i < count; i++)
        {
            result.AddRange(items);
        }

        return result;
    }


    private void AssignIndexTarget(IndexTarget target, StarlarkValue value, StarlarkEnvironment environment)
    {
        var container = _expressionEvaluator.Evaluate(target.Target, environment);
        var index = _expressionEvaluator.Evaluate(target.Index, environment);

        switch (container)
        {
            case StarlarkList list:
                AssignListIndex(list, index, value);
                break;
            case StarlarkDict dict:
                AssignDictIndex(dict, index, value);
                break;
            default:
                throw new InvalidOperationException(
                    $"Index assignment not supported for '{container.TypeName}'.");
        }
    }

    private void AssignSequenceTargets(
        IReadOnlyList<AssignmentTarget> targets,
        StarlarkValue value,
        StarlarkEnvironment environment)
    {
        var items = ExtractSequenceItems(value);
        if (items.Count != targets.Count)
        {
            throw new InvalidOperationException(
                $"Assignment length mismatch. Expected {targets.Count} values but got {items.Count}.");
        }

        for (var i = 0; i < targets.Count; i++)
        {
            AssignTarget(targets[i], items[i], environment);
        }
    }

    private static IReadOnlyList<StarlarkValue> ExtractSequenceItems(StarlarkValue value)
    {
        return value switch
        {
            StarlarkList list => list.Items,
            StarlarkTuple tuple => tuple.Items,
            _ => throw new InvalidOperationException(
                $"Value of type '{value.TypeName}' is not iterable for assignment.")
        };
    }

    private static void AssignListIndex(StarlarkList list, StarlarkValue index, StarlarkValue value)
    {
        if (index is not StarlarkInt intIndex)
        {
            throw new InvalidOperationException(
                $"Index must be an int, got '{index.TypeName}'.");
        }

        var position = checked((int)intIndex.Value);
        if (position < 0)
        {
            position = list.Items.Count + position;
        }

        if (position < 0 || position >= list.Items.Count)
        {
            throw new IndexOutOfRangeException("Index out of range.");
        }

        list.Items[position] = value;
        list.MarkMutated();
    }

    private static void AssignDictIndex(StarlarkDict dict, StarlarkValue key, StarlarkValue value)
    {
        StarlarkHash.EnsureHashable(key);
        for (var i = 0; i < dict.Entries.Count; i++)
        {
            var entry = dict.Entries[i];
            if (Equals(entry.Key, key))
            {
                dict.Entries[i] = new KeyValuePair<StarlarkValue, StarlarkValue>(entry.Key, value);
                dict.MarkMutated();
                return;
            }
        }

        dict.Entries.Add(new KeyValuePair<StarlarkValue, StarlarkValue>(key, value));
        dict.MarkMutated();
    }

    private static IEnumerable<StarlarkValue> Enumerate(StarlarkValue iterable)
    {
        switch (iterable)
        {
            case StarlarkList list:
                foreach (var item in list.EnumerateWithMutationCheck())
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
            case StarlarkDict dict:
                foreach (var key in dict.EnumerateWithMutationCheck())
                {
                    yield return key;
                }
                yield break;
            case StarlarkStringElems elems:
                foreach (var item in elems.Enumerate())
                {
                    yield return item;
                }
                yield break;
            case StarlarkBytesElems elems:
                foreach (var item in elems.Enumerate())
                {
                    yield return item;
                }
                yield break;
            case StarlarkRange range:
                if (range.Step > 0)
                {
                    for (var i = range.Start; i < range.Stop; i += range.Step)
                    {
                        yield return new StarlarkInt(i);
                    }
                }
                else
                {
                    for (var i = range.Start; i > range.Stop; i += range.Step)
                    {
                        yield return new StarlarkInt(i);
                    }
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
