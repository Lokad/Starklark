using System;
using System.Collections.Generic;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

public sealed class StarlarkEnvironment
{
    private readonly Dictionary<string, StarlarkValue> _locals;
    private readonly IReadOnlySet<string>? _declaredLocals;
    private readonly Stack<IReadOnlyList<Statement>> _callStack;
    private ExecutionGuard _guard;

    public StarlarkEnvironment? Parent { get; }

    public IDictionary<string, StarlarkValue> Globals => Parent == null ? _locals : Parent.Globals;

    private readonly Dictionary<string, IReadOnlyDictionary<string, StarlarkValue>> _modules;

    public IDictionary<string, IReadOnlyDictionary<string, StarlarkValue>> Modules =>
        Parent == null ? _modules : Parent.Modules;

    public StarlarkEnvironment()
        : this(null, null)
    {
    }

    private StarlarkEnvironment(StarlarkEnvironment? parent, IReadOnlySet<string>? declaredLocals)
    {
        Parent = parent;
        _locals = new Dictionary<string, StarlarkValue>(StringComparer.Ordinal);
        _modules = parent == null
            ? new Dictionary<string, IReadOnlyDictionary<string, StarlarkValue>>(StringComparer.Ordinal)
            : parent._modules;
        _callStack = parent == null
            ? new Stack<IReadOnlyList<Statement>>()
            : parent._callStack;
        _declaredLocals = declaredLocals;
        _guard = parent?._guard ?? ExecutionGuard.None;

        if (parent == null)
        {
            StarlarkBuiltins.Register(this);
        }
    }

    public StarlarkEnvironment CreateChild(IReadOnlySet<string>? declaredLocals = null)
    {
        return new StarlarkEnvironment(this, declaredLocals);
    }

    internal ExecutionGuard Guard => Parent?.Guard ?? _guard;

    internal ExecutionGuard SwapGuard(ExecutionGuard guard)
    {
        if (Parent != null)
        {
            return Parent.SwapGuard(guard);
        }

        var previous = _guard;
        _guard = guard;
        return previous;
    }

    public void Set(string name, StarlarkValue value)
    {
        _locals[name] = value;
    }

    public void AddFunction(
        string name,
        Func<IReadOnlyList<StarlarkValue>, IReadOnlyDictionary<string, StarlarkValue>, StarlarkValue> implementation,
        bool isBuiltin = false)
    {
        _locals[name] = new StarlarkFunction(name, implementation, isBuiltin);
    }

    public void AddModule(string name, IReadOnlyDictionary<string, StarlarkValue> members)
    {
        Modules[name] = members;
    }

    internal void EnterFunctionCall(StarlarkUserFunction function)
    {
        foreach (var body in _callStack)
        {
            if (ReferenceEquals(body, function.Body))
            {
                throw new InvalidOperationException(
                    $"function {function.Name} called recursively");
            }
        }

        _callStack.Push(function.Body);
    }

    internal void ExitFunctionCall()
    {
        _callStack.Pop();
    }

    public bool TryGet(string name, out StarlarkValue value)
    {
        return TryGetDetailed(name, out value) == LookupResult.Found;
    }

    internal LookupResult TryGetDetailed(string name, out StarlarkValue value)
    {
        for (var scope = this; scope != null; scope = scope.Parent)
        {
            if (scope._locals.TryGetValue(name, out var found))
            {
                value = found ?? StarlarkNone.Instance;
                return LookupResult.Found;
            }

            if (scope._declaredLocals != null && scope._declaredLocals.Contains(name))
            {
                value = StarlarkNone.Instance;
                return LookupResult.ReferencedBeforeAssignment;
            }
        }

        value = StarlarkNone.Instance;
        return LookupResult.NotFound;
    }
}

internal enum LookupResult
{
    Found,
    NotFound,
    ReferencedBeforeAssignment
}
