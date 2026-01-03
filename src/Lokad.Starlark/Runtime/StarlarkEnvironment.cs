using System;
using System.Collections.Generic;

namespace Lokad.Starlark.Runtime;

public sealed class StarlarkEnvironment
{
    private readonly Dictionary<string, StarlarkValue> _locals;

    public StarlarkEnvironment? Parent { get; }

    public IDictionary<string, StarlarkValue> Globals => Parent == null ? _locals : Parent.Globals;

    private readonly Dictionary<string, IReadOnlyDictionary<string, StarlarkValue>> _modules;

    public IDictionary<string, IReadOnlyDictionary<string, StarlarkValue>> Modules =>
        Parent == null ? _modules : Parent.Modules;

    public StarlarkEnvironment()
        : this(null)
    {
    }

    private StarlarkEnvironment(StarlarkEnvironment? parent)
    {
        Parent = parent;
        _locals = new Dictionary<string, StarlarkValue>(StringComparer.Ordinal);
        _modules = parent == null
            ? new Dictionary<string, IReadOnlyDictionary<string, StarlarkValue>>(StringComparer.Ordinal)
            : parent._modules;

        if (parent == null)
        {
            StarlarkBuiltins.Register(this);
        }
    }

    public StarlarkEnvironment CreateChild()
    {
        return new StarlarkEnvironment(this);
    }

    public void Set(string name, StarlarkValue value)
    {
        _locals[name] = value;
    }

    public void AddFunction(
        string name,
        Func<IReadOnlyList<StarlarkValue>, IReadOnlyDictionary<string, StarlarkValue>, StarlarkValue> implementation)
    {
        _locals[name] = new StarlarkFunction(name, implementation);
    }

    public void AddModule(string name, IReadOnlyDictionary<string, StarlarkValue> members)
    {
        Modules[name] = members;
    }

    public bool TryGet(string name, out StarlarkValue value)
    {
        for (var scope = this; scope != null; scope = scope.Parent)
        {
            if (scope._locals.TryGetValue(name, out var found))
            {
                value = found ?? StarlarkNone.Instance;
                return true;
            }
        }

        value = StarlarkNone.Instance;
        return false;
    }
}
