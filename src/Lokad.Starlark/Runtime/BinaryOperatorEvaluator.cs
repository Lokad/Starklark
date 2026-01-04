using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

internal static class BinaryOperatorEvaluator
{
    internal static StarlarkValue ApplyArithmetic(
        BinaryOperator op,
        StarlarkValue left,
        StarlarkValue right,
        string? unsupportedMessage = null)
    {
        return op switch
        {
            BinaryOperator.Add => Add(left, right),
            BinaryOperator.Subtract => Subtract(left, right),
            BinaryOperator.Multiply => Multiply(left, right),
            BinaryOperator.Divide => Divide(left, right),
            BinaryOperator.FloorDivide => FloorDivide(left, right),
            BinaryOperator.Modulo => Modulo(left, right),
            BinaryOperator.BitwiseOr => BitwiseOr(left, right),
            BinaryOperator.BitwiseXor => BitwiseXor(left, right),
            BinaryOperator.BitwiseAnd => BitwiseAnd(left, right),
            BinaryOperator.ShiftLeft => ShiftLeft(left, right),
            BinaryOperator.ShiftRight => ShiftRight(left, right),
            _ => RuntimeErrors.Fail<StarlarkValue>(
                unsupportedMessage ?? $"unknown binary op: {op}.")
        };
    }

    internal static StarlarkValue Add(StarlarkValue left, StarlarkValue right)
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

