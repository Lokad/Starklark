using System;
using System.Collections.Generic;

namespace Lokad.Starlark.Runtime;

public abstract record StarlarkValue
{
    public abstract string TypeName { get; }
    public abstract bool IsTruthy { get; }
}

public sealed record StarlarkBool(bool Value) : StarlarkValue
{
    public override string TypeName => "bool";
    public override bool IsTruthy => Value;
}

public sealed record StarlarkInt(long Value) : StarlarkValue
{
    public override string TypeName => "int";
    public override bool IsTruthy => Value != 0;
}

public sealed record StarlarkFloat(double Value) : StarlarkValue
{
    public override string TypeName => "float";
    public override bool IsTruthy => Value != 0.0;
}

public sealed record StarlarkString(string Value) : StarlarkValue
{
    public override string TypeName => "string";
    public override bool IsTruthy => Value.Length != 0;
}

public sealed record StarlarkList(IReadOnlyList<StarlarkValue> Items) : StarlarkValue
{
    public override string TypeName => "list";
    public override bool IsTruthy => Items.Count != 0;
}

public sealed record StarlarkTuple(IReadOnlyList<StarlarkValue> Items) : StarlarkValue
{
    public override string TypeName => "tuple";
    public override bool IsTruthy => Items.Count != 0;
}

public sealed record StarlarkNone : StarlarkValue
{
    public static readonly StarlarkNone Instance = new StarlarkNone();

    private StarlarkNone() { }

    public override string TypeName => "NoneType";
    public override bool IsTruthy => false;
}

public sealed record StarlarkFunction(
    string Name,
    Func<IReadOnlyList<StarlarkValue>, StarlarkValue> Invoke)
    : StarlarkValue
{
    public override string TypeName => "function";
    public override bool IsTruthy => true;

    public StarlarkValue Call(IReadOnlyList<StarlarkValue> args) => Invoke(args);
}
