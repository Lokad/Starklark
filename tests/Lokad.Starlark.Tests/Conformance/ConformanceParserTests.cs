using System;
using System.IO;
using Xunit;

namespace Lokad.Starlark.Tests.Conformance;

public sealed class ConformanceParserTests
{
    [Fact]
    public void ParsesGoTaggedExpectations()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(
                path,
                new[]
                {
                    "# comment",
                    "value = 1 ### go: expected-error",
                    "value = 2 ### java: other-error",
                    "---"
                });

            var cases = ConformanceParser.ParseFile(path);

            var testCase = Assert.Single(cases);
            Assert.Equal("expected-error", testCase.ExpectedErrorPattern);
            Assert.Contains("# comment", testCase.Source, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void KeepsFirstExpectedError()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(
                path,
                new[]
                {
                    "value = 1 ### go: first",
                    "value = 2 ### go: second",
                    "---"
                });

            var cases = ConformanceParser.ParseFile(path);

            var testCase = Assert.Single(cases);
            Assert.Equal("first", testCase.ExpectedErrorPattern);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
