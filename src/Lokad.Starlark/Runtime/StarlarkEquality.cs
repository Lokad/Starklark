using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lokad.Starlark.Runtime;

internal static class StarlarkEquality
{
    private const string RecursionError = "maximum recursion";

    internal static bool AreEqual(StarlarkValue left, StarlarkValue right)
    {
        var active = new HashSet<ReferencePair>(ReferencePairComparer.Instance);
        return AreEqualInternal(left, right, active);
    }

    private static bool AreEqualInternal(
        StarlarkValue left,
        StarlarkValue right,
        HashSet<ReferencePair> active)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return leftInt.Value == rightInt.Value;
        }

        if (left is StarlarkFloat leftFloat && right is StarlarkFloat rightFloat)
        {
            return StarlarkNumber.EqualFloatFloat(leftFloat.Value, rightFloat.Value);
        }

        if (left is StarlarkInt leftNumber && right is StarlarkFloat rightNumber)
        {
            return StarlarkNumber.EqualIntFloat(leftNumber.Value, rightNumber.Value);
        }

        if (left is StarlarkFloat leftNumberFloat && right is StarlarkInt rightNumberInt)
        {
            return StarlarkNumber.EqualIntFloat(rightNumberInt.Value, leftNumberFloat.Value);
        }

        if (left is StarlarkString leftString && right is StarlarkString rightString)
        {
            return leftString.Equals(rightString);
        }

        if (left is StarlarkBool leftBool && right is StarlarkBool rightBool)
        {
            return leftBool.Equals(rightBool);
        }

        if (left is StarlarkNone && right is StarlarkNone)
        {
            return true;
        }

        if (left is StarlarkList leftList && right is StarlarkList rightList)
        {
            return SequenceEquals(leftList.Items, rightList.Items, leftList, rightList, active);
        }

        if (left is StarlarkTuple leftTuple && right is StarlarkTuple rightTuple)
        {
            return SequenceEquals(leftTuple.Items, rightTuple.Items, leftTuple, rightTuple, active);
        }

        if (left is StarlarkDict leftDict && right is StarlarkDict rightDict)
        {
            return DictEquals(leftDict, rightDict, active);
        }

        return false;
    }

    private static bool SequenceEquals(
        IReadOnlyList<StarlarkValue> left,
        IReadOnlyList<StarlarkValue> right,
        object leftOwner,
        object rightOwner,
        HashSet<ReferencePair> active)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        var pair = new ReferencePair(leftOwner, rightOwner);
        if (!active.Add(pair))
        {
            throw new InvalidOperationException(RecursionError);
        }

        try
        {
            for (var i = 0; i < left.Count; i++)
            {
                if (!AreEqualInternal(left[i], right[i], active))
                {
                    return false;
                }
            }
        }
        finally
        {
            active.Remove(pair);
        }

        return true;
    }

    private static bool DictEquals(
        StarlarkDict left,
        StarlarkDict right,
        HashSet<ReferencePair> active)
    {
        if (left.Entries.Count != right.Entries.Count)
        {
            return false;
        }

        var pair = new ReferencePair(left, right);
        if (!active.Add(pair))
        {
            throw new InvalidOperationException(RecursionError);
        }

        try
        {
            foreach (var entry in left.Entries)
            {
                if (!TryGetEntryValue(right, entry.Key, active, out var value))
                {
                    return false;
                }

                if (!AreEqualInternal(entry.Value, value, active))
                {
                    return false;
                }
            }
        }
        finally
        {
            active.Remove(pair);
        }

        return true;
    }

    private static bool TryGetEntryValue(
        StarlarkDict dict,
        StarlarkValue key,
        HashSet<ReferencePair> active,
        out StarlarkValue value)
    {
        foreach (var entry in dict.Entries)
        {
            if (AreEqualInternal(entry.Key, key, active))
            {
                value = entry.Value;
                return true;
            }
        }

        value = StarlarkNone.Instance;
        return false;
    }

    private readonly struct ReferencePair : IEquatable<ReferencePair>
    {
        public ReferencePair(object left, object right)
        {
            Left = left;
            Right = right;
        }

        public object Left { get; }
        public object Right { get; }

        public bool Equals(ReferencePair other) =>
            ReferenceEquals(Left, other.Left) && ReferenceEquals(Right, other.Right);

        public override bool Equals(object? obj) => obj is ReferencePair other && Equals(other);

        public override int GetHashCode()
        {
            var leftHash = RuntimeHelpers.GetHashCode(Left);
            var rightHash = RuntimeHelpers.GetHashCode(Right);
            return HashCode.Combine(leftHash, rightHash);
        }
    }

    private sealed class ReferencePairComparer : IEqualityComparer<ReferencePair>
    {
        public static readonly ReferencePairComparer Instance = new ReferencePairComparer();

        public bool Equals(ReferencePair x, ReferencePair y) => x.Equals(y);

        public int GetHashCode(ReferencePair obj) => obj.GetHashCode();
    }
}
