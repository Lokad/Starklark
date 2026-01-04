using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Parsing;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

public sealed class ExpressionEvaluator
{
    public StarlarkValue Evaluate(Expression expression, StarlarkEnvironment environment)
    {
        environment.Guard.Check();
        return expression switch
        {
            LiteralExpression literal => ConvertLiteral(literal.Value),
            IdentifierExpression identifier => ResolveIdentifier(identifier, environment),
            UnaryExpression unary => EvaluateUnary(unary, environment),
            BinaryExpression binary => EvaluateBinary(binary, environment),
            CallExpression call => EvaluateCall(call, environment),
            ListExpression list => EvaluateList(list, environment),
            TupleExpression tuple => EvaluateTuple(tuple, environment),
            DictExpression dict => EvaluateDict(dict, environment),
            ListComprehensionExpression listComprehension => EvaluateListComprehension(listComprehension, environment),
            DictComprehensionExpression dictComprehension => EvaluateDictComprehension(dictComprehension, environment),
            IndexExpression index => EvaluateIndex(index, environment),
            AttributeExpression attribute => EvaluateAttribute(attribute, environment),
            ConditionalExpression conditional => EvaluateConditional(conditional, environment),
            LambdaExpression lambda => EvaluateLambda(lambda, environment),
            _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, "Unsupported expression.")
        };
    }

    private static StarlarkValue ConvertLiteral(object value)
    {
        return value switch
        {
            null => StarlarkNone.Instance,
            bool b => new StarlarkBool(b),
            long l => new StarlarkInt(l),
            int i => new StarlarkInt(i),
            double d => new StarlarkFloat(d),
            float f => new StarlarkFloat(f),
            string s => new StarlarkString(s),
            byte[] bytes => new StarlarkBytes(bytes),
            _ => RuntimeErrors.Fail<StarlarkValue>(
                $"Unsupported literal type: {value.GetType().Name}.")
        };
    }

    private static StarlarkValue ResolveIdentifier(IdentifierExpression identifier, StarlarkEnvironment environment)
    {
        var lookup = environment.TryGetDetailed(identifier.Name, out var value);
        if (lookup == LookupResult.ReferencedBeforeAssignment)
        {
            RuntimeErrors.Throw(
                $"local variable '{identifier.Name}' referenced before assignment.",
                identifier.Span);
        }

        if (lookup == LookupResult.NotFound)
        {
            RuntimeErrors.Throw($"undefined identifier '{identifier.Name}'.", identifier.Span);
        }

        return value;
    }

    private StarlarkValue EvaluateUnary(UnaryExpression unary, StarlarkEnvironment environment)
    {
        var operand = Evaluate(unary.Operand, environment);

        return unary.Operator switch
        {
            UnaryOperator.Not => new StarlarkBool(!operand.IsTruthy),
            UnaryOperator.Positive => RequireNumber(operand),
            UnaryOperator.Negate => Negate(operand),
            UnaryOperator.BitwiseNot => BitwiseNot(operand),
            _ => throw new ArgumentOutOfRangeException(nameof(unary.Operator), unary.Operator, null)
        };
    }

    private StarlarkValue Negate(StarlarkValue operand)
    {
        return operand switch
        {
            StarlarkInt value => new StarlarkInt(-value.Value),
            StarlarkFloat value => new StarlarkFloat(-value.Value),
            _ => RuntimeErrors.Fail<StarlarkValue>(
                $"Unary '-' not supported for type '{operand.TypeName}'.")
        };
    }

    private static StarlarkValue RequireNumber(StarlarkValue operand)
    {
        return operand switch
        {
            StarlarkInt => operand,
            StarlarkFloat => operand,
            _ => RuntimeErrors.Fail<StarlarkValue>(
                $"Unary '+' not supported for type '{operand.TypeName}'.")
        };
    }

    private static StarlarkValue BitwiseNot(StarlarkValue operand)
    {
        return operand switch
        {
            StarlarkInt value => new StarlarkInt(~value.Value),
            _ => RuntimeErrors.Fail<StarlarkValue>(
                $"Unary '~' not supported for type '{operand.TypeName}'.")
        };
    }

    private StarlarkValue EvaluateBinary(BinaryExpression binary, StarlarkEnvironment environment)
    {
        if (binary.Operator == BinaryOperator.And)
        {
            var left = Evaluate(binary.Left, environment);
            return left.IsTruthy ? Evaluate(binary.Right, environment) : left;
        }

        if (binary.Operator == BinaryOperator.Or)
        {
            var left = Evaluate(binary.Left, environment);
            return left.IsTruthy ? left : Evaluate(binary.Right, environment);
        }

        var leftValue = Evaluate(binary.Left, environment);
        var rightValue = Evaluate(binary.Right, environment);

        return binary.Operator switch
        {
            BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide
                or BinaryOperator.FloorDivide or BinaryOperator.Modulo or BinaryOperator.BitwiseOr
                or BinaryOperator.BitwiseXor or BinaryOperator.BitwiseAnd or BinaryOperator.ShiftLeft
                or BinaryOperator.ShiftRight
                => BinaryOperatorEvaluator.ApplyArithmetic(binary.Operator, leftValue, rightValue),
            BinaryOperator.Equal => new StarlarkBool(AreEqual(leftValue, rightValue)),
            BinaryOperator.NotEqual => new StarlarkBool(!AreEqual(leftValue, rightValue)),
            BinaryOperator.In => new StarlarkBool(IsIn(leftValue, rightValue)),
            BinaryOperator.NotIn => new StarlarkBool(!IsIn(leftValue, rightValue)),
            BinaryOperator.Less => new StarlarkBool(CompareRelational(leftValue, rightValue, RelationalOperator.Less)),
            BinaryOperator.LessEqual => new StarlarkBool(CompareRelational(leftValue, rightValue, RelationalOperator.LessEqual)),
            BinaryOperator.Greater => new StarlarkBool(CompareRelational(leftValue, rightValue, RelationalOperator.Greater)),
            BinaryOperator.GreaterEqual => new StarlarkBool(CompareRelational(leftValue, rightValue, RelationalOperator.GreaterEqual)),
            _ => throw new ArgumentOutOfRangeException(nameof(binary.Operator), binary.Operator, null)
        };
    }

    private StarlarkValue EvaluateConditional(ConditionalExpression conditional, StarlarkEnvironment environment)
    {
        var condition = Evaluate(conditional.Condition, environment);
        return condition.IsTruthy
            ? Evaluate(conditional.ThenExpression, environment)
            : Evaluate(conditional.ElseExpression, environment);
    }

    private static StarlarkValue EvaluateLambda(LambdaExpression lambda, StarlarkEnvironment environment)
    {
        var (names, defaults, varArgsName, kwArgsName) =
            FunctionParameterEvaluator.Evaluate(lambda.Parameters, environment);
        var body = new Statement[] { new ReturnStatement(lambda.Body, lambda.Body.Span) };
        var locals = FunctionLocalAnalyzer.CollectLocals(lambda.Parameters, body);
        return new StarlarkUserFunction(
            "lambda",
            names,
            defaults,
            varArgsName,
            kwArgsName,
            body,
            environment,
            locals);
    }

    private StarlarkValue EvaluateCall(CallExpression call, StarlarkEnvironment environment)
    {
        var callee = Evaluate(call.Callee, environment);
        var function = callee as StarlarkCallable;
        if (function == null)
        {
            RuntimeErrors.Throw(
                $"Attempted to call non-callable value of type '{callee.TypeName}'.",
                call.Span);
        }

        var args = new List<StarlarkValue>(call.Arguments.Count);
        var kwargs = new Dictionary<string, StarlarkValue>(StringComparer.Ordinal);
        var seenKeyword = false;
        var seenStar = false;
        var seenStarStar = false;

        for (var i = 0; i < call.Arguments.Count; i++)
        {
            var argument = call.Arguments[i];
            switch (argument.Kind)
            {
                case CallArgumentKind.Positional:
                    if (seenKeyword || seenStar)
                    {
                        RuntimeErrors.Throw(
                            "Positional argument follows keyword or *args argument.",
                            call.Span);
                    }

                    args.Add(Evaluate(argument.Value, environment));
                    break;
                case CallArgumentKind.Keyword:
                    if (seenStarStar)
                    {
                        RuntimeErrors.Throw("Keyword argument follows **kwargs argument.", call.Span);
                    }

                    seenKeyword = true;
                    if (argument.Name == null)
                    {
                        RuntimeErrors.Throw("Keyword argument missing name.", call.Span);
                    }

                    if (kwargs.ContainsKey(argument.Name))
                    {
                        RuntimeErrors.Throw(
                            $"Got multiple values for keyword argument '{argument.Name}'.",
                            call.Span);
                    }

                    kwargs[argument.Name] = Evaluate(argument.Value, environment);
                    break;
                case CallArgumentKind.Star:
                    if (seenKeyword)
                    {
                        RuntimeErrors.Throw("*args argument follows keyword argument.", call.Span);
                    }

                    if (seenStarStar)
                    {
                        RuntimeErrors.Throw("*args argument follows **kwargs argument.", call.Span);
                    }

                    seenStar = true;
                    foreach (var item in EnumerateCallArgs(Evaluate(argument.Value, environment), call.Span))
                    {
                        args.Add(item);
                    }
                    break;
                case CallArgumentKind.StarStar:
                    if (seenStarStar)
                    {
                        RuntimeErrors.Throw("Multiple **kwargs arguments are not allowed.", call.Span);
                    }

                    seenKeyword = true;
                    seenStarStar = true;
                    MergeKwArgs(kwargs, Evaluate(argument.Value, environment), call.Span);
                    break;
                default:
                    RuntimeErrors.Throw("Unsupported call argument.", call.Span);
                    break;
            }
        }

        return function.Call(args, kwargs);
    }

    private StarlarkValue EvaluateList(ListExpression list, StarlarkEnvironment environment)
    {
        var items = new StarlarkValue[list.Items.Count];
        for (var i = 0; i < list.Items.Count; i++)
        {
            items[i] = Evaluate(list.Items[i], environment);
        }

        return new StarlarkList(items);
    }

    private StarlarkValue EvaluateTuple(TupleExpression tuple, StarlarkEnvironment environment)
    {
        var items = new StarlarkValue[tuple.Items.Count];
        for (var i = 0; i < tuple.Items.Count; i++)
        {
            items[i] = Evaluate(tuple.Items[i], environment);
        }

        return new StarlarkTuple(items);
    }

    private StarlarkValue EvaluateDict(DictExpression dict, StarlarkEnvironment environment)
    {
        var entries = new KeyValuePair<StarlarkValue, StarlarkValue>[dict.Entries.Count];
        for (var i = 0; i < dict.Entries.Count; i++)
        {
            var entry = dict.Entries[i];
            var key = Evaluate(entry.Key, environment);
            StarlarkHash.EnsureHashable(key);
            var value = Evaluate(entry.Value, environment);
            entries[i] = new KeyValuePair<StarlarkValue, StarlarkValue>(key, value);
        }

        return new StarlarkDict(entries);
    }

    private StarlarkValue EvaluateListComprehension(
        ListComprehensionExpression comprehension,
        StarlarkEnvironment environment)
    {
        var result = new List<StarlarkValue>();
        var scope = environment.CreateChild();
        EvaluateComprehensionClauses(
            comprehension.Clauses,
            scope,
            () => result.Add(Evaluate(comprehension.Body, scope)));
        return new StarlarkList(result);
    }

    private StarlarkValue EvaluateDictComprehension(
        DictComprehensionExpression comprehension,
        StarlarkEnvironment environment)
    {
        var entries = new List<KeyValuePair<StarlarkValue, StarlarkValue>>();
        var scope = environment.CreateChild();
        EvaluateComprehensionClauses(
            comprehension.Clauses,
            scope,
            () =>
            {
                var key = Evaluate(comprehension.Key, scope);
                StarlarkHash.EnsureHashable(key);
                var value = Evaluate(comprehension.Value, scope);
                entries.Add(new KeyValuePair<StarlarkValue, StarlarkValue>(key, value));
            });
        return new StarlarkDict(entries);
    }

    private StarlarkValue EvaluateAttribute(AttributeExpression attribute, StarlarkEnvironment environment)
    {
        var target = Evaluate(attribute.Target, environment);
        return StarlarkMethods.Bind(target, attribute.Name);
    }

    private StarlarkValue EvaluateIndex(IndexExpression index, StarlarkEnvironment environment)
    {
        var target = Evaluate(index.Target, environment);
        return index.Index switch
        {
            IndexValue value => EvaluateIndexValue(target, value, environment, index.Span),
            SliceIndex slice => EvaluateSlice(target, slice, environment, index.Span),
            _ => RuntimeErrors.Fail<StarlarkValue>("Unsupported index specifier.", index.Span)
        };
    }

    private StarlarkValue EvaluateIndexValue(
        StarlarkValue target,
        IndexValue value,
        StarlarkEnvironment environment,
        SourceSpan span)
    {
        var key = Evaluate(value.Value, environment);

        return target switch
        {
            StarlarkList list => IndexList(list, key, span),
            StarlarkTuple tuple => IndexTuple(tuple, key, span),
            StarlarkString text => IndexString(text, key, span),
            StarlarkBytes bytes => IndexBytes(bytes, key, span),
            StarlarkDict dict => IndexDict(dict, key, span),
            _ => RuntimeErrors.Fail<StarlarkValue>(
                $"Indexing not supported for type '{target.TypeName}'.",
                span)
        };
    }

    private StarlarkValue EvaluateSlice(
        StarlarkValue target,
        SliceIndex slice,
        StarlarkEnvironment environment,
        SourceSpan span)
    {
        var start = EvaluateOptional(slice.Start, environment);
        var stop = EvaluateOptional(slice.Stop, environment);
        var step = EvaluateOptional(slice.Step, environment);

        return target switch
        {
            StarlarkList list => SliceList(list, start, stop, step, span),
            StarlarkTuple tuple => SliceTuple(tuple, start, stop, step, span),
            StarlarkString text => SliceString(text, start, stop, step, span),
            StarlarkBytes bytes => SliceBytes(bytes, start, stop, step, span),
            _ => RuntimeErrors.Fail<StarlarkValue>(
                $"Slicing not supported for type '{target.TypeName}'.",
                span)
        };
    }

    private StarlarkInt? EvaluateOptional(Expression? expression, StarlarkEnvironment environment)
    {
        if (expression == null)
        {
            return null;
        }

        var value = Evaluate(expression, environment);
        if (value is StarlarkInt intValue)
        {
            return intValue;
        }

        return RuntimeErrors.Fail<StarlarkInt>(
            $"Slice indices must be int, got '{value.TypeName}'.",
            expression.Span);
    }

    private static StarlarkValue SliceList(
        StarlarkList list,
        StarlarkInt? start,
        StarlarkInt? stop,
        StarlarkInt? step,
        SourceSpan span)
    {
        var (from, to, stride) = NormalizeSlice(list.Items.Count, start, stop, step, span);
        var result = new List<StarlarkValue>();
        if (stride > 0)
        {
            for (var i = from; i < to; i += stride)
            {
                result.Add(list.Items[i]);
            }
        }
        else
        {
            for (var i = from; i > to; i += stride)
            {
                result.Add(list.Items[i]);
            }
        }

        return new StarlarkList(result);
    }

    private static StarlarkValue SliceTuple(
        StarlarkTuple tuple,
        StarlarkInt? start,
        StarlarkInt? stop,
        StarlarkInt? step,
        SourceSpan span)
    {
        var (from, to, stride) = NormalizeSlice(tuple.Items.Count, start, stop, step, span);
        var result = new List<StarlarkValue>();
        if (stride > 0)
        {
            for (var i = from; i < to; i += stride)
            {
                result.Add(tuple.Items[i]);
            }
        }
        else
        {
            for (var i = from; i > to; i += stride)
            {
                result.Add(tuple.Items[i]);
            }
        }

        return new StarlarkTuple(result);
    }

    private static StarlarkValue SliceString(
        StarlarkString text,
        StarlarkInt? start,
        StarlarkInt? stop,
        StarlarkInt? step,
        SourceSpan span)
    {
        var (from, to, stride) = NormalizeSlice(text.Value.Length, start, stop, step, span);
        var builder = new System.Text.StringBuilder();
        if (stride > 0)
        {
            for (var i = from; i < to; i += stride)
            {
                builder.Append(text.Value[i]);
            }
        }
        else
        {
            for (var i = from; i > to; i += stride)
            {
                builder.Append(text.Value[i]);
            }
        }

        return new StarlarkString(builder.ToString());
    }

    private static StarlarkValue SliceBytes(
        StarlarkBytes bytes,
        StarlarkInt? start,
        StarlarkInt? stop,
        StarlarkInt? step,
        SourceSpan span)
    {
        var (from, to, stride) = NormalizeSlice(bytes.Bytes.Length, start, stop, step, span);
        var result = new List<byte>();
        if (stride > 0)
        {
            for (var i = from; i < to; i += stride)
            {
                result.Add(bytes.Bytes[i]);
            }
        }
        else
        {
            for (var i = from; i > to; i += stride)
            {
                result.Add(bytes.Bytes[i]);
            }
        }

        return new StarlarkBytes(result.ToArray());
    }

    private static (int Start, int Stop, int Step) NormalizeSlice(
        int length,
        StarlarkInt? start,
        StarlarkInt? stop,
        StarlarkInt? step,
        SourceSpan span)
    {
        var stride = step?.Value ?? 1;
        if (stride == 0)
        {
            RuntimeErrors.Throw("Slice step cannot be zero.", span);
        }

        var stepValue = checked((int)stride);
        if (stepValue > 0)
        {
            var from = NormalizeIndex(start?.Value, length, defaultValue: 0, clamp: true);
            var to = NormalizeIndex(stop?.Value, length, defaultValue: length, clamp: true);
            return (from, to, stepValue);
        }
        else
        {
            var from = NormalizeIndex(start?.Value, length, defaultValue: length - 1, clamp: false);
            var to = NormalizeIndex(stop?.Value, length, defaultValue: -1, clamp: false);
            return (from, to, stepValue);
        }
    }

    private static int NormalizeIndex(long? value, int length, int defaultValue, bool clamp)
    {
        if (value == null)
        {
            return defaultValue;
        }

        var index = checked((int)value.Value);
        if (index < 0)
        {
            index += length;
        }

        if (clamp)
        {
            if (index < 0)
            {
                return 0;
            }

            if (index > length)
            {
                return length;
            }
        }
        else
        {
            if (index < -1)
            {
                return -1;
            }

            if (index >= length)
            {
                return length - 1;
            }
        }

        return index;
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

    private static StarlarkValue IndexBytes(StarlarkBytes bytes, StarlarkValue index, SourceSpan span)
    {
        var position = RequireIndex(index, span);
        var resolved = ResolveIndex(position, bytes.Bytes.Length, span);
        return new StarlarkInt(bytes.Bytes[resolved]);
    }

    private static StarlarkValue IndexDict(StarlarkDict dict, StarlarkValue key, SourceSpan span)
    {
        StarlarkHash.EnsureHashable(key);
        if (dict.TryGetValue(key, out var value))
        {
            return value;
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

    private static bool IsIn(StarlarkValue item, StarlarkValue container)
    {
        return container switch
        {
            StarlarkList list => BinaryOperatorEvaluator.ContainsValue(list.Items, item),
            StarlarkTuple tuple => BinaryOperatorEvaluator.ContainsValue(tuple.Items, item),
            StarlarkDict dict => IsInDict(item, dict),
            StarlarkSet set => IsInSet(item, set),
            StarlarkString text => item is StarlarkString needle
                ? text.Value.Contains(needle.Value, StringComparison.Ordinal)
                : RuntimeErrors.Fail<bool>("in string requires string as left operand."),
            StarlarkBytes bytes => item switch
            {
                StarlarkInt intValue => IsInBytes(intValue.Value, bytes.Bytes),
                StarlarkBytes needle => ContainsBytes(bytes.Bytes, needle.Bytes),
                _ => RuntimeErrors.Fail<bool>("in bytes requires bytes or int as left operand.")
            },
            StarlarkRange range when item is StarlarkInt intValue =>
                IsInRange(intValue.Value, range),
            _ => RuntimeErrors.Fail<bool>(
                $"Membership not supported for '{container.TypeName}'.")
        };
    }

    private static bool IsInDict(StarlarkValue item, StarlarkDict dict)
    {
        StarlarkHash.EnsureHashable(item);
        return dict.ContainsKey(item);
    }

    private static bool IsInSet(StarlarkValue item, StarlarkSet set)
    {
        StarlarkHash.EnsureHashable(item);
        return set.Contains(item);
    }

    private static bool IsInBytes(long value, byte[] bytes)
    {
        if (value is < 0 or > 255)
        {
            RuntimeErrors.Throw("bytes membership requires an int in the range 0-255.");
        }

        var needle = (byte)value;
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == needle)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsBytes(byte[] bytes, byte[] needle)
    {
        if (needle.Length == 0)
        {
            return true;
        }

        if (needle.Length > bytes.Length)
        {
            return false;
        }

        for (var i = 0; i <= bytes.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (bytes[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInRange(long value, StarlarkRange range)
    {
        if (range.Step > 0)
        {
            if (value < range.Start || value >= range.Stop)
            {
                return false;
            }

            return (value - range.Start) % range.Step == 0;
        }

        if (value > range.Start || value <= range.Stop)
        {
            return false;
        }

        return (range.Start - value) % (-range.Step) == 0;
    }

    private enum RelationalOperator
    {
        Less,
        LessEqual,
        Greater,
        GreaterEqual
    }

    private static bool AreEqual(StarlarkValue left, StarlarkValue right)
    {
        return StarlarkEquality.AreEqual(left, right);
    }

    private static bool CompareRelational(StarlarkValue left, StarlarkValue right, RelationalOperator op)
    {
        if (BinaryOperatorEvaluator.TryGetNumber(left, out _, out _)
            && BinaryOperatorEvaluator.TryGetNumber(right, out _, out _))
        {
            return CompareNumbers(left, right, op);
        }

        var compare = CompareNonNumeric(left, right);
        return op switch
        {
            RelationalOperator.Less => compare < 0,
            RelationalOperator.LessEqual => compare <= 0,
            RelationalOperator.Greater => compare > 0,
            RelationalOperator.GreaterEqual => compare >= 0,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    private static int CompareNonNumeric(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkString leftString && right is StarlarkString rightString)
        {
            return string.Compare(leftString.Value, rightString.Value, StringComparison.Ordinal);
        }

        if (left is StarlarkBool leftBool && right is StarlarkBool rightBool)
        {
            return leftBool.Value.CompareTo(rightBool.Value);
        }

        if (left is StarlarkBytes leftBytes && right is StarlarkBytes rightBytes)
        {
            return CompareBytes(leftBytes.Bytes, rightBytes.Bytes);
        }

        return RuntimeErrors.Fail<int>(
            $"Comparison not supported between '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static int CompareBytes(byte[] left, byte[] right)
    {
        var length = left.Length < right.Length ? left.Length : right.Length;
        for (var i = 0; i < length; i++)
        {
            if (left[i] != right[i])
            {
                return left[i].CompareTo(right[i]);
            }
        }

        return left.Length.CompareTo(right.Length);
    }

    private static bool CompareNumbers(StarlarkValue left, StarlarkValue right, RelationalOperator op)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return CompareLong(leftInt.Value, rightInt.Value, op);
        }

        if (left is StarlarkFloat leftFloat && right is StarlarkFloat rightFloat)
        {
            if (double.IsNaN(leftFloat.Value) || double.IsNaN(rightFloat.Value))
            {
                return false;
            }

            return op switch
            {
                RelationalOperator.Less => leftFloat.Value < rightFloat.Value,
                RelationalOperator.LessEqual => leftFloat.Value <= rightFloat.Value,
                RelationalOperator.Greater => leftFloat.Value > rightFloat.Value,
                RelationalOperator.GreaterEqual => leftFloat.Value >= rightFloat.Value,
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };
        }

        if (left is StarlarkInt leftNumber && right is StarlarkFloat rightNumber)
        {
            if (double.IsNaN(rightNumber.Value))
            {
                return false;
            }

            var compare = StarlarkNumber.CompareIntFloat(leftNumber.Value, rightNumber.Value);
            return CompareFromSign(compare, op);
        }

        if (left is StarlarkFloat leftFloatNumber && right is StarlarkInt rightIntNumber)
        {
            if (double.IsNaN(leftFloatNumber.Value))
            {
                return false;
            }

            var compare = StarlarkNumber.CompareFloatInt(leftFloatNumber.Value, rightIntNumber.Value);
            return CompareFromSign(compare, op);
        }

        return false;
    }

    private static bool CompareLong(long left, long right, RelationalOperator op)
    {
        return op switch
        {
            RelationalOperator.Less => left < right,
            RelationalOperator.LessEqual => left <= right,
            RelationalOperator.Greater => left > right,
            RelationalOperator.GreaterEqual => left >= right,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    private static bool CompareFromSign(int compare, RelationalOperator op)
    {
        return op switch
        {
            RelationalOperator.Less => compare < 0,
            RelationalOperator.LessEqual => compare <= 0,
            RelationalOperator.Greater => compare > 0,
            RelationalOperator.GreaterEqual => compare >= 0,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    private void EvaluateComprehensionClauses(
        IReadOnlyList<ComprehensionClause> clauses,
        StarlarkEnvironment environment,
        Action emit)
    {
        EvaluateComprehensionClause(clauses, 0, environment, emit);
    }

    private void EvaluateComprehensionClause(
        IReadOnlyList<ComprehensionClause> clauses,
        int index,
        StarlarkEnvironment environment,
        Action emit)
    {
        if (index >= clauses.Count)
        {
            emit();
            return;
        }

        var clause = clauses[index];
        switch (clause.Kind)
        {
            case ComprehensionClauseKind.For:
                if (clause.Target == null || clause.Iterable == null)
                {
                    RuntimeErrors.Throw("Invalid comprehension for-clause.", clause.Span);
                }

                var iterable = Evaluate(clause.Iterable, environment);
                foreach (var item in EnumerateComprehension(iterable, clause.Span))
                {
                    environment.Guard.Check();
                    AssignTarget(clause.Target, item, environment);
                    EvaluateComprehensionClause(clauses, index + 1, environment, emit);
                }
                break;
            case ComprehensionClauseKind.If:
                if (clause.Condition == null)
                {
                    RuntimeErrors.Throw("Invalid comprehension if-clause.", clause.Span);
                }

                var condition = Evaluate(clause.Condition, environment);
                if (condition.IsTruthy)
                {
                    EvaluateComprehensionClause(clauses, index + 1, environment, emit);
                }
                break;
            default:
                RuntimeErrors.Throw("Unsupported comprehension clause.", clause.Span);
                break;
        }
    }

    private static IEnumerable<StarlarkValue> EnumerateComprehension(StarlarkValue value, SourceSpan span)
    {
        switch (value)
        {
            case StarlarkList list:
                return list.EnumerateWithMutationCheck();
            case StarlarkTuple tuple:
                return tuple.Items;
            case StarlarkDict dict:
                return dict.EnumerateWithMutationCheck();
            case StarlarkSet set:
                return set.EnumerateWithMutationCheck();
            case StarlarkStringElems elems:
                return elems.Enumerate();
            case StarlarkBytesElems elems:
                return elems.Enumerate();
            case StarlarkRange range:
                return EnumerateRange(range);
            default:
                return RuntimeErrors.Fail<IEnumerable<StarlarkValue>>(
                    $"Type '{value.TypeName}' is not iterable.",
                    span);
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
                RuntimeErrors.Throw($"Unsupported assignment target '{target.GetType().Name}'.");
                break;
        }
    }

    private void AssignIndexTarget(IndexTarget target, StarlarkValue value, StarlarkEnvironment environment)
    {
        var container = Evaluate(target.Target, environment);
        var index = Evaluate(target.Index, environment);

        switch (container)
        {
            case StarlarkList list:
                AssignListIndex(list, index, value);
                break;
            case StarlarkDict dict:
                AssignDictIndex(dict, index, value);
                break;
            default:
                RuntimeErrors.Throw($"Index assignment not supported for '{container.TypeName}'.");
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
            _ => RuntimeErrors.Fail<IReadOnlyList<StarlarkValue>>(
                $"Value of type '{value.TypeName}' is not iterable for assignment.")
        };
    }

    private static void AssignListIndex(StarlarkList list, StarlarkValue index, StarlarkValue value)
    {
        var intIndex = index as StarlarkInt;
        if (intIndex == null)
        {
            RuntimeErrors.Throw($"Index must be an int, got '{index.TypeName}'.");
        }

        var position = checked((int)intIndex.Value);
        if (position < 0)
        {
            position = list.Items.Count + position;
        }

        if (position < 0 || position >= list.Items.Count)
        {
            RuntimeErrors.Throw("Index out of range.");
        }

        list.Items[position] = value;
        list.MarkMutated();
    }

    private static void AssignDictIndex(StarlarkDict dict, StarlarkValue key, StarlarkValue value)
    {
        StarlarkHash.EnsureHashable(key);
        dict.SetValue(key, value);
    }

    private static IEnumerable<StarlarkValue> EnumerateCallArgs(StarlarkValue value, SourceSpan span)
    {
        switch (value)
        {
            case StarlarkList list:
                return list.Items;
            case StarlarkTuple tuple:
                return tuple.Items;
            case StarlarkDict dict:
                return dict.Entries.Select(entry => entry.Key);
            case StarlarkRange range:
                return EnumerateRange(range);
            case StarlarkStringElems elems:
                return elems.Enumerate();
            case StarlarkBytesElems elems:
                return elems.Enumerate();
            default:
                return RuntimeErrors.Fail<IEnumerable<StarlarkValue>>(
                    $"Object of type '{value.TypeName}' is not iterable.",
                    span);
        }
    }

    private static IEnumerable<StarlarkValue> EnumerateRange(StarlarkRange range)
    {
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
    }

    private static void MergeKwArgs(
        IDictionary<string, StarlarkValue> kwargs,
        StarlarkValue value,
        SourceSpan span)
    {
        var dict = value as StarlarkDict;
        if (dict == null)
        {
            RuntimeErrors.Throw("**kwargs requires a dict.", span);
        }

        foreach (var entry in dict.Entries)
        {
            var key = entry.Key as StarlarkString;
            if (key == null)
            {
                RuntimeErrors.Throw("**kwargs keys must be strings.", span);
            }

            if (kwargs.ContainsKey(key.Value))
            {
                RuntimeErrors.Throw($"Got multiple values for keyword argument '{key.Value}'.", span);
            }

            kwargs[key.Value] = entry.Value;
        }
    }
}
