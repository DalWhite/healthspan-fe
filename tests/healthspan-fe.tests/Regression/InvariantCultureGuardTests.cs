using System.IO;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// WASM runs with InvariantGlobalization=true (#37 payload trim), so any
/// GetCultureInfo("es-ES") / new CultureInfo("es-ES") in app code crashes at
/// runtime in the browser (CultureNotFoundException → WebAssemblyRenderer[100])
/// while server-side unit tests stay green. Spanish formatting must go through
/// EsFormat instead. This test is the CI tripwire for that gap.
/// </summary>
public class InvariantCultureGuardTests
{
    [Fact]
    public void AppCode_UsesNoExplicitEsCultures()
    {
        var repoRoot = FindRepoRoot();

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(repoRoot, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (ext is not (".cs" or ".razor"))
            {
                continue;
            }

            var relative = Path.GetRelativePath(repoRoot, file);
            if (relative.Contains("bin") || relative.Contains("obj")
                || relative.StartsWith(".worktrees") || relative.StartsWith("tests"))
            {
                continue;
            }

            if (relative == Path.Combine("Helpers", "EsFormat.cs"))
            {
                continue; // the sanctioned helper itself
            }

            if (File.ReadAllText(file).Contains("\"es-ES\""))
            {
                offenders.Add(relative);
            }
        }

        Assert.Empty(offenders);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "healthspan-fe-wasm-pwa.csproj")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new DirectoryNotFoundException("healthspan-fe-wasm-pwa.csproj not found above the test assembly.");
    }
}
