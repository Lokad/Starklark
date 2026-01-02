namespace Lokad.Starlark.Runtime;

public static class StarlarkHash
{
    public static void EnsureHashable(StarlarkValue value)
    {
        switch (value)
        {
            case StarlarkList:
            case StarlarkDict:
                throw new InvalidOperationException(
                    $"unhashable type: '{value.TypeName}'.");
            case StarlarkTuple tuple:
                foreach (var item in tuple.Items)
                {
                    EnsureHashable(item);
                }
                break;
        }
    }
}
