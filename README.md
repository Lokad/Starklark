# Lokad.Starlark

Lokad.Starlark is a .NET implementation of the Starlark language with a hermetic core runtime and a host extension model. The interpreter targets `net8.0` and `net10.0`, uses Lokad.Parsing for the parser, and ships with a portable conformance suite derived from the Bazel Starlark tests.

Planned NuGet package name: `Lokad.Starlark` (unpublished for now).

## Usage

```csharp
using Lokad.Starlark;
using Lokad.Starlark.Runtime;

var interpreter = new StarlarkInterpreter();
var environment = new StarlarkEnvironment();

environment.AddFunction(
    "add",
    (args, kwargs) =>
    {
        var left = (StarlarkInt)args[0];
        var right = (StarlarkInt)args[1];
        return new StarlarkInt(left.Value + right.Value);
    },
    isBuiltin: true);

interpreter.ExecuteModule(
    "result = add(2, 3)",
    environment);
```

## Conformance Tests

Conformance scripts live under `tests/Lokad.Starlark.Tests/TestData/Conformance`, split into `go`, `java`, and `rust` subsets. Each file contains multiple cases separated by `---`, and error expectations are annotated with `### (regex)` on the failing statement.

## Conformance Status

The versioned conformance subsets (`tests/Lokad.Starlark.Tests/TestData/Conformance/{go,java,rust}`) are currently green. This status reflects the curated, orthogonal subset we maintain in this repo.

Run the full suite with:

```powershell
dotnet test --tl:off --nologo -v minimal
```

## CLI Demo

The CLI is a local demo utility (not packaged in the NuGet) with two modes: a hermetic REPL and a script runner. It injects a `print` helper to demonstrate host extensibility.

Start the REPL:

```powershell
dotnet run --project tools/Lokad.Starlark.Cli -- repl
```

Run a script file:

```powershell
dotnet run --project tools/Lokad.Starlark.Cli -- run path\to\script.star
```
