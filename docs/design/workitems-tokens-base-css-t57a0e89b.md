# Mapa de work items — Design tokens y CSS base (t_57a0e89b, 2026-10-23)

**Objetivo:** dividir la implementación del nuevo design system en work items de **tokens y CSS base únicamente** (sin cambios de componentes ni páginas), cada uno ≤5-8 ficheros, listo para que pau despache tras aprobación.

**Inputs:** propuesta de design system t_38c41b61 (en redacción; sus hijos t_45b423e0/t_0098e568/t_3c855b9f/t_68a74222/t_ba764d7a aún abiertos) + auditoría t_511ecd88 (`docs/design/audit-design-system-v1-t511ecd88.md`) + benchmark t_75c7b777 + literatura t_8a8f583d. Los **valores finales de tokens salen de la propuesta aprobada**; los hexos citados abajo (#1B66E8, #868FA0) son los candidatos ya fundamentados en la auditoría y deben confirmarse contra ella antes de despachar WI-T3/WI-T4.

**Verificación hecha en código (2026-10-23):** un solo fichero CSS (`wwwroot/css/app.css`, 6109L); `:root` tokens app.css:2-81; 14 alias legacy → `--mud-palette-*` app.css:23-36; espejo C# `Shared/HealthspanTheme.cs:13-24` + `ApplyV1Palette` :76-118 con `PaletteLight` = valores dark (hack :30-33); test de contrato `tests/healthspan-fe.tests/Regression/SharedVisualFoundationTests.cs:90-130` fija hexos de `:root` y del MudTheme; 29 `@media` con ≥10 breakpoints ad-hoc en app.css (380/420/599/600/699/700/760/768/769/800/950/959/960/999/1000); Inter vía Google Fonts en `wwwroot/index.html:10-12`; no existe ningún tokens.json/DTCG en el repo.

## Inventario de categorías de tokens (estado actual)

| Categoría | Spec v1 (docs/design/DESIGN.md) | Implementación | Drift |
|---|---|---|---|
| Color (13 tokens + accent-ink) | §1 prosa, 13 hex | app.css:4-19 · HealthspanTheme.cs:13-24 | `--hs-accent-ink` (app.css:13) no está en la spec; triple fuente de verdad |
| Tipografía | §2 escala 8 pasos | solo 3 tokens `--hs-font-size-*` (app.css:38-43) + Inter index.html:10-12 + tabular-nums app.css:89 | escala de la spec no tokenizada |
| Spacing | sin nombres | `--hs-space-1..7` app.css:54-60 | implementado, no nombrado en spec |
| Radius | sin nombres | `--hs-radius-*` app.css:46-52 | ídem |
| Shadows | §1 "SIN sombras" | `--hs-shadow-*` app.css:64-68 (card/panel = none) | contradicción L1 con glow FAB (app.css:462) |
| Breakpoints | único 1000px | 29 `@media`, ≥10 valores ad-hoc | escalera incoherente (M3) |
| Shell/layout | §4 | `--hs-content-max/-touch-target/-topbar-height/-sidebar-width/-bottom-nav-height` app.css:70-74 | OK |

## Work items (tokens / CSS base)

### WI-T1 · Spec de tokens machine-readable (DESIGN.md v2 + export DTCG)
- **Objetivo:** reescribir `docs/design/DESIGN.md` como spec con front-matter de tokens (nombres, valores dark/light, roles accent-fill vs accent-ink, ladder de breakpoints) y export DTCG `docs/design/tokens.json`.
- **Acceptance:** cada token de `:root` de app.css tiene entrada nombrada en la spec; valores dark y light explícitos; resuelve L1 (glow ≠ elevación) y documenta `--hs-accent-ink`, spacing y radius hoy indocumentados; `tokens.json` valida como DTCG (schema check).
- **Ficheros (1-2):** docs/design/DESIGN.md, docs/design/tokens.json.
- **Dependencias:** propuesta t_38c41b61 aprobada (fuente de valores). **Owner sugerido:** ux-designer.

### WI-T2 · Fuente única de verdad: tokens.json alimenta CSS y C#
- **Objetivo:** eliminar la triple fuente (app.css + alias mud + constantes C#): un generador/aplicación propaga tokens.json → `:root` de app.css y constantes de HealthspanTheme.cs.
- **Acceptance:** cambiar un hex en tokens.json y regenerar deja app.css y HealthspanTheme.cs sincronizados sin editarlos a mano; ningún literal hex duplicado entre app.css `:root` y HealthspanTheme.cs fuera del generado; tests de regresión pasan.
- **Ficheros (3-4):** docs/design/tokens.json, wwwroot/css/app.css, Shared/HealthspanTheme.cs, tests/…/SharedVisualFoundationTests.cs.
- **Dependencias:** WI-T1. **Owner:** pau.

### WI-T3 · Ajustes de paleta dark AA (accent, tertiary, ink sobre fills)
- **Objetivo:** aplicar los fixes de contraste aprobados: accent #1E6FFF→#1B66E8 (white-on-accent 5.11:1), t3 #78818F→#868FA0 (5.31 card / 4.68 input), regla accent-fill vs accent-ink documentada en tokens.
- **Acceptance:** tabla de contrastes WCAG en la spec: texto ≥4.5:1 y no-texto ≥3:1 en ambas paletas; test de contrato actualizado a los nuevos hexos y en verde; ningún selector de componente se toca (los usos de accent como texto —tabs, nombres de ejercicio— migran a accent-ink en el mapa de componentes, t_ee4c71df; hoy ya fallan, este item no introduce regresión).
- **Ficheros (3):** wwwroot/css/app.css, Shared/HealthspanTheme.cs, tests/…/SharedVisualFoundationTests.cs.
- **Dependencias:** WI-T1 (valores), WI-T2 recomendado antes (una edición en vez de tres). **Owner:** pau.

### WI-T4 · Paleta light real (eliminar el hack PaletteLight = dark)
- **Objetivo:** definir la paleta light espejo del ladder (superficies claras, mismo accent, mismos semánticos) según la propuesta, en tokens y en el theme C#; la app sigue dark-first por defecto.
- **Acceptance:** `HealthspanTheme.cs` PaletteLight ≠ PaletteDark (el hack :30-33 desaparece); contraste AA verificado en light por el gate WI-T8; sin cambio de comportamiento visible en dark (test de regresión dark intacto); bloque `[data-theme="light"]` (o equivalente) de tokens en app.css.
- **Ficheros (3-4):** Shared/HealthspanTheme.cs, wwwroot/css/app.css, docs/design/tokens.json, tests/…/SharedVisualFoundationTests.cs.
- **Dependencias:** WI-T1, WI-T3 (el light reutiliza el accent corregido). **Owner:** pau.

### WI-T5 · Escalera de breakpoints de 3 escalones
- **Objetivo:** consolidar los 29 `@media` con ≥10 breakpoints ad-hoc de app.css a la ladder de 3 escalones de la propuesta (candidato 480/768/1000), solo en CSS base.
- **Acceptance:** en app.css quedan exactamente 3 valores de breakpoint de layout (queries de media no-layout —prefers-reduced-motion, pointer:coarse— excluidas); el shell cambia de nav a los 1000px; QA verifica sin regresión visual a 360/480/768/1024px (gate de t_b155320d). Riesgo alto: item de un solo fichero pero 29 bloques — despachar con checklist QA por bloque.
- **Ficheros (1-2):** wwwroot/css/app.css (+ test harness de QA si existe).
- **Dependencias:** WI-T1 (definición de la ladder). **Owner:** pau.

### WI-T6 · Tipografía: tokenizar la escala de 8 pasos + resistencia a font scaling 200%
- **Objetivo:** declarar los 8 pasos de la escala §2 como tokens (`--hs-font-size-*` + line-height/tracking) y asegurar que el CSS base aguanta font scaling al 200% (WCAG 1.4.4).
- **Acceptance:** la escala completa existe como tokens en `:root` y coincide con la spec; tabular-nums global intacto (app.css:89); verificación con zoom de fuente 200% sin overflow/clipping en layout base (checklist QA); Inter y fallback sin cambios.
- **Ficheros (3):** wwwroot/css/app.css, wwwroot/index.html, Shared/HealthspanTheme.cs (Typography).
- **Dependencias:** WI-T1. **Owner:** pau.

### WI-T7 · Limpieza de alias legacy `--hs-color-*` → `--mud-palette-*`
- **Objetivo:** retirar o redirigir los 14 alias legacy (app.css:23-36) para que solo la fuente única de tokens impulse el color.
- **Acceptance:** cero referencias `--mud-palette-*` en app.css fuera de la capa de theme; los consumidores de `--hs-color-*` leen tokens v1; tests pasan. (El único consumidor fuera de app.css/test es MuscleLoadBodyMap.razor → se trata en el mapa de componentes t_ee4c71df.)
- **Ficheros (2):** wwwroot/css/app.css, tests/…/SharedVisualFoundationTests.cs.
- **Dependencias:** WI-T2. **Owner:** pau.

### WI-T8 · Gate de tests: contrato de tokens + validación de contraste WCAG
- **Objetivo:** extender el test de regresión para que falle si cualquier token del código diverge de tokens.json y añadir un test que calcule ratios WCAG de los pares clave (texto sobre superficies, white sobre accent/fills semánticos) y exija ≥4.5:1 / ≥3:1 en ambos temas.
- **Acceptance:** `dotnet test` en verde; el test falla deliberadamente si se cambia un hex sin actualizar la spec (verificado con una prueba intencional); cubre pares dark y light.
- **Ficheros (2-3):** tests/…/SharedVisualFoundationTests.cs, tests/…/ContrastContractTests.cs (nuevo), docs/design/tokens.json.
- **Dependencias:** WI-T1..WI-T4. **Owner:** pau, revisión qa.

## Orden de ejecución

```
WI-T1 (spec+tokens.json)
  └─ WI-T2 (single source)
       ├─ WI-T3 (paleta dark AA) ── WI-T4 (light real) ── WI-T8 (gate)
       ├─ WI-T7 (alias legacy)
       └─ WI-T5 (breakpoints)  ·  WI-T6 (tipografía)   [paralelos, sin solapamiento de zonas]
```

## Fuera de alcance de este mapa (va al mapa de componentes t_ee4c71df o a migración de páginas)

- Hexos hardcodeados en razor/charts (M1: Biomarkers.razor:535-536, Assessments.razor:414, ExerciseTypeDetails.razor:526/749, MuscleLoadBodyMap.razor:29/165-167, TrendChart.razor:50, SparklineChart.razor:32).
- Migración de usos accent→accent-ink en selectores de componente (tabs, nombres de ejercicio) y fix ink sobre fill done del FAB (H3, app.css:464-468).
- Chip interactivo vs badge estático (L2) y token disabled (L3).
- Gates de QA y orden de rollout → t_b155320d.

**Ningún item toca componentes ni páginas; ningún item contiene código — son especificaciones para despachar.**
