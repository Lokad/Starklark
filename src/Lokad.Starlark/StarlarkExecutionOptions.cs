using System.Threading;

namespace Lokad.Starlark;

public sealed class StarlarkExecutionOptions
{
    public long? StepBudget { get; init; }
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}
