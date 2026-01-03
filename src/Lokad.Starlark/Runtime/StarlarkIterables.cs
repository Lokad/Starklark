using System.Collections.Generic;

namespace Lokad.Starlark.Runtime;

public sealed class StarlarkStringElems : StarlarkValue
{
    public StarlarkStringElems(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string TypeName => "string.elems";
    public override bool IsTruthy => Value.Length != 0;

    public IEnumerable<StarlarkValue> Enumerate()
    {
        for (var i = 0; i < Value.Length; i++)
        {
            yield return new StarlarkString(Value[i].ToString());
        }
    }
}

public sealed class StarlarkBytesElems : StarlarkValue
{
    public StarlarkBytesElems(byte[] bytes)
    {
        Bytes = bytes;
    }

    public byte[] Bytes { get; }

    public override string TypeName => "bytes.elems";
    public override bool IsTruthy => Bytes.Length != 0;

    public IEnumerable<StarlarkValue> Enumerate()
    {
        foreach (var item in Bytes)
        {
            yield return new StarlarkInt(item);
        }
    }
}
