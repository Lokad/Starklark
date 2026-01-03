using System.Collections.Generic;
using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

internal static class FunctionParameterEvaluator
{
    internal static (
        IReadOnlyList<string> Names,
        IReadOnlyList<StarlarkValue?> Defaults,
        string? VarArgsName,
        string? KwArgsName) Evaluate(
        IReadOnlyList<FunctionParameter> parameters,
        StarlarkEnvironment environment)
    {
        var names = new List<string>(parameters.Count);
        var defaults = new List<StarlarkValue?>(parameters.Count);
        var evaluator = new ExpressionEvaluator();
        string? varArgsName = null;
        string? kwArgsName = null;

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            if (parameter.Kind == ParameterKind.VarArgs)
            {
                varArgsName = parameter.Name;
                continue;
            }

            if (parameter.Kind == ParameterKind.KwArgs)
            {
                kwArgsName = parameter.Name;
                continue;
            }

            names.Add(parameter.Name);
            if (parameter.Default != null)
            {
                defaults.Add(evaluator.Evaluate(parameter.Default, environment));
            }
            else
            {
                defaults.Add(null);
            }
        }

        return (names, defaults, varArgsName, kwArgsName);
    }
}
