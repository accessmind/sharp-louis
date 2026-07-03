using System;
using System.IO;
using System.Text;
using AccessMind.SharpLouis;
using FluentAssertions;
using Xunit;

namespace AccessMind.SharpLouis.Tests;

// Covers the ASCII-safe table-search-path resolution that lets liblouis (an ANSI char* path API) find
// tables even under a non-ASCII base directory, e.g. a non-Latin Windows user name.
public class TableSearchPathTests {
    [Fact]
    public void ResolveTableSearchPath_AsciiPath_ReturnedUnchanged() {
        const string path = @"C:\Users\Public\LibLouis\tables";
        Wrapper.ResolveTableSearchPath(path).Should().Be(path);
    }

    [Fact]
    public void ResolveTableSearchPath_NonAsciiPath_NeverYieldsANonAsciiResult() {
        // Real, existing directory with a non-ASCII name so GetShortPathName has something to shorten.
        var dir = Path.Combine(Path.GetTempPath(), "sharplouis_тест_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {
            var resolved = Wrapper.ResolveTableSearchPath(dir);
            // On a volume with 8.3 generation enabled we get the ASCII short form; where it is disabled
            // we fall back to the original path. Both are acceptable — what must never happen is a
            // freshly invented non-ASCII result that differs from the input.
            (Ascii.IsValid(resolved) || resolved == dir).Should().BeTrue();
        } finally {
            Directory.Delete(dir);
        }
    }
}
