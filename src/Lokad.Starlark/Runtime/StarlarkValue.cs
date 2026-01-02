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

public sealed record StarlarkDict(IReadOnlyList<KeyValuePair<StarlarkValue, StarlarkValue>> Entries)
    : StarlarkValue
{
    public override string TypeName => "dict";
    public override bool IsTruthy => Entries.Count != 0;
}

public sealed record StarlarkNone : StarlarkValue
{
    public static readonly StarlarkNone Instance = new StarlarkNone();

    private StarlarkNone() { }

    public override string TypeName => "NoneType";
    public override bool IsTruthy => false;
}

public abstract record StarlarkCallable : StarlarkValue
{
    public abstract StarlarkValue Call(IReadOnlyList<StarlarkValue> args);

    public override string TypeName => "function";
    public override bool IsTruthy => true;
}

public sealed record StarlarkFunction(
    string Name,
    Func<IReadOnlyList<StarlarkValue>, StarlarkValue> Invoke)
    : StarlarkCallable
{
    public override StarlarkValue Call(IReadOnlyList<StarlarkValue> args) => Invoke(args);
}

public sealed record StarlarkUserFunction(
    string Name,
    IReadOnlyList<string> Parameters,
    IReadOnlyList<Lokad.Starlark.Syntax.Statement> Body,
    StarlarkEnvironment Closure)
    : StarlarkCallable
{
    public override StarlarkValue Call(IReadOnlyList<StarlarkValue> args)
    {
        if (args.Count != Parameters.Count)
        {
            throw new InvalidOperationException(
                $"Function '{Name}' expects {Parameters.Count} arguments but got {args.Count}.");
        }

        var callEnvironment = Closure.CreateChild();
        for (var i = 0; i < Parameters.Count; i++)
        {
            callEnvironment.Set(Parameters[i], args[i]);
        }

        var evaluator = new ModuleEvaluator();
        return evaluator.ExecuteFunctionBody(Body, callEnvironment);
    }
}
