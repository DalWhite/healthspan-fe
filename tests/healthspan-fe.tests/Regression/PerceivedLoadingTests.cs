using System.IO;
using Bunit;
using Xunit;
using healthspan_fe_wasm_pwa.Components;

namespace healthspan_fe_wasm_pwa.tests.Regression;

// Contrato de carga percibida (t_fea01cd1 · docs/design/skeleton-loading-spec.md):
// cada pantalla clave renderiza su skeleton ANTES del fetch y expone los
// selectores que fe_bench mide en modo cache-miss:
//   Tier-1 (boot WASM): #hs-boot-skeleton[data-hs-loading="boot"] en index.html
//   Tier-2 (fetch):     [data-hs-loading="skeleton"] (ScreenSkeleton + historial)
public class PerceivedLoadingTests
{
    [Theory]
    [InlineData("today", 5)]     // 3 tiles + 2 tarjetas
    [InlineData("list", 5)]      // 5 filas de lista
    [InlineData("cards", 3)]     // 3 tarjetas de rutina
    [InlineData("insights", 4)]  // 3 tiles + panel
    [InlineData("settings", 4)]  // 4 tarjetas
    public void ScreenSkeleton_ExposesContractMarkers(string variant, int blockCount)
    {
        using var ctx = new TestContext();
        var cut = ctx.RenderComponent<ScreenSkeleton>(p => p.Add(c => c.Variant, variant));

        var root = cut.Find("div.hs-screen-skeleton");
        Assert.Equal("skeleton", root.GetAttribute("data-hs-loading"));
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Equal("true", root.GetAttribute("aria-busy"));

        // Bloques con altura fija: el swap skeleton->contenido no desplaza nada (CLS 0).
        var blocks = cut.FindAll(".hs-screen-skeleton .hs-skeleton");
        Assert.Equal(blockCount, blocks.Count);
        Assert.All(blocks, b => Assert.False(string.IsNullOrEmpty(b.GetAttribute("style"))));

        // Señal accesible para lectores de pantalla.
        Assert.Contains("visually-hidden", cut.Find("span").GetAttribute("class"));
    }

    [Fact]
    public void BootSkeleton_PresentInIndexHtml_WithVariantScript()
    {
        var html = File.ReadAllText(RepoRootFile("wwwroot/index.html"));
        Assert.Contains("id=\"hs-boot-skeleton\"", html);
        Assert.Contains("data-hs-loading=\"boot\"", html);
        // Variante por ruta sin requests extra (Today/historial/rutinas/perfil/importación).
        foreach (var route in new[] { "/training/today", "/wellbeing/activities", "/training/routines", "/profile", "/settings" })
            Assert.Contains(route.Replace("/", "\\/"), html);
    }

    [Fact]
    public void BootSkeleton_AddsNoExtraRequests()
    {
        // El Tier-1 no puede añadir assets ni requests (no empeora cold kb):
        // en index.html no hay <img/link/script nuevos hacia css/js de skeleton.
        var html = File.ReadAllText(RepoRootFile("wwwroot/index.html"));
        Assert.DoesNotContain("hs-boot.css", html);
        Assert.DoesNotContain("hs-boot.js", html);
        Assert.DoesNotContain("src=\"icon-512.png\"", html); // cargador antiguo con imagen, eliminado
    }

    [Fact]
    public void HistorialSkeleton_HasContractMarker()
    {
        var src = File.ReadAllText(RepoRootFile("Pages/Wellbeing/ActivityHistory.razor"));
        Assert.Contains("hs-hist-skeleton\" data-hs-loading=\"skeleton\"", src);
    }

    private static string RepoRootFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "wwwroot/index.html")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
