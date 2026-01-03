using System.Threading;
using Lokad.Starlark;

namespace Lokad.Starlark.Runtime;

internal sealed class ExecutionGuard
{
    internal static readonly ExecutionGuard None = new ExecutionGuard(null);

    private long? _remaining;
    private readonly CancellationToken _token;

    public ExecutionGuard(StarlarkExecutionOptions? options)
    {
        if (options != null)
        {
            _remaining = options.StepBudget;
            _token = options.CancellationToken;
        }
        else
        {
            _remaining = null;
            _token = CancellationToken.None;
        }
    }

    internal void Check()
    {
        if (_token.IsCancellationRequested)
        {
            RuntimeErrors.Throw("Execution cancelled.");
        }

        if (_remaining.HasValue)
        {
            if (_remaining.Value <= 0)
            {
                RuntimeErrors.Throw("Execution step budget exceeded.");
            }

            _remaining--;
        }
    }
}
