# Interpreter Refinements

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into this repo at `.agent/PLANS.md`. This ExecPlan follows those requirements and must remain compliant with them.

## Purpose / Big Picture

The goal is to make the Starlark runtime easier to integrate and more reliable by improving error reporting, reducing duplicated logic, improving dict/set lookup performance, and adding optional execution safeguards. After this work, a host can catch a dedicated runtime exception type, see error locations when available, rely on more consistent operator behavior, enjoy better dict/set performance without changing semantics, and optionally limit evaluation steps to avoid runaway scripts. Success is visible through the existing test suite and by exercising small scripts that trigger errors or large loops.

## Progress

- [x] (2026-01-03 20:58Z) Create a dedicated runtime exception type and convert key runtime error sites to use it.
- [x] (2026-01-03 21:14Z) Attach source spans to runtime errors where syntax nodes provide locations.
- [ ] (2026-01-03 20:40Z) Consolidate augmented assignment operator logic with shared binary operator handling.
- [ ] (2026-01-03 20:40Z) Add indexed storage for dict/set to improve lookup performance without changing order or semantics.
- [ ] (2026-01-03 20:40Z) Add an argument binder helper and migrate repetitive builtin/method argument validation.
- [ ] (2026-01-03 20:40Z) Add optional execution guards (step budget and cancellation) wired through the interpreter.

## Surprises & Discoveries

- Observation: None yet. This section will be updated if unexpected behavior or tradeoffs are discovered.

## Decision Log

- Decision: Sequence the work so error handling improvements come before performance or API refactors.
  Rationale: Error classification and diagnostics impact all subsequent changes and reduce debugging time.
  Date/Author: 2026-01-03 / Codex

## Outcomes & Retrospective

Not started yet. This section will summarize what changed and any gaps or lessons when milestones complete.

## Context and Orientation

The runtime entry point is `src/Lokad.Starlark/StarlarkInterpreter.cs`, which parses and evaluates modules and expressions through `ExpressionEvaluator` and `ModuleEvaluator`. Runtime values and collections are in `src/Lokad.Starlark/Runtime/StarlarkValue.cs` with list-backed dict/set implementations. Builtins and methods live in `src/Lokad.Starlark/Runtime/StarlarkBuiltins.cs` and `src/Lokad.Starlark/Runtime/StarlarkMethods.cs`. Parsing and syntax nodes live under `src/Lokad.Starlark/Parsing` and `src/Lokad.Starlark/Syntax`. Tests are in `tests/Lokad.Starlark.Tests`, and the conformance suite runs via `dotnet test`.

## Plan of Work

First, introduce a dedicated `StarlarkRuntimeException` and a small helper to construct runtime errors, then update the evaluators to use it for user-facing errors (not internal invariants). Next, carry `SourceSpan` data into runtime error creation for expressions and statements so error messages can include locations. Then, consolidate binary operator logic for augmented assignment by reusing or extracting the existing operator evaluation logic to avoid drift. After that, evolve `StarlarkDict` and `StarlarkSet` to maintain an internal hash index so lookups are O(1) while preserving insertion order and mutation checks. Then, implement a minimal argument binder helper to replace duplicated positional/keyword validation in a few representative builtins and methods, making error messages more consistent. Finally, add optional execution guards by threading a `StarlarkExecutionOptions` object (step budget and cancellation token) through `StarlarkInterpreter` into evaluators, and instrument evaluation loops to decrement or check the budget without changing default behavior. When each commit passes build and tests, proceed immediately to the next planned commit without waiting for approval.

## Concrete Steps

Work from the repository root `C:\Users\JoannesVermorel\code\starlark`. For each commit, make the changes described in the Plan of Work, then run:

    dotnet build --tl:off --nologo -v minimal
    dotnet test --tl:off --nologo -v minimal

Expect tests to pass with the same count as before (currently 215 on both net8.0 and net10.0). For error changes, validate by running a small script that triggers a runtime error and confirm the message includes the new exception type and location when applicable.

## Validation and Acceptance

Each milestone is accepted when the build and tests pass, and the new behavior is observable. For runtime errors, a small script like:

    x = 1 / 0

should raise the new runtime exception type, and once span wiring is complete, the error should include a location that points to the division expression. For the execution guard, a script with an infinite loop should be stopped with a clear “budget exceeded” error when the budget is set, and remain unchanged with default options.

## Idempotence and Recovery

Edits are additive and can be re-run safely. If a change partially applies and breaks tests, revert only the files touched in that commit and re-apply the step carefully. Because each step is separated into a commit with tests, recovery is to reset to the last passing commit and continue.

## Artifacts and Notes

Store short test outputs and error examples here as indented snippets when they are generated. Keep them brief, such as a one-line exception message or the final test summary.

- 2026-01-03: `dotnet test` summary
  - Passed: 215, Failed: 0, Skipped: 0 (net8.0)
  - Passed: 215, Failed: 0, Skipped: 0 (net10.0)

## Interfaces and Dependencies

Introduce `StarlarkRuntimeException` under `src/Lokad.Starlark/Runtime` and keep it public so hosts can catch it. Add a helper in `src/Lokad.Starlark/Runtime` (e.g., `RuntimeErrors`) that accepts a message and optional `SourceSpan` from `src/Lokad.Starlark/Syntax`. If introducing `StarlarkExecutionOptions`, define it in `src/Lokad.Starlark` and thread it through `StarlarkInterpreter`, `ExpressionEvaluator`, and `ModuleEvaluator`. Keep existing public APIs source compatible where possible by adding optional parameters or overloads.

## Change Log

2026-01-03: Initial plan written using `.agent/PLANS.md` format, replacing the earlier non-compliant draft.
2026-01-03: Explicitly documented auto-progression to the next commit after green validation.
