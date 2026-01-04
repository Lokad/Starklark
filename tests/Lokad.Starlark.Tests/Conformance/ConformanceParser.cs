using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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
        var buffer = new StringBuilder();
        var expectedErrors = new List<string>();
        var index = 1;
        var errorRegex = new Regex("^(.*?) *### *((go|java|rust):)? *(.*)$");
        const string impl = "go";

        void Flush()
        {
            if (buffer.Length == 0 && expectedErrors.Count == 0)
            {
                expectedErrors.Clear();
                return;
            }

            var source = buffer.ToString();
            var expectedError = expectedErrors.Count == 0 ? null : expectedErrors[0];
            cases.Add(new ConformanceCase($"case-{index}", source, expectedError));
            buffer.Clear();
            expectedErrors.Clear();
            index++;
        }

        foreach (var line in lines)
        {
            if (line == "---")
            {
                Flush();
                continue;
            }

            var match = errorRegex.Match(line);
            if (match.Success)
            {
                var errorImpl = match.Groups[3].Value;
                if ((string.IsNullOrEmpty(errorImpl) || errorImpl == impl) && expectedErrors.Count == 0)
                {
                    expectedErrors.Add(match.Groups[4].Value);
                }

                buffer.Append(match.Groups[1].Value);
                buffer.Append('\n');
                continue;
            }

            buffer.Append(line);
            buffer.Append('\n');
        }

        Flush();
        return cases;
    }
}
