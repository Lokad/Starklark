# Build Lokad.Starlark core interpreter and conformance test harness

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document follows `code:\starlark\.agent\PLANS.md` and must be maintained in accordance with it.

## Purpose / Big Picture

The goal is to deliver an open-source Starlark interpreter in .NET named Lokad.Starlark, with a hermetic default runtime and a clean extension model for host-provided functions and modules. Users should be able to run Starlark scripts, add their own host functions (for example, filesystem access or agent helpers) without modifying the core interpreter, and verify correctness via a portable xUnit conformance suite derived from the Bazel Starlark test suite. The result is a NuGet-ready library (packaging deferred) that targets `net8.0` and `net10.0`, uses Lokad.Parsing for parsing, and can be validated by running tests locally.

## Progress

- [x] (2026-01-02 21:30Z) Captured initial constraints, targets, and test suite scope for Lokad.Starlark.
- [x] (2026-01-02 21:45Z) Scaffolded `Lokad.Starlark.slnx`, core library project, and test project with multi-targeting.
- [ ] Implement lexer/parser using Lokad.Parsing and verify syntax on simple programs (completed: expression parser + basic module statements + if blocks + list/tuple/dict literals + indexing + in/comparison operators; remaining: full Starlark grammar and statement parsing).
- [ ] Implement runtime values, evaluator, and hermetic builtins for core language (completed: basic values + expression evaluator + function calls + module execution; remaining: full value model and builtins).
- [ ] Design and implement extension API for host-provided functions/modules without compromising hermetic defaults.
- [ ] Port Bazel Starlark `test_suite/testdata/go` into xUnit conformance tests with a reusable harness.
- [ ] Extend test suite with non-redundant cases from `java`/`rust` when they cover distinct semantics.
- [ ] Add README/docs for extension model and test harness usage.
- [ ] Final stabilization: run full test suite, review diffs, and prepare for packaging (NuGet metadata deferred).

## Surprises & Discoveries

- Observation: `dotnet test` reports `MSB1009` when passed a `.slnx` file; CLI may not recognize the new solution format.
  Evidence: `MSBUILD : error MSB1009: Project file does not exist. Switch: Lokad.Starlark.slnx`
- Observation: `dotnet test` accepted the tests project only when given an absolute path.
  Evidence: relative `tests\Lokad.Starlark.Tests\Lokad.Starlark.Tests.csproj` failed with `MSB1009`, absolute path succeeded.

## Decision Log

- Decision: Target multi-framework `net8.0` and `net10.0`, unless a `net8.0` constraint blocks critical functionality.
  Rationale: Provide broad compatibility while enabling newer runtime features.
  Date/Author: 2026-01-02 / Codex
- Decision: Use Bazel `test_suite/testdata/go` as the initial conformance baseline, and only add `java`/`rust` tests when they introduce distinct coverage.
  Rationale: Maintain a manageable, non-redundant test suite.
  Date/Author: 2026-01-02 / Codex
- Decision: Default runtime is hermetic (no IO, env, or host access) with an extension API to add host capabilities.
  Rationale: Preserve deterministic core behavior while enabling host-driven features.
  Date/Author: 2026-01-02 / Codex
- Decision: Use NuGet package reference for Lokad.Parsing (no local path references).
  Rationale: Ensure commits are portable and not tied to local paths.
  Date/Author: 2026-01-02 / Codex

## Outcomes & Retrospective

Not completed yet.

## Context and Orientation

The repository currently contains only `AGENTS.md` and `.agent/PLANS.md`. We will create a new solution `code:\starlark\Lokad.Starlark.slnx` using the new solution format, with a core library project and an xUnit test project. The interpreter must rely on Lokad.Parsing for lexing/parsing. The conformance suite is derived from a local clone of `bazelbuild/starlark` (outside this repo) but will be copied into this repo so the test suite is portable. We will not bake any local paths into the source; all test data will live within this repo.

Terms used in this plan:

Hermetic runtime: a default interpreter configuration that has no access to IO, filesystem, environment variables, clocks, or host services unless explicitly provided by the host.

Extension API: a host-facing API for registering additional functions, types, or modules, which the interpreter exposes to Starlark code as globals or modules.

Conformance suite: a set of `.star` programs and expected outcomes that validate interpreter semantics.

## Plan of Work

We begin by scaffolding the solution and projects. Create `Lokad.Starlark.slnx` with two projects: `src\Lokad.Starlark\Lokad.Starlark.csproj` (class library) and `tests\Lokad.Starlark.Tests\Lokad.Starlark.Tests.csproj` (xUnit). Both projects target `net8.0` and `net10.0`. The core library references Lokad.Parsing via NuGet. The test project references the core library and xUnit packages.

Next, implement a minimal end-to-end pipeline: tokenizer, parser, AST, evaluator, and value model, all using Lokad.Parsing for parsing. The first milestone should be capable of parsing and evaluating a small Starlark program with arithmetic, literals, and function calls to builtins. Keep the hermetic default by providing only spec-aligned builtins (e.g., `len`, `range`, `print` should be carefully considered; if `print` is included, route it to a host-provided sink with a default that collects output without IO).

Then design the extension API. The core should expose a `StarlarkEnvironment` (or similarly named) object that holds:

1. A dictionary of global bindings (string to `IStarlarkValue`).
2. A collection of modules, each addressable by a name and providing their own globals.
3. Optional host services (e.g., an output sink for `print`) provided via interfaces, with safe defaults that do nothing or capture output in memory.

