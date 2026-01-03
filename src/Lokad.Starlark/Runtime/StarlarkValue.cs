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

    public override bool Equals(object? obj)
    {
        if (obj is StarlarkInt other)
        {
            return Equals(other);
        }

        if (obj is StarlarkFloat floatValue)
        {
            return StarlarkNumber.EqualIntFloat(Value, floatValue.Value);
        }

        return false;
    }

    public override int GetHashCode() => StarlarkNumber.HashInt(Value);
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

    public bool Equals(StarlarkFloat? other) =>
        other != null && StarlarkNumber.EqualFloatFloat(Value, other.Value);

    public override bool Equals(object? obj)
    {
        if (obj is StarlarkFloat other)
        {
            return StarlarkNumber.EqualFloatFloat(Value, other.Value);
        }

        if (obj is StarlarkInt intValue)
        {
            return StarlarkNumber.EqualIntFloat(intValue.Value, Value);
        }

        return false;
    }

    public override int GetHashCode() => StarlarkNumber.HashFloat(Value);
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

public sealed class StarlarkBytes : StarlarkValue, IEquatable<StarlarkBytes>
{
    public StarlarkBytes(byte[] bytes)
    {
        Bytes = bytes;
    }

    public byte[] Bytes { get; }

    public override string TypeName => "bytes";
    public override bool IsTruthy => Bytes.Length != 0;

