using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Lokad.Starlark;
using Xunit;

namespace Lokad.Starlark.Tests.Conformance;

public sealed class ConformanceTests
{
    public static IEnumerable<object[]> GetCases()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "TestData", "Conformance", "go");
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var file in Directory.GetFiles(root, "*.star", SearchOption.AllDirectories))
        {
            var cases = ConformanceParser.ParseFile(file);
            foreach (var testCase in cases)
            {
                yield return new object[] { file, testCase };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetCases))]
    public void RunsConformanceCase(string path, ConformanceCase testCase)
    {
        var interpreter = new StarlarkInterpreter();
        var environment = ConformanceTestEnvironment.Create();

        if (testCase.ExpectedErrorPattern == null)
        {
            interpreter.ExecuteModule(testCase.Source, environment);
            return;
        }

        var exception = Assert.ThrowsAny<Exception>(
            () => interpreter.ExecuteModule(testCase.Source, environment));
        var regex = new Regex(testCase.ExpectedErrorPattern, RegexOptions.Singleline);
        Assert.True(
            regex.IsMatch(exception.Message),
            $"Expected error in {path}:{testCase.Name} to match '{testCase.ExpectedErrorPattern}', got '{exception.Message}'.");
    }
}
