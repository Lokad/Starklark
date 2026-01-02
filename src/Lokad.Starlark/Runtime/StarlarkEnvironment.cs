using System.Collections.Generic;

namespace Lokad.Starlark.Runtime;

public sealed class StarlarkEnvironment
{
    public IDictionary<string, StarlarkValue> Globals { get; } =
        new Dictionary<string, StarlarkValue>(StringComparer.Ordinal);
}
