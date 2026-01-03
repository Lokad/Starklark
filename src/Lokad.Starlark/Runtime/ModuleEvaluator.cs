using System.Collections.Generic;
using Lokad.Parsing;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

public sealed class ModuleEvaluator
{
    private readonly ExpressionEvaluator _expressionEvaluator = new ExpressionEvaluator();

    public StarlarkValue? ExecuteModule(StarlarkModule module, StarlarkEnvironment environment)
    {
        var result = ExecuteStatements(module.Statements, environment);
        switch (result.Kind)
        {
            case FlowKind.Return:
                RuntimeErrors.Throw("Return statement is not allowed at module scope.", result.Span);
                break;
            case FlowKind.Break:
                RuntimeErrors.Throw("Break statement is not allowed at module scope.", result.Span);
                break;
            case FlowKind.Continue:
                RuntimeErrors.Throw("Continue statement is not allowed at module scope.", result.Span);
                break;
            default:
                return result.LastValue;
        }

        return null;
    }

    public StarlarkValue ExecuteFunctionBody(IReadOnlyList<Statement> statements, StarlarkEnvironment environment)
    {
        var result = ExecuteStatements(statements, environment);
        switch (result.Kind)
        {
            case FlowKind.Return:
                return result.Value ?? StarlarkNone.Instance;
            case FlowKind.Break:
                RuntimeErrors.Throw("Break statement is only valid inside loops.", result.Span);
                break;
            case FlowKind.Continue:
                RuntimeErrors.Throw("Continue statement is only valid inside loops.", result.Span);
                break;
            default:
                return StarlarkNone.Instance;
        }

        return StarlarkNone.Instance;
    }

    private FlowResult ExecuteStatements(IReadOnlyList<Statement> statements, StarlarkEnvironment environment)
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
                    var ifResult = ExecuteIfStatement(ifStatement, environment);
                    if (ifResult.Kind != FlowKind.Normal)
                    {
                        return ifResult;
                    }

                    lastValue = ifResult.LastValue;
                    break;
                case ForStatement forStatement:
                    var forResult = ExecuteForStatement(forStatement, environment);
                    if (forResult.Kind != FlowKind.Normal)
                    {
                        return forResult;
                    }

