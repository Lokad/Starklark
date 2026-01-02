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
            IndexExpression index => EvaluateIndex(index, environment),
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
            UnaryOperator.Negate => Negate(operand),
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
            BinaryOperator.Equal => new StarlarkBool(Equals(leftValue, rightValue)),
            BinaryOperator.NotEqual => new StarlarkBool(!Equals(leftValue, rightValue)),
            BinaryOperator.In => new StarlarkBool(IsIn(leftValue, rightValue)),
            BinaryOperator.NotIn => new StarlarkBool(!IsIn(leftValue, rightValue)),
            BinaryOperator.Less => new StarlarkBool(Compare(leftValue, rightValue) < 0),
            BinaryOperator.LessEqual => new StarlarkBool(Compare(leftValue, rightValue) <= 0),
            BinaryOperator.Greater => new StarlarkBool(Compare(leftValue, rightValue) > 0),
            BinaryOperator.GreaterEqual => new StarlarkBool(Compare(leftValue, rightValue) >= 0),
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

        var args = new StarlarkValue[call.Arguments.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            args[i] = Evaluate(call.Arguments[i], environment);
        }

        return function.Call(args);
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
            var value = Evaluate(entry.Value, environment);
            entries[i] = new KeyValuePair<StarlarkValue, StarlarkValue>(key, value);
        }

        return new StarlarkDict(entries);
    }

    private StarlarkValue EvaluateIndex(IndexExpression index, StarlarkEnvironment environment)
    {
        var target = Evaluate(index.Target, environment);
        var key = Evaluate(index.Index, environment);

        return target switch
        {
            StarlarkList list => IndexList(list, key),
            StarlarkTuple tuple => IndexTuple(tuple, key),
            StarlarkString text => IndexString(text, key),
            StarlarkDict dict => IndexDict(dict, key),
            _ => throw new InvalidOperationException(
                $"Indexing not supported for type '{target.TypeName}'.")
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
            StarlarkDict dict => dict.Entries.Any(entry => Equals(entry.Key, item)),
            StarlarkString text when item is StarlarkString needle =>
                text.Value.Contains(needle.Value, StringComparison.Ordinal),
            _ => throw new InvalidOperationException(
                $"Membership not supported for '{container.TypeName}'.")
        };
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

    private static int Compare(StarlarkValue left, StarlarkValue right)
    {
        if (TryGetNumber(left, out var leftNumber, out _)
            && TryGetNumber(right, out var rightNumber, out _))
        {
            return leftNumber.CompareTo(rightNumber);
        }

        if (left is StarlarkString leftString && right is StarlarkString rightString)
        {
            return string.Compare(leftString.Value, rightString.Value, StringComparison.Ordinal);
        }

        throw new InvalidOperationException(
            $"Comparison not supported between '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue Add(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkString leftString && right is StarlarkString rightString)
        {
            return new StarlarkString(leftString.Value + rightString.Value);
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
}
