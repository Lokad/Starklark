using System.Collections.Generic;

namespace Lokad.Starlark.Syntax;

public sealed record StarlarkModule(IReadOnlyList<Statement> Statements);
