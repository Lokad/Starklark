using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Starlark.Runtime;

public abstract class StarlarkValue
{
    public abstract string TypeName { get; }
    public abstract bool IsTruthy { get; }
}

public sealed class StarlarkBool : StarlarkValue, IEquatable<StarlarkBool>
{
    public StarlarkBool(bool value)
    {
        Value = value;
    }

    public bool Value { get; }

    public override string TypeName => "bool";
    public override bool IsTruthy => Value;

    public bool Equals(StarlarkBool? other) => other != null && Value == other.Value;

    public override bool Equals(object? obj) => obj is StarlarkBool other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();
}

public sealed class StarlarkInt : StarlarkValue, IEquatable<StarlarkInt>
{
    public StarlarkInt(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public override string TypeName => "int";
    public override bool IsTruthy => Value != 0;

    public bool Equals(StarlarkInt? other) => other != null && Value == other.Value;

    public override bool Equals(object? obj) => obj is StarlarkInt other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();
}

public sealed class StarlarkFloat : StarlarkValue, IEquatable<StarlarkFloat>
{
    public StarlarkFloat(double value)
    {
        Value = value;
    }

    public double Value { get; }

    public override string TypeName => "float";
    public override bool IsTruthy => Value != 0.0;

    public bool Equals(StarlarkFloat? other) => other != null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is StarlarkFloat other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();
}

public sealed class StarlarkString : StarlarkValue, IEquatable<StarlarkString>
{
    public StarlarkString(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string TypeName => "string";
    public override bool IsTruthy => Value.Length != 0;

    public bool Equals(StarlarkString? other) => other != null && StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is StarlarkString other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
}

public sealed class StarlarkList : StarlarkValue
{
    public StarlarkList(IEnumerable<StarlarkValue> items)
    {
        Items = new List<StarlarkValue>(items);
    }

    public List<StarlarkValue> Items { get; }

    public override string TypeName => "list";
    public override bool IsTruthy => Items.Count != 0;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is StarlarkList other && Items.SequenceEqual(other.Items);
    }

    public override int GetHashCode()
    {
        throw new InvalidOperationException("unhashable type: 'list'.");
    }
}

public sealed class StarlarkTuple : StarlarkValue
{
    public StarlarkTuple(IReadOnlyList<StarlarkValue> items)
    {
        Items = items;
    }

    public IReadOnlyList<StarlarkValue> Items { get; }

    public override string TypeName => "tuple";
    public override bool IsTruthy => Items.Count != 0;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is StarlarkTuple other && Items.SequenceEqual(other.Items);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Items)
        {
            StarlarkHash.EnsureHashable(item);
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}

public sealed class StarlarkDict : StarlarkValue
{
    public StarlarkDict(IEnumerable<KeyValuePair<StarlarkValue, StarlarkValue>> entries)
    {
        Entries = new List<KeyValuePair<StarlarkValue, StarlarkValue>>(entries);
    }

    public List<KeyValuePair<StarlarkValue, StarlarkValue>> Entries { get; }

    public override string TypeName => "dict";
    public override bool IsTruthy => Entries.Count != 0;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not StarlarkDict other)
        {
            return false;
        }

        if (Entries.Count != other.Entries.Count)
        {
            return false;
        }

        foreach (var entry in Entries)
        {
            if (!TryGetEntryValue(other, entry.Key, out var otherValue))
            {
                return false;
            }

            if (!Equals(entry.Value, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        throw new InvalidOperationException("unhashable type: 'dict'.");
    }

    private static bool TryGetEntryValue(StarlarkDict dict, StarlarkValue key, out StarlarkValue value)
    {
        foreach (var entry in dict.Entries)
        {
            if (Equals(entry.Key, key))
            {
                value = entry.Value;
                return true;
            }
        }

        value = StarlarkNone.Instance;
        return false;
    }
}

public sealed class StarlarkRange : StarlarkValue, IEquatable<StarlarkRange>
{
    public StarlarkRange(long start, long stop, long step)
    {
        Start = start;
        Stop = stop;
        Step = step;
    }

    public long Start { get; }
    public long Stop { get; }
    public long Step { get; }

    public override string TypeName => "range";
    public override bool IsTruthy => Count > 0;

    public long Count => Step > 0
        ? Start >= Stop ? 0 : (Stop - Start + Step - 1) / Step
        : Start <= Stop ? 0 : (Start - Stop - Step - 1) / -Step;

    public bool Equals(StarlarkRange? other) => other != null
        && Start == other.Start
        && Stop == other.Stop
        && Step == other.Step;

    public override bool Equals(object? obj) => obj is StarlarkRange other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, Stop, Step);
}

public sealed class StarlarkNone : StarlarkValue
{
    public static readonly StarlarkNone Instance = new StarlarkNone();

    private StarlarkNone() { }

    public override string TypeName => "NoneType";
    public override bool IsTruthy => false;

    public override bool Equals(object? obj) => obj is StarlarkNone;

    public override int GetHashCode() => 0;
}

public abstract class StarlarkCallable : StarlarkValue
{
    public abstract StarlarkValue Call(IReadOnlyList<StarlarkValue> args);

    public override string TypeName => "function";
    public override bool IsTruthy => true;
}

public sealed class StarlarkFunction : StarlarkCallable
{
    public StarlarkFunction(
        string name,
        Func<IReadOnlyList<StarlarkValue>, StarlarkValue> invoke)
    {
        Name = name;
        Invoke = invoke;
    }

    public string Name { get; }
    public Func<IReadOnlyList<StarlarkValue>, StarlarkValue> Invoke { get; }

    public override StarlarkValue Call(IReadOnlyList<StarlarkValue> args) => Invoke(args);
}

public sealed class StarlarkUserFunction : StarlarkCallable
{
    public StarlarkUserFunction(
        string name,
        IReadOnlyList<string> parameters,
        IReadOnlyList<Lokad.Starlark.Syntax.Statement> body,
        StarlarkEnvironment closure)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
        Closure = closure;
    }

    public string Name { get; }
    public IReadOnlyList<string> Parameters { get; }
    public IReadOnlyList<Lokad.Starlark.Syntax.Statement> Body { get; }
    public StarlarkEnvironment Closure { get; }

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
