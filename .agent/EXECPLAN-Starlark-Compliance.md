# Reach full Starlark conformance for Lokad.Starlark

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan must be maintained in accordance with `/.agent/PLANS.md`.

## Purpose / Big Picture

The goal is to bring the hermetic Lokad.Starlark interpreter to full Starlark conformance, using the bazelbuild Starlark test suite as the reference. After this work, the local conformance tests should pass without exceptions, and a staged import of the upstream tests should confirm that the interpreter matches the Starlark specification in behavior and error messages. This provides a stable, predictable core for downstream extensions.

## Progress

- [x] (2026-01-03 22:30Z) Collected a preliminary gap inventory by running the upstream conformance suite locally and noting failure categories.
- [x] (2026-01-03 22:36Z) Mirrored upstream testdata into a local `_fullsuite` folder and captured the baseline counts for the net10.0 run.
- [ ] (2026-01-03 23:50Z) Parser now handles semicolons and trailing commas inside brackets via a `TrailingComma` token; single-item tuples parse again.
- [ ] Close parser and syntax gaps that block spec tests (semicolons, trailing commas, slice parsing, call parsing).
- [ ] Close runtime semantics gaps (scope rules, mutation detection, comparison and containment rules).
- [ ] Close builtin behavior gaps (int parsing, sorted/min/max/reversed/range, string helpers).
- [ ] Close string formatting gaps for both `%` and `.format` and align error messages to expected patterns.
- [ ] Prune and curate the in-repo conformance suite to keep it lean while still covering distinct angles.
- [ ] Validate full conformance and update the plan and checklist to reflect completion.

## Surprises & Discoveries

- Observation: The upstream suite surfaced many failures that are message-matching deltas rather than semantic mismatches (case sensitivity, wording, and phrasing).
  Evidence: Local full-suite run reported roughly 175 failures out of 570 cases, with repeated mismatches like "division by zero" vs "Division by zero" and "referenced before assignment" vs "Undefined identifier".
- Observation: Some parser errors are caused by missing grammar conveniences (semicolon separators, trailing commas, None in slices).
  Evidence: `equality.star` and `function.star` cases in the upstream suite fail during parsing rather than evaluation.
- Observation: Builtins differ from Starlark expectations in subtle ways (e.g., `int` base parsing and `sorted` key handling).
  Evidence: `java/int_constructor.star` and `java/sorted.star` cases expect specific error text and type rules that are not currently met.
- Observation: The net10.0 baseline for the full-suite mirror is 395 passing and 175 failing tests.
  Evidence: `dotnet test Lokad.Starlark.slnx --framework net10.0 --no-build` reports "Failed: 175, Passed: 395, Total: 570".
- Observation: After parser updates for semicolons and trailing commas, the net10.0 run now reports 401 passing and 169 failing tests.
  Evidence: `dotnet test tests\\Lokad.Starlark.Tests\\Lokad.Starlark.Tests.csproj --framework net10.0 --no-build` reports "Failed: 169, Passed: 401, Total: 570".

## Decision Log

- Decision: Use the bazelbuild Starlark test suite as the authoritative conformance oracle and align error messages to its expected patterns.
  Rationale: This maximizes compatibility and provides a stable, widely-used external reference.
  Date/Author: 2026-01-03 (Codex)
- Decision: Keep a lean, curated in-repo conformance suite for fast iteration, while relying on a local full-suite mirror for deep validation.
  Rationale: The user asked for a manageable suite and to avoid redundant tests, but full conformance still requires the upstream coverage during development.
  Date/Author: 2026-01-03 (Codex)

## Outcomes & Retrospective

No outcomes yet. This plan has been drafted and work has not started.

## Context and Orientation

Lokad.Starlark is a hermetic Starlark interpreter in `src/Lokad.Starlark`. The runtime and evaluation logic live under `src/Lokad.Starlark/Runtime`, where `ExpressionEvaluator.cs` and `ModuleEvaluator.cs` implement execution, `StarlarkBuiltins.cs` defines builtins, and `RuntimeErrors.cs` constructs error messages. The syntax tree and parser are in `src/Lokad.Starlark/Syntax` and `src/Lokad.Starlark/Parsing`.

Conformance tests live in `tests/Lokad.Starlark.Tests`. The harness in `tests/Lokad.Starlark.Tests/Conformance/ConformanceTests.cs` loads all `*.star` files under `tests/Lokad.Starlark.Tests/TestData/Conformance` and treats embedded expectations as regexes. The test environment is created in `tests/Lokad.Starlark.Tests/Conformance/ConformanceTestEnvironment.cs` and only defines `assert_`, `assert_eq`, and `assert_ne` as builtins. Errors are matched by regex, so message casing and wording must align.

The upstream reference tests are located in the `test_suite/testdata` directory of the bazelbuild Starlark repository. Locally, the developer typically keeps a sibling clone of that repo (for example `../starlark-bazelbuild`), but this plan must not assume absolute paths. The expected workflow is to mirror those testdata files into a local, ignored folder in this repo for validation only.

## Plan of Work

The work proceeds in a sequence of commit-sized milestones, each finishing with a clean build and test run plus a hygiene check (reviewing `git status` and `git show` to ensure only intended changes are present). When a milestone completes successfully, proceed immediately to the next one without asking for approval.

