using System;
using System.Collections.Generic;

namespace Lokad.Starlark.Runtime;

public sealed class StarlarkEnvironment
{
    public IDictionary<string, StarlarkValue> Globals { get; } =
        new Dictionary<string, StarlarkValue>(StringComparer.Ordinal);

    public void Set(string name, StarlarkValue value)
    {
        Globals[name] = value;
    }

    public void AddFunction(string name, Func<IReadOnlyList<StarlarkValue>, StarlarkValue> implementation)
    {
        Globals[name] = new StarlarkFunction(name, implementation);
    }
}