                    lastValue = forResult.LastValue;
                    break;
                case FunctionDefinitionStatement functionDefinition:
                    var (names, defaults, varArgsName, kwArgsName) =
                        FunctionParameterEvaluator.Evaluate(functionDefinition.Parameters, environment);
                    var locals = FunctionLocalAnalyzer.CollectLocals(
                        functionDefinition.Parameters,
                        functionDefinition.Body);
                    var function = new StarlarkUserFunction(
                        functionDefinition.Name,
                        names,
                        defaults,
                        varArgsName,
                        kwArgsName,
                        functionDefinition.Body,
                        environment,
                        locals);
                    environment.Set(functionDefinition.Name, function);
                    lastValue = null;
                    break;
                case ReturnStatement returnStatement:
                    var returnValue = returnStatement.Value == null
                        ? StarlarkNone.Instance
                        : _expressionEvaluator.Evaluate(returnStatement.Value, environment);
                    return FlowResult.Return(returnValue, returnStatement.Span);
                case BreakStatement:
                    return FlowResult.Break(statement.Span);
                case ContinueStatement:
                    return FlowResult.Continue(statement.Span);
                case PassStatement:
                    lastValue = null;
                    break;
                case LoadStatement loadStatement:
                    ExecuteLoadStatement(loadStatement, environment);
                    lastValue = null;
                    break;
                default:
                    RuntimeErrors.Throw($"Unsupported statement type '{statement.GetType().Name}'.");
                    break;
            }
        }

        return FlowResult.Normal(lastValue);
    }

    private FlowResult ExecuteIfStatement(IfStatement ifStatement, StarlarkEnvironment environment)
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

    private FlowResult ExecuteForStatement(ForStatement forStatement, StarlarkEnvironment environment)
    {
        var iterable = _expressionEvaluator.Evaluate(forStatement.Iterable, environment);

        foreach (var item in Enumerate(iterable, forStatement.Iterable.Span))
        {
            AssignTarget(forStatement.Target, item, environment);
            var bodyResult = ExecuteStatements(forStatement.Body, environment);
            switch (bodyResult.Kind)
            {
                case FlowKind.Continue:
                    continue;
                case FlowKind.Break:
                    return FlowResult.Normal(null);
                case FlowKind.Return:
                    return bodyResult;
            }
        }

        return FlowResult.Normal(null);
    }

    private static void ExecuteLoadStatement(LoadStatement loadStatement, StarlarkEnvironment environment)
    {
        if (environment.Parent != null)
        {
            RuntimeErrors.Throw("load statements may only appear at top level.", loadStatement.Span);
        }

        if (!environment.Modules.TryGetValue(loadStatement.Module, out var module))
        {
            RuntimeErrors.Throw($"Module '{loadStatement.Module}' not found.", loadStatement.Span);
        }

        foreach (var binding in loadStatement.Bindings)
        {
            if (!module.TryGetValue(binding.Name, out var value))
            {
                RuntimeErrors.Throw(
                    $"Symbol '{binding.Name}' not found in module '{loadStatement.Module}'.",
                    loadStatement.Span);
            }

            environment.Set(binding.Alias, value);
        }
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
                RuntimeErrors.Throw(
                    $"Unsupported assignment target '{target.GetType().Name}'.",
                    target.Span);
                break;
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
                RuntimeErrors.Throw(
                    $"Augmented assignment not supported for '{assignment.Target.GetType().Name}'.",
                    assignment.Target.Span);
                break;
        }
    }

    private void ExecuteAugmentedNameAssignment(
        NameTarget target,
        AugmentedAssignmentStatement assignment,
        StarlarkEnvironment environment)
    {
        if (!environment.TryGet(target.Name, out var existing))
        {
            RuntimeErrors.Throw($"Undefined identifier '{target.Name}'.", target.Span);
        }

        var right = _expressionEvaluator.Evaluate(assignment.Value, environment);
        if (existing is StarlarkList list && assignment.Operator == BinaryOperator.Add)
        {
            AppendList(list, right);
            environment.Set(target.Name, list);
            return;
        }

        if (existing is StarlarkDict dict && assignment.Operator == BinaryOperator.BitwiseOr)
        {
            UnionDictInPlace(dict, right);
            environment.Set(target.Name, dict);
            return;
        }

        if (existing is StarlarkSet set)
        {
            switch (assignment.Operator)
            {
                case BinaryOperator.BitwiseOr:
                    UnionSetInPlace(set, right);
                    environment.Set(target.Name, set);
                    return;
                case BinaryOperator.BitwiseAnd:
                    IntersectionSetInPlace(set, right);
                    environment.Set(target.Name, set);
                    return;
                case BinaryOperator.BitwiseXor:
                    SymmetricDifferenceSetInPlace(set, right);
                    environment.Set(target.Name, set);
                    return;
                case BinaryOperator.Subtract:
                    DifferenceSetInPlace(set, right);
                    environment.Set(target.Name, set);
                    return;
            }
        }

        var result = BinaryOperatorEvaluator.ApplyArithmetic(
            assignment.Operator,
            existing,
            right,
            $"Operator '{assignment.Operator}' not supported for augmented assignment.");
        environment.Set(target.Name, result);
    }

    private void ExecuteAugmentedIndexAssignment(
        IndexTarget target,
        AugmentedAssignmentStatement assignment,
        StarlarkEnvironment environment)
    {
        var container = _expressionEvaluator.Evaluate(target.Target, environment);
        var index = _expressionEvaluator.Evaluate(target.Index, environment);
        var existing = GetIndexedValue(container, index, target.Span);
        var right = _expressionEvaluator.Evaluate(assignment.Value, environment);
        var result = BinaryOperatorEvaluator.ApplyArithmetic(
            assignment.Operator,
            existing,
            right,
            $"Operator '{assignment.Operator}' not supported for augmented assignment.");

        switch (container)
        {
            case StarlarkList list:
                AssignListIndex(list, index, result, target.Span);
                break;
            case StarlarkDict dict:
                AssignDictIndex(dict, index, result);
                break;
            default:
                RuntimeErrors.Throw(
                    $"Index assignment not supported for '{container.TypeName}'.",
                    target.Span);
                break;
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
                RuntimeErrors.Throw(
                    $"Operator '+=' not supported for '{list.TypeName}' and '{value.TypeName}'.");
                break;
        }
    }

    private static StarlarkValue GetIndexedValue(StarlarkValue container, StarlarkValue index, SourceSpan span)
    {
        return container switch
        {
            StarlarkList list => IndexList(list, index, span),
            StarlarkTuple tuple => IndexTuple(tuple, index, span),
            StarlarkString text => IndexString(text, index, span),
            StarlarkDict dict => IndexDict(dict, index, span),
            _ => RuntimeErrors.Fail<StarlarkValue>(
                $"Indexing not supported for '{container.TypeName}'.",
                span)
        };
    }

    private static StarlarkValue IndexList(StarlarkList list, StarlarkValue index, SourceSpan span)
    {
        var position = RequireIndex(index, span);
        var resolved = ResolveIndex(position, list.Items.Count, span);
        return list.Items[resolved];
    }

    private static StarlarkValue IndexTuple(StarlarkTuple tuple, StarlarkValue index, SourceSpan span)
    {
        var position = RequireIndex(index, span);
        var resolved = ResolveIndex(position, tuple.Items.Count, span);
        return tuple.Items[resolved];
    }

    private static StarlarkValue IndexString(StarlarkString text, StarlarkValue index, SourceSpan span)
    {
        var position = RequireIndex(index, span);
        var resolved = ResolveIndex(position, text.Value.Length, span);
        return new StarlarkString(text.Value[resolved].ToString());
    }

    private static StarlarkValue IndexDict(StarlarkDict dict, StarlarkValue key, SourceSpan span)
    {
        StarlarkHash.EnsureHashable(key);
        foreach (var entry in dict.Entries)
        {
            if (Equals(entry.Key, key))
            {
                return entry.Value;
            }
        }

        RuntimeErrors.Throw("Key not found in dict.", span);
        return StarlarkNone.Instance;
    }

    private static int RequireIndex(StarlarkValue index, SourceSpan span)
    {
        if (index is StarlarkInt intValue)
        {
            return checked((int)intValue.Value);
        }

        return RuntimeErrors.Fail<int>($"Index must be an int, got '{index.TypeName}'.", span);
    }

    private static int ResolveIndex(int position, int length, SourceSpan span)
    {
        var resolved = position < 0 ? length + position : position;
        if (resolved < 0 || resolved >= length)
        {
            return RuntimeErrors.Fail<int>("Index out of range.", span);
        }

        return resolved;
    }

    private void AssignIndexTarget(IndexTarget target, StarlarkValue value, StarlarkEnvironment environment)
    {
        var container = _expressionEvaluator.Evaluate(target.Target, environment);
        var index = _expressionEvaluator.Evaluate(target.Index, environment);

        switch (container)
        {
            case StarlarkList list:
                AssignListIndex(list, index, value, target.Span);
                break;
            case StarlarkDict dict:
                AssignDictIndex(dict, index, value);
                break;
            default:
                RuntimeErrors.Throw(
                    $"Index assignment not supported for '{container.TypeName}'.",
                    target.Span);
                break;
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
            RuntimeErrors.Throw(
                $"Assignment length mismatch. Expected {targets.Count} values but got {items.Count}.",
                targets.Count > 0 ? targets[0].Span : null);
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
            _ => RuntimeErrors.Fail<IReadOnlyList<StarlarkValue>>(
                $"Value of type '{value.TypeName}' is not iterable for assignment.")
        };
    }

    private static void AssignListIndex(
        StarlarkList list,
        StarlarkValue index,
        StarlarkValue value,
        SourceSpan span)
    {
        var intIndex = index as StarlarkInt;
        if (intIndex == null)
        {
            RuntimeErrors.Throw($"Index must be an int, got '{index.TypeName}'.", span);
        }

        var position = checked((int)intIndex.Value);
        if (position < 0)
        {
            position = list.Items.Count + position;
        }

        if (position < 0 || position >= list.Items.Count)
        {
            RuntimeErrors.Throw("Index out of range.", span);
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

    private static void UnionDictInPlace(StarlarkDict dict, StarlarkValue right)
    {
        var other = right as StarlarkDict;
        if (other == null)
        {
            RuntimeErrors.Throw(
                $"Operator '|=' not supported for '{dict.TypeName}' and '{right.TypeName}'.");
        }

        var mutated = false;
        for (var i = 0; i < other.Entries.Count; i++)
        {
            var entry = other.Entries[i];
            mutated |= BinaryOperatorEvaluator.AddOrReplace(dict.Entries, entry.Key, entry.Value);
        }

        if (mutated)
        {
            dict.MarkMutated();
        }
    }

    private static void UnionSetInPlace(StarlarkSet set, StarlarkValue right)
    {
        var other = right as StarlarkSet;
        if (other == null)
        {
            RuntimeErrors.Throw(
                $"Operator '|=' not supported for '{set.TypeName}' and '{right.TypeName}'.");
        }

        for (var i = 0; i < other.Items.Count; i++)
        {
            set.AddValue(other.Items[i]);
        }
    }

    private static void IntersectionSetInPlace(StarlarkSet set, StarlarkValue right)
    {
        var other = right as StarlarkSet;
        if (other == null)
        {
            RuntimeErrors.Throw(
                $"Operator '&=' not supported for '{set.TypeName}' and '{right.TypeName}'.");
        }

        var mutated = false;
        for (var i = set.Items.Count - 1; i >= 0; i--)
        {
            if (!BinaryOperatorEvaluator.ContainsValue(other.Items, set.Items[i]))
            {
                set.Items.RemoveAt(i);
                mutated = true;
            }
        }

        if (mutated)
        {
            set.MarkMutated();
        }
    }

    private static void DifferenceSetInPlace(StarlarkSet set, StarlarkValue right)
    {
        var other = right as StarlarkSet;
        if (other == null)
        {
            RuntimeErrors.Throw(
                $"Operator '-=' not supported for '{set.TypeName}' and '{right.TypeName}'.");
        }

        var mutated = false;
        for (var i = set.Items.Count - 1; i >= 0; i--)
        {
            if (BinaryOperatorEvaluator.ContainsValue(other.Items, set.Items[i]))
            {
                set.Items.RemoveAt(i);
                mutated = true;
            }
        }

        if (mutated)
        {
            set.MarkMutated();
        }
    }

    private static void SymmetricDifferenceSetInPlace(StarlarkSet set, StarlarkValue right)
    {
        var other = right as StarlarkSet;
        if (other == null)
        {
            RuntimeErrors.Throw(
                $"Operator '^=' not supported for '{set.TypeName}' and '{right.TypeName}'.");
        }

        var result = new List<StarlarkValue>();
        foreach (var item in set.Items)
        {
            if (!BinaryOperatorEvaluator.ContainsValue(other.Items, item))
            {
                result.Add(item);
            }
        }

        foreach (var item in other.Items)
        {
            if (!BinaryOperatorEvaluator.ContainsValue(set.Items, item))
            {
                result.Add(item);
            }
        }

        set.Items.Clear();
        set.Items.AddRange(result);
        set.MarkMutated();
    }

    private static IEnumerable<StarlarkValue> Enumerate(StarlarkValue iterable, SourceSpan span)
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
            case StarlarkSet set:
                foreach (var item in set.EnumerateWithMutationCheck())
                {
                    yield return item;
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
                RuntimeErrors.Throw($"Type '{iterable.TypeName}' is not iterable.", span);
                yield break;
        }
    }
    private enum FlowKind
    {
        Normal,
        Return,
        Break,
        Continue
    }

    private readonly struct FlowResult
    {
        private FlowResult(FlowKind kind, StarlarkValue? value, StarlarkValue? lastValue, SourceSpan? span)
        {
            Kind = kind;
            Value = value;
            LastValue = lastValue;
            Span = span;
        }

        public FlowKind Kind { get; }
        public StarlarkValue? Value { get; }
        public StarlarkValue? LastValue { get; }
        public SourceSpan? Span { get; }

        public static FlowResult Normal(StarlarkValue? lastValue) =>
            new FlowResult(FlowKind.Normal, null, lastValue, null);

        public static FlowResult Return(StarlarkValue value, SourceSpan? span) =>
            new FlowResult(FlowKind.Return, value, null, span);

        public static FlowResult Break(SourceSpan? span) =>
            new FlowResult(FlowKind.Break, null, null, span);

        public static FlowResult Continue(SourceSpan? span) =>
            new FlowResult(FlowKind.Continue, null, null, span);
    }
}
