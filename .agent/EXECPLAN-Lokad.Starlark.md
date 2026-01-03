# Build Lokad.Starlark core interpreter and conformance test harness

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This document follows `code:\starlark\.agent\PLANS.md` and must be maintained in accordance with it.

## Purpose / Big Picture

The goal is to deliver an open-source Starlark interpreter in .NET named Lokad.Starlark, with a hermetic default runtime and a clean extension model for host-provided functions and modules. Users should be able to run Starlark scripts, add their own host functions (for example, filesystem access or agent helpers) without modifying the core interpreter, and verify correctness via a portable xUnit conformance suite derived from the Bazel Starlark test suite. The result is a NuGet-ready library (packaging deferred) that targets `net8.0` and `net10.0`, uses Lokad.Parsing for parsing, and can be validated by running tests locally.

## Progress

- [x] (2026-01-02 21:30Z) Captured initial constraints, targets, and test suite scope for Lokad.Starlark.
- [x] (2026-01-02 21:45Z) Scaffolded `Lokad.Starlark.slnx`, core library project, and test project with multi-targeting.
- [ ] Implement lexer/parser using Lokad.Parsing and verify syntax on simple programs (completed: expression parser + basic module statements + if/elif blocks + for loops + function definitions + return/break/continue/pass + load statements + tuple assignment targets + for loop tuple targets + augmented assignments + list/tuple/dict literals + indexing + slicing + in/not-in/comparison operators + conditional expressions + // and % operators + string escape handling + attribute access + keyword call arguments + *args/**kwargs + list/dict comprehensions + default parameter parsing; remaining: full Starlark grammar and statement parsing).
- [ ] Implement runtime values, evaluator, and hermetic builtins for core language (completed: basic values + range type + expression evaluator + function calls + module execution + function bodies + loops + for loop tuple targets + len/range/list/tuple/bool/any/all/dict/str/int/float/type/repr/sorted/reversed/min/max/enumerate/zip/dir/getattr/hasattr/fail builtins + list/tuple/dict equality with hashability checks + string % formatting + attribute-based methods + keyword arguments + *args/**kwargs + list/dict comprehensions + default argument binding + built-in function rendering + string non-iterability in loops/comprehensions; remaining: full value model and builtins).
- [ ] Design and implement extension API for host-provided functions/modules without compromising hermetic defaults.
- [ ] Port Bazel Starlark `test_suite/testdata/go` into xUnit conformance tests with a reusable harness (started: added initial harness, loop/comprehension subset, error-pattern cases, control/function subsets, list/dict/string/int subsets, bool/tuple subsets, builtins subset, misc subset, plus selected java/rust subsets for strings/formatting).
- [ ] Extend test suite with non-redundant cases from `java`/`rust` when they cover distinct semantics.
- [ ] Add README/docs for extension model and test harness usage.
- [ ] Final stabilization: run full test suite, review diffs, and prepare for packaging (NuGet metadata deferred).
- [x] (2026-01-03 13:00Z) Added mutation-during-iteration checks for list/dict and conformance coverage.
- [x] (2026-01-03 13:15Z) Added recursion detection for user functions with conformance coverage.
- [x] (2026-01-03 13:25Z) Added boolean ordering comparisons with conformance coverage.
- [x] (2026-01-03 13:40Z) Aligned numeric equality, hashing, and comparisons for int/float mix.
- [x] (2026-01-03 14:05Z) Added NaN comparison coverage and recursion guards for equality/`repr`.
- [x] (2026-01-03 14:20Z) Added java/rust conformance slices and modulo subsets.
- [x] (2026-01-03 15:05Z) Added bytes literals/builtin plus missing string methods with conformance coverage.
- [x] (2026-01-03 15:30Z) Corrected raw bytes conformance expectation for `br` literals.
- [x] (2026-01-03 16:00Z) Added hash builtin, whitespace-aware string splitting, and missing string methods (`index`, `rindex`, `isalnum`, `isalpha`, `isdigit`, `removeprefix`, `removesuffix`) plus updated conformance coverage.
- [x] (2026-01-03 16:15Z) Added zero-argument `bool()`/`float()` behavior and conformance coverage for the builtins subset.
- [x] (2026-01-03 16:40Z) Added bitwise/shift operators (including unary `+`/`~`) and dict union semantics with conformance coverage.

## Spec Compliance Checklist

This checklist is the source of truth for the remaining work needed to reach full Starlark spec compliance.

Parsing & Syntax:
- Support single-quoted strings and raw string literals (`r"..."` / `r'...'`).
- Parse empty tuple literal `()` and disambiguate parenthesized expressions.
- Allow conditional expressions in all expression positions (including call arguments).
- Ensure `load` statements and string literal forms match the spec grammar.
- Add bytes literals (`b"..."`, `br"..."`, `rb"..."`) and triple-quoted strings/bytes. (done)

Core Semantics:
- Implement boolean ordering comparisons (`False < True`, etc.) per spec ordering rules. (done)
- Enforce mutation-during-iteration restrictions for list/dict (and comprehensions) where required. (done)
- Implement recursion detection (dynamic call stack) consistent with Starlark rules. (done)
- Align hashing, equality, and type ordering to spec (including cross-type comparison restrictions). (done)
- Implement bytes type semantics (indexing, slicing, equality, hashability, formatting). (done)

Builtins & Methods:
- Complete core builtins per spec (including `len`, `range`, `type`, `repr`, `bool`, `list`, `tuple`, `dict`, `sorted`, `reversed`, `min`, `max`, `enumerate`, `zip`, `any`, `all`, `dir`, `getattr`, `hasattr`, `fail`).
- Complete string/list/dict methods per spec, including argument validation and error messages.
- Verify `%` formatting and `str.format` behaviors with spec-aligned edge cases.
- Add `bytes` builtin, `bytes.elems`, and missing string methods (`capitalize`, `elems`, `islower`, `istitle`, `isupper`, `isspace`). (done)

Diagnostics & Error Behavior:
- Normalize error types/messages to spec expectations where tests rely on them.
- Add conformance coverage for error paths, not just success cases.

## Compliance Plan

1. Close parsing gaps (single-quoted/raw strings, empty tuple, conditional expression precedence).
2. Implement runtime semantics: mutation-during-iteration, recursion detection, bool ordering.
3. Finish builtins/methods per spec and add targeted tests for each.
4. Expand conformance suite with spec-driven edge cases and ensure deterministic errors.
5. Stabilize and run the full suite across net8/net10.
6. Create a root-level `README.md` with project overview and a C# usage snippet (NuGet name: `Lokad.Starlark`).

## Execution Mode

Proceed unattended through the compliance plan without pausing for confirmation between steps, unless blocked by missing requirements or ambiguities that require user input. Continue until full Starlark spec compliance is achieved without re-asking for confirmation.

## Surprises & Discoveries

- Observation: `dotnet test` reports `MSB1009` when passed a `.slnx` file; CLI may not recognize the new solution format.
  Evidence: `MSBUILD : error MSB1009: Project file does not exist. Switch: Lokad.Starlark.slnx`
- Observation: `dotnet test` accepted the tests project only when given an absolute path.
  Evidence: relative `tests\Lokad.Starlark.Tests\Lokad.Starlark.Tests.csproj` failed with `MSB1009`, absolute path succeeded.
- Observation: Raw bytes literals (`br"..."`) keep backslashes literal; conformance test data had to reflect that.
  Evidence: `br"\\n"` yields bytes `[92, 92, 110]` while `br"\n"` yields `[92, 110]`.

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
- Noted parser progress after adding string escape handling.
- Noted parser/runtime progress after adding for loops, function definitions, and control flow statements.
- Noted parser/runtime progress after adding load statements and module imports.
- Noted parser/runtime progress after adding conditional expressions, not-in, floor division, and modulo.
- Noted parser/runtime progress after adding tuple targets and index assignment support.
- Noted parser/runtime progress after adding augmented assignments and list/string concatenation support.
- Noted parser/runtime progress after adding slice index support.
- Noted runtime progress after adding range values and core builtins.
- Noted runtime progress after adding hashability checks and collection equality semantics.
- Noted runtime progress after adding core conversion builtins and formatting helpers.
- Noted runtime progress after adding string percent formatting support.
- Noted parser/runtime progress after adding attribute access, keyword arguments, and core string/list/dict methods.
- Noted parser/runtime progress after adding default parameters, extended builtins, and string non-iterability.
- Noted parser/runtime progress after adding *args/**kwargs support.
- Noted parser/runtime progress after adding list/dict comprehensions and rejecting string iteration in loops.
- Noted parser/runtime progress after allowing tuple targets in for loops.
- Noted conformance harness start with loop/comprehension subset from go suite.
- Added error-pattern conformance cases for assert helper failures.
- Added control-flow and function-behavior conformance subsets from go suite.
- Added list/dict/string/int conformance subsets from go suite.
- Added bool/tuple conformance subsets from go suite.
- Added builtins conformance subset from go suite.
- Added misc conformance subset from go suite.
- Added java/rust conformance subsets for string and regression coverage.
- Added java/rust conformance subsets for string formatting behavior.
- Added spec compliance checklist and compliance plan.
- Set execution mode to proceed unattended through compliance work.
- Enabled parsing for single-quoted/raw strings, empty tuples, and conditional expressions in call arguments.
- Added mutation-during-iteration enforcement with conformance coverage.
- Added recursion detection for Starlark user functions with conformance coverage.
- Added boolean ordering comparisons with conformance coverage.
- Aligned numeric equality, hashing, and comparisons for mixed int/float values.
- Added NaN comparison coverage and recursion guards for equality/`repr`.
- Added java list/string slice subsets plus rust int modulo coverage.
- Added bytes literals/builtin and string method coverage (capitalize, is* variants, elems).