    public bool Equals(StarlarkBytes? other)
    {
        if (other == null || other.Bytes.Length != Bytes.Length)
        {
            return false;
        }

        for (var i = 0; i < Bytes.Length; i++)
        {
            if (Bytes[i] != other.Bytes[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is StarlarkBytes other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (var i = 0; i < Bytes.Length; i++)
        {
            hash.Add(Bytes[i]);
        }

        return hash.ToHashCode();
    }
}

public sealed class StarlarkList : StarlarkValue
{
    public StarlarkList(IEnumerable<StarlarkValue> items)
    {
        Items = new List<StarlarkValue>(items);
    }

    public List<StarlarkValue> Items { get; }
    internal int Version { get; private set; }

    internal void MarkMutated() => Version++;

    internal IEnumerable<StarlarkValue> EnumerateWithMutationCheck()
    {
        var version = Version;
        for (var i = 0; i < Items.Count; i++)
        {
            if (version != Version)
            {
                throw new InvalidOperationException("Cannot mutate an iterable during iteration.");
            }

            yield return Items[i];
        }
    }

    public override string TypeName => "list";
    public override bool IsTruthy => Items.Count != 0;

    public override bool Equals(object? obj)
    {
        return obj is StarlarkList other && StarlarkEquality.AreEqual(this, other);
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
        return obj is StarlarkTuple other && StarlarkEquality.AreEqual(this, other);
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
    internal int Version { get; private set; }

    internal void MarkMutated() => Version++;

    internal IEnumerable<StarlarkValue> EnumerateWithMutationCheck()
    {
        var version = Version;
        for (var i = 0; i < Entries.Count; i++)
        {
            if (version != Version)
            {
                throw new InvalidOperationException("Cannot mutate an iterable during iteration.");
            }

            yield return Entries[i].Key;
        }
    }

    public override string TypeName => "dict";
    public override bool IsTruthy => Entries.Count != 0;

    public override bool Equals(object? obj)
    {
        return obj is StarlarkDict other && StarlarkEquality.AreEqual(this, other);
    }

    public override int GetHashCode()
    {
        throw new InvalidOperationException("unhashable type: 'dict'.");
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
    public abstract StarlarkValue Call(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs);

    public override string TypeName => "function";
    public override bool IsTruthy => true;
}

public sealed class StarlarkFunction : StarlarkCallable
{
    public StarlarkFunction(
        string name,
        Func<IReadOnlyList<StarlarkValue>, IReadOnlyDictionary<string, StarlarkValue>, StarlarkValue> invoke,
        bool isBuiltin = false)
    {
        Name = name;
        Invoke = invoke;
        IsBuiltin = isBuiltin;
    }

    public string Name { get; }
    public Func<IReadOnlyList<StarlarkValue>, IReadOnlyDictionary<string, StarlarkValue>, StarlarkValue> Invoke { get; }
    public bool IsBuiltin { get; }

    public override StarlarkValue Call(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs) => Invoke(args, kwargs);
}

public sealed class StarlarkBoundMethod : StarlarkCallable
{
    public StarlarkBoundMethod(
        string name,
        StarlarkValue target,
        Func<StarlarkValue, IReadOnlyList<StarlarkValue>, IReadOnlyDictionary<string, StarlarkValue>, StarlarkValue> invoke)
    {
        Name = name;
        Target = target;
        Invoke = invoke;
    }

    public string Name { get; }
    public StarlarkValue Target { get; }
    public Func<StarlarkValue, IReadOnlyList<StarlarkValue>, IReadOnlyDictionary<string, StarlarkValue>, StarlarkValue> Invoke { get; }

    public override StarlarkValue Call(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs) => Invoke(Target, args, kwargs);
}

public sealed class StarlarkUserFunction : StarlarkCallable
{
    public StarlarkUserFunction(
        string name,
        IReadOnlyList<string> parameters,
        IReadOnlyList<StarlarkValue?> defaults,
        string? varArgsName,
        string? kwArgsName,
        IReadOnlyList<Lokad.Starlark.Syntax.Statement> body,
        StarlarkEnvironment closure)
    {
        Name = name;
        Parameters = parameters;
        Defaults = defaults;
        VarArgsName = varArgsName;
        KwArgsName = kwArgsName;
        Body = body;
        Closure = closure;
    }

    public string Name { get; }
    public IReadOnlyList<string> Parameters { get; }
    public IReadOnlyList<StarlarkValue?> Defaults { get; }
    public string? VarArgsName { get; }
    public string? KwArgsName { get; }
    public IReadOnlyList<Lokad.Starlark.Syntax.Statement> Body { get; }
    public StarlarkEnvironment Closure { get; }

    public override StarlarkValue Call(
        IReadOnlyList<StarlarkValue> args,
        IReadOnlyDictionary<string, StarlarkValue> kwargs)
    {
        if (Defaults.Count != Parameters.Count)
        {
            throw new InvalidOperationException(
                $"Function '{Name}' has inconsistent defaults.");
        }

        var values = new StarlarkValue?[Parameters.Count];
        var positionalCount = Math.Min(args.Count, Parameters.Count);
        for (var i = 0; i < positionalCount; i++)
        {
            values[i] = args[i];
        }

        var varArgsValues = new List<StarlarkValue>();
        if (args.Count > Parameters.Count)
        {
            if (VarArgsName == null)
            {
                throw new InvalidOperationException(
                    $"Function '{Name}' expects {Parameters.Count} arguments but got {args.Count}.");
            }

            for (var i = Parameters.Count; i < args.Count; i++)
            {
                varArgsValues.Add(args[i]);
            }
        }

        StarlarkDict? kwArgsDict = KwArgsName == null
            ? null
            : new StarlarkDict(Array.Empty<KeyValuePair<StarlarkValue, StarlarkValue>>());

        if (kwargs.Count > 0)
        {
            foreach (var pair in kwargs)
            {
                var index = -1;
                for (var i = 0; i < Parameters.Count; i++)
                {
                    if (Parameters[i] == pair.Key)
                    {
                        index = i;
                        break;
                    }
                }
                if (index < 0)
                {
                    if (kwArgsDict == null)
                    {
                        throw new InvalidOperationException(
                            $"Function '{Name}' got an unexpected keyword argument '{pair.Key}'.");
                    }

                    kwArgsDict.Entries.Add(
                        new KeyValuePair<StarlarkValue, StarlarkValue>(
                            new StarlarkString(pair.Key),
                            pair.Value));
                    kwArgsDict.MarkMutated();
                    continue;
                }

                if (index < args.Count || values[index] != null)
                {
                    throw new InvalidOperationException(
                        $"Function '{Name}' got multiple values for argument '{pair.Key}'.");
                }

                values[index] = pair.Value;
            }
        }

        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] == null && Defaults[i] != null)
            {
                values[i] = Defaults[i];
            }

            if (values[i] == null)
            {
                throw new InvalidOperationException(
                    $"Function '{Name}' expects {Parameters.Count} arguments but got {args.Count + kwargs.Count}.");
            }
        }

        var callEnvironment = Closure.CreateChild();
        for (var i = 0; i < Parameters.Count; i++)
        {
            callEnvironment.Set(Parameters[i], values[i]!);
        }

        if (VarArgsName != null)
        {
            callEnvironment.Set(VarArgsName, new StarlarkTuple(varArgsValues));
        }

        if (KwArgsName != null)
        {
            callEnvironment.Set(KwArgsName, kwArgsDict!);
        }

        var evaluator = new ModuleEvaluator();
        callEnvironment.EnterFunctionCall(this);
        try
        {
            return evaluator.ExecuteFunctionBody(Body, callEnvironment);
        }
        finally
        {
            callEnvironment.ExitFunctionCall();
        }
    }
}
