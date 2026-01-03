namespace Lokad.Starlark.Tests.Conformance;

public sealed record ConformanceCase(
    string Name,
    string Source,
    string? ExpectedErrorPattern);
