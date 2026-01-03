using System.Collections.Generic;
using System.Text;

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
        var bytes = Encoding.UTF8.GetBytes(Value);
        foreach (var item in bytes)
        {
            yield return new StarlarkString(Encoding.Latin1.GetString(new[] { item }));
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
