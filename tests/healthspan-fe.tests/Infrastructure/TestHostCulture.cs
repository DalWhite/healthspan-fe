using System.Globalization;
using System.Runtime.CompilerServices;

namespace healthspan_fe.tests.Infrastructure;

/// <summary>
/// Pins the test host to InvariantCulture so bUnit tests observe the SAME
/// formatting as production WASM, which runs with InvariantGlobalization=true
/// (healthspan-fe-wasm-pwa.csproj). Without this, components that format
/// implicitly ({N0}, {N1}, {0.##}) render with the OS locale (e.g. es_ES →
/// "80,0 kg") while the assertions reflect production invariant output
/// ("80.0 kg"), producing deterministic "flaky" failures on es_ES hosts.
/// Spanish user-visible strings go through EsFormat (static tables), which is
/// culture-independent, so pinning invariant never hides intended Spanish copy.
///
/// Tests that intentionally exercise other cultures (e.g.
/// DateOfBirthConverterTests) set CultureInfo.CurrentCulture explicitly per
/// test; an explicit culture always wins over the default set here.
/// </summary>
public static class TestHostCulture
{
    [ModuleInitializer]
    internal static void PinInvariantCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
    }
}
