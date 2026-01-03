using System;
using Lokad.Parsing;

namespace Lokad.Starlark.Runtime;

public sealed class StarlarkRuntimeException : Exception
{
    public SourceSpan? Location { get; }

    public StarlarkRuntimeException(string message, SourceSpan? location = null, Exception? inner = null)
        : base(message, inner)
    {
        Location = location;
    }
}
