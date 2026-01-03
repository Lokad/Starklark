using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

public sealed class ExpressionEvaluator
{
    public StarlarkValue Evaluate(Expression expression, StarlarkEnvironment environment)
    {
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
            _ => throw new InvalidOperationException(
                $"Unsupported literal type: {value.GetType().Name}.")
        };
    }

    private static StarlarkValue ResolveIdentifier(IdentifierExpression identifier, StarlarkEnvironment environment)
    {
        if (!environment.TryGet(identifier.Name, out var value))
        {
            throw new KeyNotFoundException($"Undefined identifier '{identifier.Name}'.");
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
            _ => throw new InvalidOperationException(
                $"Unary '-' not supported for type '{operand.TypeName}'.")
        };
    }

    private static StarlarkValue RequireNumber(StarlarkValue operand)
    {
        return operand switch
        {
            StarlarkInt => operand,
            StarlarkFloat => operand,
            _ => throw new InvalidOperationException(
                $"Unary '+' not supported for type '{operand.TypeName}'.")
        };
    }

    private static StarlarkValue BitwiseNot(StarlarkValue operand)
    {
        return operand switch
        {
            StarlarkInt value => new StarlarkInt(~value.Value),
            _ => throw new InvalidOperationException(
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
            BinaryOperator.Add => Add(leftValue, rightValue),
            BinaryOperator.Subtract => Subtract(leftValue, rightValue),
            BinaryOperator.Multiply => Multiply(leftValue, rightValue),
            BinaryOperator.Divide => Divide(leftValue, rightValue),
            BinaryOperator.FloorDivide => FloorDivide(leftValue, rightValue),
            BinaryOperator.Modulo => Modulo(leftValue, rightValue),
            BinaryOperator.BitwiseOr => BitwiseOr(leftValue, rightValue),
            BinaryOperator.BitwiseXor => BitwiseXor(leftValue, rightValue),
            BinaryOperator.BitwiseAnd => BitwiseAnd(leftValue, rightValue),
            BinaryOperator.ShiftLeft => ShiftLeft(leftValue, rightValue),
            BinaryOperator.ShiftRight => ShiftRight(leftValue, rightValue),
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

    private StarlarkValue EvaluateCall(CallExpression call, StarlarkEnvironment environment)
    {
        var callee = Evaluate(call.Callee, environment);
        if (callee is not StarlarkCallable function)
        {
            throw new InvalidOperationException(
                $"Attempted to call non-callable value of type '{callee.TypeName}'.");
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
                        throw new InvalidOperationException(
                            "Positional argument follows keyword or *args argument.");
                    }

                    args.Add(Evaluate(argument.Value, environment));
                    break;
                case CallArgumentKind.Keyword:
                    if (seenStarStar)
                    {
                        throw new InvalidOperationException(
                            "Keyword argument follows **kwargs argument.");
                    }

                    seenKeyword = true;
                    if (argument.Name == null)
                    {
                        throw new InvalidOperationException("Keyword argument missing name.");
                    }

                    if (kwargs.ContainsKey(argument.Name))
                    {
                        throw new InvalidOperationException(
                            $"Got multiple values for keyword argument '{argument.Name}'.");
                    }

                    kwargs[argument.Name] = Evaluate(argument.Value, environment);
                    break;
                case CallArgumentKind.Star:
                    if (seenKeyword)
                    {
                        throw new InvalidOperationException(
                            "*args argument follows keyword argument.");
                    }

                    if (seenStarStar)
                    {
                        throw new InvalidOperationException(
                            "*args argument follows **kwargs argument.");
                    }

                    seenStar = true;
                    foreach (var item in EnumerateCallArgs(Evaluate(argument.Value, environment)))
                    {
                        args.Add(item);
                    }
                    break;
                case CallArgumentKind.StarStar:
                    if (seenStarStar)
                    {
                        throw new InvalidOperationException(
                            "Multiple **kwargs arguments are not allowed.");
                    }

                    seenKeyword = true;
                    seenStarStar = true;
                    MergeKwArgs(kwargs, Evaluate(argument.Value, environment));
                    break;
                default:
                    throw new InvalidOperationException("Unsupported call argument.");
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
            IndexValue value => EvaluateIndexValue(target, value, environment),
            SliceIndex slice => EvaluateSlice(target, slice, environment),
            _ => throw new InvalidOperationException("Unsupported index specifier.")
        };
    }

    private StarlarkValue EvaluateIndexValue(
        StarlarkValue target,
        IndexValue value,
        StarlarkEnvironment environment)
    {
        var key = Evaluate(value.Value, environment);

        return target switch
        {
            StarlarkList list => IndexList(list, key),
            StarlarkTuple tuple => IndexTuple(tuple, key),
            StarlarkString text => IndexString(text, key),
            StarlarkBytes bytes => IndexBytes(bytes, key),
            StarlarkDict dict => IndexDict(dict, key),
            _ => throw new InvalidOperationException(
                $"Indexing not supported for type '{target.TypeName}'.")
        };
    }

    private StarlarkValue EvaluateSlice(
        StarlarkValue target,
        SliceIndex slice,
        StarlarkEnvironment environment)
    {
        var start = EvaluateOptional(slice.Start, environment);
        var stop = EvaluateOptional(slice.Stop, environment);
        var step = EvaluateOptional(slice.Step, environment);

        return target switch
        {
            StarlarkList list => SliceList(list, start, stop, step),
            StarlarkTuple tuple => SliceTuple(tuple, start, stop, step),
            StarlarkString text => SliceString(text, start, stop, step),
            StarlarkBytes bytes => SliceBytes(bytes, start, stop, step),
            _ => throw new InvalidOperationException(
                $"Slicing not supported for type '{target.TypeName}'.")
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

        throw new InvalidOperationException(
            $"Slice indices must be int, got '{value.TypeName}'.");
    }

    private static StarlarkValue SliceList(
        StarlarkList list,
        StarlarkInt? start,
        StarlarkInt? stop,
        StarlarkInt? step)
    {
        var (from, to, stride) = NormalizeSlice(list.Items.Count, start, stop, step);
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
        StarlarkInt? step)
    {
        var (from, to, stride) = NormalizeSlice(tuple.Items.Count, start, stop, step);
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
        StarlarkInt? step)
    {
        var (from, to, stride) = NormalizeSlice(text.Value.Length, start, stop, step);
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
        StarlarkInt? step)
    {
        var (from, to, stride) = NormalizeSlice(bytes.Bytes.Length, start, stop, step);
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
        StarlarkInt? step)
    {
        var stride = step?.Value ?? 1;
        if (stride == 0)
        {
            throw new InvalidOperationException("Slice step cannot be zero.");
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

    private static StarlarkValue IndexBytes(StarlarkBytes bytes, StarlarkValue index)
    {
        var position = RequireIndex(index);
        var resolved = ResolveIndex(position, bytes.Bytes.Length);
        return new StarlarkInt(bytes.Bytes[resolved]);
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

    private static bool IsIn(StarlarkValue item, StarlarkValue container)
    {
        return container switch
        {
            StarlarkList list => ContainsValue(list.Items, item),
            StarlarkTuple tuple => ContainsValue(tuple.Items, item),
            StarlarkDict dict => IsInDict(item, dict),
            StarlarkString text when item is StarlarkString needle =>
                text.Value.Contains(needle.Value, StringComparison.Ordinal),
            StarlarkBytes bytes when item is StarlarkInt intValue =>
                IsInBytes(intValue.Value, bytes.Bytes),
            StarlarkBytes bytes when item is StarlarkBytes needle =>
                ContainsBytes(bytes.Bytes, needle.Bytes),
            StarlarkRange range when item is StarlarkInt intValue =>
                IsInRange(intValue.Value, range),
            _ => throw new InvalidOperationException(
                $"Membership not supported for '{container.TypeName}'.")
        };
    }

    private static bool IsInDict(StarlarkValue item, StarlarkDict dict)
    {
        StarlarkHash.EnsureHashable(item);
        return dict.Entries.Any(entry => Equals(entry.Key, item));
    }

    private static bool IsInBytes(long value, byte[] bytes)
    {
        if (value is < 0 or > 255)
        {
            throw new InvalidOperationException("bytes membership requires an int in the range 0-255.");
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

    private static bool ContainsValue(IReadOnlyList<StarlarkValue> items, StarlarkValue item)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (Equals(items[i], item))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AddOrReplace(
        List<KeyValuePair<StarlarkValue, StarlarkValue>> entries,
        StarlarkValue key,
        StarlarkValue value)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (Equals(entries[i].Key, key))
            {
                entries[i] = new KeyValuePair<StarlarkValue, StarlarkValue>(key, value);
                return true;
            }
        }

        entries.Add(new KeyValuePair<StarlarkValue, StarlarkValue>(key, value));
        return true;
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
        if (TryGetNumber(left, out _, out _) && TryGetNumber(right, out _, out _))
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

        throw new InvalidOperationException(
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

    private static StarlarkValue Add(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkString leftString && right is StarlarkString rightString)
        {
            return new StarlarkString(leftString.Value + rightString.Value);
        }

        if (left is StarlarkBytes leftBytes && right is StarlarkBytes rightBytes)
        {
            var combined = new byte[leftBytes.Bytes.Length + rightBytes.Bytes.Length];
            leftBytes.Bytes.CopyTo(combined, 0);
            rightBytes.Bytes.CopyTo(combined, leftBytes.Bytes.Length);
            return new StarlarkBytes(combined);
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

        if (left is StarlarkBytes leftBytes && right is StarlarkInt rightCountBytes)
        {
            return new StarlarkBytes(RepeatBytes(leftBytes.Bytes, rightCountBytes.Value));
        }

        if (left is StarlarkInt leftCountBytes && right is StarlarkBytes rightBytes)
        {
            return new StarlarkBytes(RepeatBytes(rightBytes.Bytes, leftCountBytes.Value));
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

    private static byte[] RepeatBytes(byte[] value, long count)
    {
        if (count <= 0)
        {
            return Array.Empty<byte>();
        }

        if (count > int.MaxValue)
        {
            throw new InvalidOperationException("Repeat count is too large.");
        }

        var total = value.Length * (int)count;
        var result = new byte[total];
        for (var i = 0; i < count; i++)
        {
            value.CopyTo(result, i * value.Length);
        }

        return result;
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

    private static StarlarkValue BitwiseOr(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value | rightInt.Value);
        }

        if (left is StarlarkDict leftDict && right is StarlarkDict rightDict)
        {
            return UnionDict(leftDict, rightDict);
        }

        throw new InvalidOperationException(
            $"Operator '|' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue BitwiseXor(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value ^ rightInt.Value);
        }

        throw new InvalidOperationException(
            $"Operator '^' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue BitwiseAnd(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value & rightInt.Value);
        }

        throw new InvalidOperationException(
            $"Operator '&' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue ShiftLeft(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            if (rightInt.Value < 0)
            {
                throw new InvalidOperationException("shift count must be non-negative.");
            }

            return new StarlarkInt(leftInt.Value << (int)Math.Min(rightInt.Value, int.MaxValue));
        }

        throw new InvalidOperationException(
            $"Operator '<<' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue ShiftRight(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            if (rightInt.Value < 0)
            {
                throw new InvalidOperationException("shift count must be non-negative.");
            }

            return new StarlarkInt(leftInt.Value >> (int)Math.Min(rightInt.Value, int.MaxValue));
        }

        throw new InvalidOperationException(
            $"Operator '>>' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkDict UnionDict(StarlarkDict left, StarlarkDict right)
    {
        var entries = new List<KeyValuePair<StarlarkValue, StarlarkValue>>(left.Entries.Count + right.Entries.Count);
        entries.AddRange(left.Entries);
        for (var i = 0; i < right.Entries.Count; i++)
        {
            var entry = right.Entries[i];
            AddOrReplace(entries, entry.Key, entry.Value);
        }

        return new StarlarkDict(entries);
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
                    throw new InvalidOperationException("Invalid comprehension for-clause.");
                }

                var iterable = Evaluate(clause.Iterable, environment);
                foreach (var item in EnumerateComprehension(iterable))
                {
                    AssignTarget(clause.Target, item, environment);
                    EvaluateComprehensionClause(clauses, index + 1, environment, emit);
                }
                break;
            case ComprehensionClauseKind.If:
                if (clause.Condition == null)
                {
                    throw new InvalidOperationException("Invalid comprehension if-clause.");
                }

                var condition = Evaluate(clause.Condition, environment);
                if (condition.IsTruthy)
                {
                    EvaluateComprehensionClause(clauses, index + 1, environment, emit);
                }
                break;
            default:
                throw new InvalidOperationException("Unsupported comprehension clause.");
        }
    }

    private static IEnumerable<StarlarkValue> EnumerateComprehension(StarlarkValue value)
    {
        switch (value)
        {
            case StarlarkList list:
                return list.EnumerateWithMutationCheck();
            case StarlarkTuple tuple:
                return tuple.Items;
            case StarlarkDict dict:
                return dict.EnumerateWithMutationCheck();
            case StarlarkStringElems elems:
                return elems.Enumerate();
            case StarlarkBytesElems elems:
                return elems.Enumerate();
            case StarlarkRange range:
                return EnumerateRange(range);
            default:
                throw new InvalidOperationException(
                    $"Type '{value.TypeName}' is not iterable.");
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
                throw new InvalidOperationException(
                    $"Unsupported assignment target '{target.GetType().Name}'.");
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

    private static IEnumerable<StarlarkValue> EnumerateCallArgs(StarlarkValue value)
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
                throw new InvalidOperationException(
                    $"Object of type '{value.TypeName}' is not iterable.");
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
        StarlarkValue value)
    {
        if (value is not StarlarkDict dict)
        {
            throw new InvalidOperationException("**kwargs requires a dict.");
        }

        foreach (var entry in dict.Entries)
        {
            if (entry.Key is not StarlarkString key)
            {
                throw new InvalidOperationException("**kwargs keys must be strings.");
            }

            if (kwargs.ContainsKey(key.Value))
            {
                throw new InvalidOperationException(
                    $"Got multiple values for keyword argument '{key.Value}'.");
            }

            kwargs[key.Value] = entry.Value;
        }
    }
}
