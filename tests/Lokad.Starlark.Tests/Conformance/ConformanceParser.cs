using System;
using System.Collections.Generic;
using System.IO;

namespace Lokad.Starlark.Tests.Conformance;

public static class ConformanceParser
{
    public static IReadOnlyList<ConformanceCase> ParseFile(string path)
    {
        return ParseContent(File.ReadAllLines(path));
    }

    private static IReadOnlyList<ConformanceCase> ParseContent(string[] lines)
    {
        var cases = new List<ConformanceCase>();
        var buffer = new List<string>();
        string? expectedError = null;
        var index = 1;

        void Flush()
        {
            if (buffer.Count == 0)
            {
                expectedError = null;
                return;
            }

            var source = string.Join("\n", buffer);
            cases.Add(new ConformanceCase($"case-{index}", source, expectedError));
            buffer.Clear();
            expectedError = null;
            index++;
        }

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed == "---")
            {
                Flush();
                continue;
            }

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var (code, pattern) = SplitExpected(line);
            if (pattern != null)
            {
                expectedError = pattern;
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                buffer.Add(code);
            }
        }

        Flush();
        return cases;
    }

    private static (string Code, string? Pattern) SplitExpected(string line)
    {
        var index = line.IndexOf("###", StringComparison.Ordinal);
        if (index < 0)
        {
            return (line, null);
        }

        var code = line[..index].TrimEnd();
        var pattern = line[(index + 3)..].Trim();
        if (pattern.StartsWith("(", StringComparison.Ordinal) && pattern.EndsWith(")", StringComparison.Ordinal))
        {
            pattern = pattern[1..^1].Trim();
        }

        return (code, pattern.Length == 0 ? null : pattern);
    }
}