        return FailBinaryOp("+", left, right);
    }

    internal static StarlarkValue Subtract(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkSet leftSet && right is StarlarkSet rightSet)
        {
            return DifferenceSet(leftSet, rightSet);
        }

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

        return FailBinaryOp("-", left, right);
    }

    internal static StarlarkValue Multiply(StarlarkValue left, StarlarkValue right)
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

        if (left is StarlarkBytes leftBytes && right is StarlarkInt bytesCount)
        {
            return new StarlarkBytes(RepeatBytes(leftBytes.Bytes, bytesCount.Value));
        }

        if (left is StarlarkInt leftBytesCount && right is StarlarkBytes rightBytes)
        {
            return new StarlarkBytes(RepeatBytes(rightBytes.Bytes, leftBytesCount.Value));
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

        return FailBinaryOp("*", left, right);
    }

    internal static StarlarkValue Divide(StarlarkValue left, StarlarkValue right)
    {
        if (TryGetNumber(left, out var leftNumber, out _)
            && TryGetNumber(right, out var rightNumber, out _))
        {
            if (rightNumber == 0)
            {
                RuntimeErrors.Throw("division by zero");
            }

            return new StarlarkFloat(leftNumber / rightNumber);
        }

        return FailBinaryOp("/", left, right);
    }

    internal static StarlarkValue FloorDivide(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            if (rightInt.Value == 0)
            {
                RuntimeErrors.Throw("division by zero");
            }

            var quotient = leftInt.Value / rightInt.Value;
            var remainder = leftInt.Value % rightInt.Value;
            if (remainder != 0 && (remainder < 0) != (rightInt.Value < 0))
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
                RuntimeErrors.Throw("division by zero");
            }

            return new StarlarkFloat(Math.Floor(leftNumber / rightNumber));
        }

        return FailBinaryOp("//", left, right);
    }

    internal static StarlarkValue Modulo(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkString text)
        {
            return new StarlarkString(StarlarkFormatting.FormatPercent(text.Value, right));
        }

        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            if (rightInt.Value == 0)
            {
                RuntimeErrors.Throw("division by zero");
            }

            var quotient = leftInt.Value / rightInt.Value;
            var remainder = leftInt.Value % rightInt.Value;
            if (remainder != 0 && (remainder < 0) != (rightInt.Value < 0))
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
                RuntimeErrors.Throw("division by zero");
            }

            var quotient = Math.Floor(leftNumber / rightNumber);
            return new StarlarkFloat(leftNumber - quotient * rightNumber);
        }

        return FailBinaryOp("%", left, right);
    }

    internal static StarlarkValue BitwiseOr(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value | rightInt.Value);
        }

        if (left is StarlarkDict leftDict && right is StarlarkDict rightDict)
        {
            return UnionDict(leftDict, rightDict);
        }

        if (left is StarlarkSet leftSet && right is StarlarkSet rightSet)
        {
            return UnionSet(leftSet, rightSet);
        }

        return FailBinaryOp("|", left, right);
    }

    internal static StarlarkValue BitwiseXor(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value ^ rightInt.Value);
        }

        if (left is StarlarkSet leftSet && right is StarlarkSet rightSet)
        {
            return SymmetricDifferenceSet(leftSet, rightSet);
        }

        return FailBinaryOp("^", left, right);
    }

    internal static StarlarkValue BitwiseAnd(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value & rightInt.Value);
        }

        if (left is StarlarkSet leftSet && right is StarlarkSet rightSet)
        {
            return IntersectionSet(leftSet, rightSet);
        }

        return FailBinaryOp("&", left, right);
    }

    internal static StarlarkValue ShiftLeft(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            if (rightInt.Value < 0)
            {
                RuntimeErrors.Throw("shift count must be non-negative.");
            }

            return new StarlarkInt(leftInt.Value << (int)Math.Min(rightInt.Value, int.MaxValue));
        }

        return FailBinaryOp("<<", left, right);
    }

    internal static StarlarkValue ShiftRight(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            if (rightInt.Value < 0)
            {
                RuntimeErrors.Throw("shift count must be non-negative.");
            }

            return new StarlarkInt(leftInt.Value >> (int)Math.Min(rightInt.Value, int.MaxValue));
        }

        return FailBinaryOp(">>", left, right);
    }

    internal static StarlarkDict UnionDict(StarlarkDict left, StarlarkDict right)
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

    internal static StarlarkSet UnionSet(StarlarkSet left, StarlarkSet right)
    {
        var items = new List<StarlarkValue>(left.Items);
        foreach (var item in right.Items)
        {
            if (!ContainsValue(items, item))
            {
                items.Add(item);
            }
        }

        return new StarlarkSet(items);
    }

    internal static StarlarkSet IntersectionSet(StarlarkSet left, StarlarkSet right)
    {
        var items = new List<StarlarkValue>();
        foreach (var item in left.Items)
        {
            if (ContainsValue(right.Items, item))
            {
                items.Add(item);
            }
        }

        return new StarlarkSet(items);
    }

    internal static StarlarkSet DifferenceSet(StarlarkSet left, StarlarkSet right)
    {
        var items = new List<StarlarkValue>();
        foreach (var item in left.Items)
        {
            if (!ContainsValue(right.Items, item))
            {
                items.Add(item);
            }
        }

        return new StarlarkSet(items);
    }

    internal static StarlarkSet SymmetricDifferenceSet(StarlarkSet left, StarlarkSet right)
    {
        var items = new List<StarlarkValue>();
        foreach (var item in left.Items)
        {
            if (!ContainsValue(right.Items, item))
            {
                items.Add(item);
            }
        }

        foreach (var item in right.Items)
        {
            if (!ContainsValue(left.Items, item))
            {
                items.Add(item);
            }
        }

        return new StarlarkSet(items);
    }

    internal static bool TryGetNumber(StarlarkValue value, out double number, out bool isInt)
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

    internal static bool AddOrReplace(
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

    internal static bool ContainsValue(IReadOnlyList<StarlarkValue> items, StarlarkValue value)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (StarlarkEquality.AreEqual(items[i], value))
            {
                return true;
            }
        }

        return false;
    }

    private static StarlarkValue FailBinaryOp(string op, StarlarkValue left, StarlarkValue right)
    {
        return RuntimeErrors.Fail<StarlarkValue>(
            $"unknown binary op: {left.TypeName} {op} {right.TypeName}");
    }

    private static byte[] RepeatBytes(byte[] bytes, long count)
    {
        if (count <= 0)
        {
            return Array.Empty<byte>();
        }

        if (count > int.MaxValue)
        {
            RuntimeErrors.Throw("Repeat count is too large.");
        }

        var total = bytes.Length * (int)count;
        var result = new byte[total];
        for (var i = 0; i < count; i++)
        {
            bytes.CopyTo(result, (int)i * bytes.Length);
        }

        return result;
    }

    private static string RepeatString(string value, long count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        if (count > int.MaxValue)
        {
            RuntimeErrors.Throw("Repeat count is too large.");
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
            RuntimeErrors.Throw("Repeat count is too large.");
        }

        var total = checked(items.Count * (int)count);
        var result = new List<StarlarkValue>(total);
        for (var i = 0; i < count; i++)
        {
            result.AddRange(items);
        }

        return result;
    }
}