The extension API must allow the host to register:

1. Native functions exposed as Starlark callables.
2. Native values or modules without modifying the core interpreter.

The interpreter should accept a `StarlarkEnvironment` to execute a file or script, leaving hermetic defaults intact when the host provides none.

After the interpreter is functional, implement the test harness. Copy `bazelbuild/starlark/test_suite/testdata/go` into `tests\Lokad.Starlark.Tests\TestData\Conformance\go` with a short README describing origin and license. Build a test runner that loads each `.star` program, executes it with the hermetic environment (plus any test helper module required), and asserts outputs/errors against expectations encoded in the test files (we will define a simple expectation format as part of the harness). Ensure the harness is deterministic and does not rely on timestamps or filesystem.

Then evaluate `test_suite/testdata/java` and `test_suite/testdata/rust` for distinct behavior coverage. If a test is new and not redundant, add it to the suite with a brief note on why it was included. Keep the total test count reasonable to avoid excessive test time.

Finally, add developer documentation in `README.md` describing how to extend the interpreter and how to run the conformance tests. Defer NuGet packaging metadata until the interpreter is stable and tests are green.

## Concrete Steps

All commands should be run from `code:\starlark` unless specified otherwise.

1. Create the solution and projects.
   - Run `dotnet new sln -n Lokad.Starlark --format slnx`.
   - Run `dotnet new classlib -n Lokad.Starlark -o src\Lokad.Starlark`.
   - Run `dotnet new xunit -n Lokad.Starlark.Tests -o tests\Lokad.Starlark.Tests`.
   - Add project references and target frameworks in the csproj files.

2. Add package references.
   - Add NuGet reference to Lokad.Parsing in `src\Lokad.Starlark\Lokad.Starlark.csproj`.
   - Add xUnit packages in the test project.

3. Implement the parser and evaluator.
   - Define AST types in `src\Lokad.Starlark\Syntax\`.
   - Implement tokenization and parsing using Lokad.Parsing in `src\Lokad.Starlark\Parsing\`.
   - Implement evaluator and value types in `src\Lokad.Starlark\Runtime\`.

4. Add extension API.
   - Define interfaces in `src\Lokad.Starlark\Host\`.
   - Implement `StarlarkEnvironment` and function registration.

5. Add conformance test data and harness.
   - Copy test data into `tests\Lokad.Starlark.Tests\TestData\Conformance\go`.
   - Implement test runner in `tests\Lokad.Starlark.Tests\Conformance\`.

6. Validate.
   - Run `dotnet test --tl:off --nologo -v minimal`.

Expected outputs should show passing tests and no warnings about missing test data.

## Validation and Acceptance

Acceptance is achieved when a user can run a Starlark script with the hermetic runtime, register a custom function, and pass the conformance suite.

Concrete checks:

1. Run `dotnet test --tl:off --nologo -v minimal` and see all tests pass.
2. Add a small test that registers a custom host function (e.g., `add(a, b)`) and verify a Starlark script can call it.
3. Confirm conformance tests execute deterministically without external IO or filesystem access.

## Idempotence and Recovery

The steps are safe to re-run. If test data is copied multiple times, replace the target directory to avoid duplicates. If a step fails, fix the issue and re-run the same command; no destructive operations are required.

## Artifacts and Notes

When test data is copied, include a short README in `tests\Lokad.Starlark.Tests\TestData\Conformance\README.md` noting the origin and license. Provide a short example of a test run:

    PS> dotnet test --tl:off --nologo -v minimal
    Passed!  - Failed: 0, Passed: <N>, Skipped: 0, Total: <N>, Duration: <T>

## Interfaces and Dependencies

The core library must expose the following minimum surface:

1. `Lokad.Starlark.StarlarkEnvironment`
   - Holds globals and modules; can be instantiated empty to remain hermetic.
2. `Lokad.Starlark.StarlarkFunction` (or equivalent)
   - Represents a host-callable function with a name and signature.
3. `Lokad.Starlark.StarlarkValue` base type or interface for runtime values.
4. `Lokad.Starlark.StarlarkInterpreter`
   - Methods: `ExecuteFile(string path, StarlarkEnvironment env)` and `Execute(string source, StarlarkEnvironment env)`.

The interpreter should accept optional host services such as:

1. `IStarlarkOutputSink` (or similar) for `print`.
2. `IStarlarkModule` for module registration.

Dependencies:

1. Lokad.Parsing via NuGet (no local paths).
2. xUnit for tests.

Plan updates must be recorded in the Decision Log and Progress sections, and a note must be added at the end of this document whenever the plan is revised.

Plan revisions:

- Initial version created to capture constraints, milestones, and validation steps.
- Updated Progress after scaffolding the solution and projects.
- Noted partial completion for the lexer/parser milestone after adding a minimal expression parser.
- Recorded `dotnet test` failure when targeting `.slnx`.
- Noted partial completion for runtime values after adding expression evaluation support.
- Recorded that `dotnet test` requires absolute project paths in this environment.
- Noted partial completion for runtime values after adding function call support.
- Noted parser progress after adding basic module statements.
- Noted runtime progress after adding module execution support.
- Noted parser/runtime progress after adding if-statement blocks.
- Noted parser/runtime progress after adding list literal support.
- Noted parser/runtime progress after adding tuple literal support.
- Noted parser/runtime progress after adding dict literal support.
- Noted parser/runtime progress after adding index expressions.
- Noted parser/runtime progress after adding `in` operator.
- Noted parser/runtime progress after adding comparison operators.
