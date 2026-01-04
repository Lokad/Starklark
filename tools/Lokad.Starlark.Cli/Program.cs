using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Lokad.Starlark;
using Lokad.Starlark.Runtime;

namespace Lokad.Starlark.Cli;

[Command(Name = "lokad-starlark", Description = "Hermetic Starlark REPL and script runner.")]
[Subcommand(typeof(ReplCommand), typeof(RunCommand), typeof(ExecCommand))]
[HelpOption("-?|-h|--help")]
public sealed class StarlarkCli
{
    private int OnExecute(CommandLineApplication app, IConsole console)
    {
        return new ReplCommand().OnExecute(console);
    }
}

[Command("repl", Description = "Start an interactive Starlark REPL.")]
public sealed class ReplCommand
{
    public int OnExecute(IConsole console)
    {
        var interpreter = new StarlarkInterpreter();
        var environment = CliEnvironment.CreateEnvironment(console);

        Console.WriteLine("Lokad.Starlark REPL");
        Console.WriteLine("Type :exit or :quit to leave.");

        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line == null)
            {
                return 0;
            }

            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed == ":exit" || trimmed == ":quit")
            {
                return 0;
            }

            if (!TryEvaluateExpression(line, interpreter, environment, console))
            {
                TryExecuteModule(line, interpreter, environment, console);
            }
        }
    }

    private static bool TryEvaluateExpression(
        string source,
        StarlarkInterpreter interpreter,
        StarlarkEnvironment environment,
        IConsole console)
    {
        try
        {
            var value = interpreter.EvaluateExpression(source, environment);
            PrintValue(value, console);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void TryExecuteModule(
        string source,
        StarlarkInterpreter interpreter,
        StarlarkEnvironment environment,
        IConsole console)
    {
        try
        {
            var value = interpreter.ExecuteModule(source, environment);
            if (value != null && value is not StarlarkNone)
            {
                PrintValue(value, console);
            }
        }
        catch (Exception ex)
        {
            console.Error.WriteLine(ex.Message);
        }
    }

    private static void PrintValue(StarlarkValue value, IConsole console)
    {
        console.Out.WriteLine(StarlarkFormatting.ToRepr(value));
    }
}

[Command("run", Description = "Run a Starlark script file.")]
public sealed class RunCommand
{
    [Argument(0, "script", "Path to a .star file.")]
    public string? ScriptPath { get; set; }

    public int OnExecute(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(ScriptPath))
        {
            console.Error.WriteLine("Missing script path.");
            return 1;
        }

        string source;
        try
        {
            source = File.ReadAllText(ScriptPath);
        }
        catch (Exception ex)
        {
            console.Error.WriteLine(ex.Message);
            return 1;
        }

        var interpreter = new StarlarkInterpreter();
        var environment = CliEnvironment.CreateEnvironment(console);

        try
        {
            var value = interpreter.ExecuteModule(source, environment);
            if (value != null && value is not StarlarkNone)
            {
                console.WriteLine(StarlarkFormatting.ToRepr(value));
            }
        }
        catch (Exception ex)
        {
            console.Error.WriteLine(ex.Message);
            return 1;
        }

        return 0;
    }
}

[Command("exec", Description = "Run Starlark source provided inline.")]
public sealed class ExecCommand
{
    [Argument(0, "source", "Inline Starlark source.")]
    public string? Source { get; set; }

    public int OnExecute(IConsole console)
    {
        if (string.IsNullOrWhiteSpace(Source))
        {
            console.Error.WriteLine("Missing source.");
            return 1;
        }

        var interpreter = new StarlarkInterpreter();
        var environment = CliEnvironment.CreateEnvironment(console);

        try
        {
            var value = interpreter.ExecuteModule(Source, environment);
            if (value != null && value is not StarlarkNone)
            {
                console.WriteLine(StarlarkFormatting.ToRepr(value));
            }
        }
        catch (Exception ex)
        {
            console.Error.WriteLine(ex.Message);
            return 1;
        }

        return 0;
    }
}

public static class Program
{
    public static int Main(string[] args) => CommandLineApplication.Execute<StarlarkCli>(args);
}

internal static class CliEnvironment
{
    public static StarlarkEnvironment CreateEnvironment(IConsole console)
    {
        var environment = new StarlarkEnvironment();
        environment.AddFunction(
            "print",
            (args, kwargs) =>
            {
                if (kwargs.Count != 0)
                {
                    throw new InvalidOperationException("print does not accept keyword arguments.");
                }

                if (args.Count == 0)
                {
                    console.Out.WriteLine();
                    return StarlarkNone.Instance;
                }

                var builder = new System.Text.StringBuilder();
                for (var i = 0; i < args.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(StarlarkFormatting.ToString(args[i]));
                }

                console.Out.WriteLine(builder.ToString());
                return StarlarkNone.Instance;
            },
            isBuiltin: true);
        return environment;
    }
}
