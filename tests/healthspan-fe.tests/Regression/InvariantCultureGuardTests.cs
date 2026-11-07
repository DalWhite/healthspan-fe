using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// WASM runs with InvariantGlobalization=true (#37 payload trim), so any
/// GetCultureInfo("es-ES") / new CultureInfo("es-ES") in app code crashes at
/// runtime in the browser (CultureNotFoundException → WebAssemblyRenderer[100])
/// while server-side unit tests stay green. Spanish formatting must go through
/// EsFormat instead. This test is the CI tripwire for that gap.
///
/// Second tripwire (t_82353d5d): the components below render numbers that bUnit
/// tests assert in fixed en-US/invariant shapes. If a component reformats a
/// number with the thread's implicit culture, local runs under LANG=es_ES
/// diverge from CI/prod. Every numeric ToString or interpolated format
/// specifier in these files must carry an explicit format provider
/// (ToString(spec, CultureInfo.InvariantCulture) or
/// string.Create(CultureInfo.InvariantCulture, $"...")).
/// </summary>
public class InvariantCultureGuardTests
{
    // Files whose numeric rendering is asserted by tests; a regression here is
    // silent in CI (en-US host) and only bites on es_ES hosts and future tests.
    private static readonly string[] NumericFormatGuardFiles =
    [
        "Pages/Workouts.razor",
        "Components/Training/ReadinessCard.razor",
        "Components/Training/AdaptationCoverageRadar.razor",
        "Pages/ExerciseTypes/ExerciseTypeDetails.razor",
    ];

    // A C# string literal with escape handling, used to delimit constructs.
    private const string StringLiteral = @"""(?:\\.|[^""\\])*""";

    // Constructs that already pin the culture explicitly (pinned = exempt).
    private static readonly Regex[] PinnedConstructs =
    [
        new($@"string\.Create\(\s*CultureInfo\.InvariantCulture\s*,\s*\$?{StringLiteral}", RegexOptions.Singleline),
        new($@"FormattableString\.Invariant\(\s*\$?{StringLiteral}", RegexOptions.Singleline),
        new(@".ToString\([^()]*(?:CultureInfo|IFormatProvider)[^()]*\)"),
    ];

    // .ToString("numeric-format") with no provider argument. Numeric formats:
    // custom ([0#]...) or standard with digits (N1, F2...) or bare single-char
    // numeric (C, E, F, G, P, R). Bare "N" is exempt: Guid.ToString("N") is
    // culture-free and used legitimately. Date/time formats (d, y, M, h...)
    // are out of scope: dates go through the test-host culture pin (PR #70).
    private static readonly Regex UnpinnedToString =
        new(@"""(?:[0#][^""]*|[NR]\d+[^""]*|[CFEPGR]\d*)""(?=\s*\))");

    // Interpolated literals, then numeric format holes inside them
    // ({expr:N0}, {x:0.##}). Date holes ({t:dd MMM}) use lowercase specifiers
    // and are out of scope, same rationale as above.
    private static readonly Regex InterpolatedLiteral =
        new($@"\$@?{StringLiteral}", RegexOptions.Singleline);

    private static readonly Regex InterpolatedNumericHole =
        new(@"\{[^{}]+?:\s*(?:[0#][0-9,.#]*|[NFCPEG]\d+)\}");

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

    [Fact]
    public void GuardedComponents_NumericFormattingPinsCulture()
    {
        var repoRoot = FindRepoRoot();

        var offenders = new List<string>();
        foreach (var relative in NumericFormatGuardFiles)
        {
            var path = Path.Combine(repoRoot, relative);
            Assert.True(File.Exists(path),
                $"Guarded component is gone or moved: {relative}. Update NumericFormatGuardFiles deliberately.");

            offenders.AddRange(FindUnpinnedNumericFormatting(File.ReadAllText(path), relative));
        }

        Assert.Empty(offenders);
    }

    private static IEnumerable<string> FindUnpinnedNumericFormatting(string text, string relative)
    {
        // Collect exempt spans first so line numbers stay exact.
        var pinned = new List<(int Start, int End)>();
        foreach (var regex in PinnedConstructs)
        {
            foreach (Match m in regex.Matches(text))
            {
                pinned.Add((m.Index, m.Index + m.Length));
            }
        }

        bool IsPinned(int index) => pinned.Exists(p => index >= p.Start && index < p.End);

        string Locate(int index)
        {
            var line = 1 + text.AsSpan(0, index).Count('\n');
            return $"{relative}:{line}";
        }

        foreach (Match m in UnpinnedToString.Matches(text))
        {
            if (!IsPinned(m.Index))
            {
                yield return $"{Locate(m.Index)}: .ToString({m.Value}) without an explicit format provider";
            }
        }

        foreach (Match literal in InterpolatedLiteral.Matches(text))
        {
            if (IsPinned(literal.Index))
            {
                continue;
            }

            foreach (Match hole in InterpolatedNumericHole.Matches(literal.Value))
            {
                yield return $"{Locate(literal.Index + hole.Index)}: interpolated numeric format {hole.Value} outside string.Create(CultureInfo.InvariantCulture, ...)";
            }
        }
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
