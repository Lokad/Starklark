using System;
using System.Diagnostics.CodeAnalysis;
using Lokad.Parsing;

namespace Lokad.Starlark.Runtime;

internal static class RuntimeErrors
{
    public static StarlarkRuntimeException Create(string message, SourceSpan? location = null, Exception? inner = null)
    {
        return new StarlarkRuntimeException(message, location, inner);
    }

    [DoesNotReturn]
    public static void Throw(string message, SourceSpan? location = null, Exception? inner = null)
    {
        throw Create(message, location, inner);
    }

    [DoesNotReturn]
    public static T Fail<T>(string message, SourceSpan? location = null, Exception? inner = null)
    {
        throw Create(message, location, inner);
    }
}
