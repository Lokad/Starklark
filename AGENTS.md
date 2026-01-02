## Turn off the .NET “terminal logger”

Use `--tl:off` avoids dynamic output and progress rendering.

```powershell
dotnet restore --tl:off -v minimal
dotnet build   --tl:off --nologo -v minimal
dotnet test    --tl:off --nologo -v minimal --no-build
```

## Definition of done

[to be completed]

## ExecPlans

When writing complex features or significant refactors, use an ExecPlan (as described in .agent/PLANS.md) from design to implementation.