First, align the conformance harness with a local, ignored full-suite mirror. This requires creating an ignored folder such as `tests/Lokad.Starlark.Tests/TestData/Conformance/_fullsuite` and copying upstream `test_suite/testdata` into it. The harness already loads all `.star` files recursively, so this mirror is enough to drive baseline results. This milestone should also document the current failure counts so the work can be tracked.

Second, close parser and syntax gaps revealed by the upstream suite. That includes accepting semicolons between statements, allowing trailing commas in tuple/list/dict literals, accepting `None` in slice indices, and ensuring function call parsing supports long argument lists and line continuation as Starlark expects. Update parser error messages to match expected patterns when syntax is invalid.

Third, align runtime semantics. Scope rules must produce the correct "referenced before assignment" error when reading a local variable before assignment. Mutation during iteration must be detected for dicts and lists per Starlark rules. Containment (`in`) should validate operand types and match error messages. Comparison semantics and truthiness should match spec behavior for `None`, lists, tuples, and dicts.

Fourth, align builtins and methods. This includes `int()` base parsing and its error messages, `sorted()` ordering and key validation, `min`/`max` error reporting, `range()` argument checks, `reversed()` and iteration helpers, and string helper naming differences (supporting `starts_with` where expected). Ensure the conformance environment includes `print` only if the upstream tests require it; otherwise keep it confined to conformance test environment.

Fifth, align string formatting. Implement or adjust `%` formatting and `.format` behavior to match Starlark, including field numbering, brace handling, and detailed error messages. The goal is that upstream `format.star` and `%` formatting tests pass without message mismatches.

Sixth, prune and curate the in-repo conformance suite. Keep the local upstream mirror ignored, then cherry-pick distinct tests from upstream into `tests/Lokad.Starlark.Tests/TestData/Conformance` to maintain a manageable runtime while preserving coverage of unique behaviors.

Finally, validate full compliance by re-running the full upstream mirror, record the results, and update this plan with outcomes and any remaining gaps.

## Concrete Steps

Use the repository root as `C:\Users\JoannesVermorel\code\starlark` for all commands. Replace the upstream repo location with your actual local clone if different.

Create and populate the local full-suite mirror (ignored):

    Set-Location C:\Users\JoannesVermorel\code\starlark
    Copy-Item -Recurse -Force ..\starlark-bazelbuild\test_suite\testdata tests\Lokad.Starlark.Tests\TestData\Conformance\_fullsuite

If the upstream repo is not available, clone it adjacent to this repo and retry the copy:

    Set-Location C:\Users\JoannesVermorel\code
    git clone https://github.com/bazelbuild/starlark.git starlark-bazelbuild

Run the baseline test suite and capture the failing count:

    Set-Location C:\Users\JoannesVermorel\code\starlark
    dotnet test Lokad.Starlark.slnx

After each milestone commit, repeat the build/test and hygiene checks:

    Set-Location C:\Users\JoannesVermorel\code\starlark
    dotnet build Lokad.Starlark.slnx
    dotnet test Lokad.Starlark.slnx
    git status -sb
    git show --stat

## Validation and Acceptance

Acceptance is achieved when all upstream conformance tests in the local mirror pass, with no failures, and the curated in-repo suite still passes. The tests must be run via:

    Set-Location C:\Users\JoannesVermorel\code\starlark
    dotnet test Lokad.Starlark.slnx

Success is defined as all test cases passing, and the error messages emitted by the interpreter matching the upstream regex expectations. Document the final pass count in the `Outcomes & Retrospective` section.

## Idempotence and Recovery

All steps are repeatable. If the full-suite mirror copy becomes stale, re-copy from the upstream repo. If tests fail after a change, revert or adjust only the latest commit and re-run the tests. Do not commit the `_fullsuite` mirror; keep it ignored and delete it safely when done.

## Artifacts and Notes

Example baseline failure transcript (for context only; update with real counts as you proceed):

    Failed: ~175 / 570 cases (upstream mirror)
    Common categories: error message casing, parser gaps (semicolons, trailing commas), slice None handling, int() base parsing, string formatting errors.

## Interfaces and Dependencies

The interpreter and runtime live in `src/Lokad.Starlark`. The following locations are the primary integration points that will change:

- `src/Lokad.Starlark/Parsing` and `src/Lokad.Starlark/Syntax` for grammar updates (semicolon separators, trailing commas, slice rules).
- `src/Lokad.Starlark/Runtime/ExpressionEvaluator.cs` and `src/Lokad.Starlark/Runtime/ModuleEvaluator.cs` for scope rules, iteration mutation checks, and comparison semantics.
- `src/Lokad.Starlark/Runtime/StarlarkBuiltins.cs` and `src/Lokad.Starlark/Runtime/StarlarkMethods.cs` for builtins and string helpers.
- `src/Lokad.Starlark/Runtime/RuntimeErrors.cs` and `src/Lokad.Starlark/Runtime/StarlarkRuntimeException.cs` for error message normalization and formatting.
- `tests/Lokad.Starlark.Tests/Conformance/ConformanceTestEnvironment.cs` if additional conformance-only builtins are required (for example, `print`).

If new helpers are needed to centralize error messages, define them in `RuntimeErrors.cs` and use them consistently across evaluators and builtins.

Note on updates: when this plan changes, add a note at the bottom describing what changed and why.

Plan update (2026-01-03): Marked the full-suite mirror/baseline milestone as complete and recorded the net10.0 pass/fail counts so future deltas are measurable.
Plan update (2026-01-03): Added parser progress on trailing commas/semicolons and refreshed the net10.0 pass/fail counts after the grammar changes.
