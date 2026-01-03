using System;
using System.Numerics;

namespace Lokad.Starlark.Runtime;

internal static class StarlarkNumber
{
    internal static bool EqualFloatFloat(double left, double right)
    {
        if (double.IsNaN(left) || double.IsNaN(right))
        {
            return false;
        }

        return left.Equals(right);
    }

    internal static bool EqualIntFloat(long intValue, double floatValue)
    {
        if (!TryGetExactIntFromFloat(floatValue, out var floatAsInt))
        {
            return false;
        }

        return intValue == floatAsInt;
    }

    internal static int CompareIntFloat(long intValue, double floatValue)
    {
        if (double.IsPositiveInfinity(floatValue))
        {
            return -1;
        }

        if (double.IsNegativeInfinity(floatValue))
        {
            return 1;
        }

        return CompareIntToDoubleExact(intValue, floatValue);
    }

    internal static int CompareFloatInt(double floatValue, long intValue)
    {
        return -CompareIntFloat(intValue, floatValue);
    }

    internal static int HashInt(long value)
    {
        return value.GetHashCode();
    }

    internal static int HashFloat(double value)
    {
        if (TryGetExactIntFromFloat(value, out var intValue))
        {
            return intValue.GetHashCode();
        }

        return value.GetHashCode();
    }

    internal static bool TryGetExactIntFromFloat(double value, out long result)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            result = 0;
            return false;
        }

        if (value < long.MinValue || value > long.MaxValue)
        {
            result = 0;
            return false;
        }

        var truncated = (long)value;
        if (truncated != value)
        {
            result = 0;
            return false;
        }

        result = truncated;
        return true;
    }

    private static int CompareIntToDoubleExact(long intValue, double floatValue)
    {
        if (floatValue == 0.0)
        {
            return intValue.CompareTo(0);
        }

        var bits = BitConverter.DoubleToInt64Bits(floatValue);
        var negative = (bits >> 63) != 0;
        var exponentBits = (int)((bits >> 52) & 0x7FF);
        var mantissa = bits & 0xFFFFFFFFFFFFF;
        var exponent = exponentBits - 1023 - 52;

        if (exponentBits == 0)
        {
            if (mantissa == 0)
            {
                return intValue.CompareTo(0);
            }

            exponent = -1022 - 52;
        }
        else
        {
            mantissa |= 1L << 52;
        }

        var magnitude = new BigInteger(mantissa);
        if (negative)
        {
            magnitude = -magnitude;
        }

        if (exponent >= 0)
        {
            var floatInt = magnitude << exponent;
            return ((BigInteger)intValue).CompareTo(floatInt);
        }

        var shift = -exponent;
        var scaledInt = (BigInteger)intValue << shift;
        return scaledInt.CompareTo(magnitude);
    }
}
