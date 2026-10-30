# Auditoría design system v1 — hallazgos (t_511ecd88, 2026-10-23)

Auditoría de `docs/design/DESIGN.md` v1 vs. implementación real (`wwwroot/css/app.css` 6109 líneas, `Shared/HealthspanTheme.cs`, `Shared/MainLayout.razor`, Pages/Components). Solo hallazgos; ningún código modificado. Contrastes calculados con la fórmula WCAG relative luminance (script: `contrast_audit*.py`, scratch).

## Inventario spec ↔ código

| Área | DESIGN.md v1 | Implementación | Estado |
|---|---|---|---|
| Color (13 tokens) | §1, 13 hex | `app.css:2-20` `--hs-*` + `HealthspanTheme.cs:13-24` | Cubierto, +1 token no en spec (`--hs-accent-ink`, app.css:15-17) |
| Tipografía | §2 escala 8 pasos | Inter cargado (index.html:10-12), tabular-nums global (app.css:88-90), hero 4.75rem/800/-0.045em (app.css:476-482), micro-labels 0.69rem/700/uppercase (app.css:484-490) | Cubierto |
| Radios / spacing / sombras | sin nombres de token | `app.css:45-77` ladder completo (`--hs-radius-*`, `--hs-space-*`, sombras `none`) | Implementado pero NO nombrado en spec (drift código→spec) |
| Nav móvil/desktop | §4, breakpoint 1000px | `.hs-bottom-nav` 5 slots + FAB central (app.css:412-468, MainLayout.razor:79-106), sidebar ≥1000px, sheet de cuenta (MainLayout.razor:24) | Cubierto |
| Componentes | §3 card/CTA/chip/FAB/set table | `.hs-cta` (5620+), `.hs-chip` (3958+), `.hs-settable` (493+), `.hs-fab` (448+) | Cubierto con defectos de contraste |
| Alias legacy | — | 14 alias `--hs-color-*` → `--mud-palette-*` (app.css:22-35) | Doble fuente de verdad |

## Hallazgos priorizados

### HIGH

**H1 · Color · CTA primario falla AA.** `#FFFFFF` sobre `#1E6FFF` = **4.40:1** (< 4.5; label 16px/700 no es "large text"). Evidencia: `app.css:5642-5644` (`.hs-cta-primary color:#fff`), DESIGN.md §3. Viola WCAG SC 1.4.3. Es el botón más usado de la app (Hoy → Empezar entreno). Fix: oscurecer el accent a **#1B66E8** (white 5.11:1 ✓; el fill sigue visible sobre card 3.39:1 y canvas 3.70:1, cumple 1.4.11). Fuente: WCAG 1.4.3; ux-ui-good-apps "reservar color para significado, legibilidad primero"; popular-web-designs (Linear/Stripe usan accent más oscuro o texto tintado sobre fills de accent). → **ajustar token**.

**H2 · Color · accent como texto falla AA y la spec lo obliga.** DESIGN.md §1 usa `#1E6FFF` como texto (tabs activos, títulos de ejercicio editables, timer). Contraste: sobre nav-bg **4.23:1**, sobre card **3.93:1** — normal text < 4.5. Evidencia: `app.css:444-446` (tab activo, label 0.7rem/650), DESIGN.md §1/§5.2. La implementación ya resolvió chips/CTA-secundario con `--hs-accent-ink` (#9ABEFF, 8.3-10.1:1 ✓, app.css:15-17, 3981, 5655) pero la spec no lo documenta y tabs/nombres siguen con accent crudo. Viola WCAG 1.4.3 + ux-ui-good-apps regla 5 (color+contraste, no solo matiz). Fix: regla nueva — accent crudo SOLO fills; todo accent-text usa accent-ink. → **reconstruir regla de color**.

**H3 · Componentes · FAB "Finalizar" verde ilegible.** Icono `#fff` sobre `--hs-done` = **2.38:1** (< 3:1 incluso para gráficos, WCAG 1.4.11). Evidencia: `app.css:464-468` + `MainLayout.razor:88-98`. Además introduce un segundo fill accionable (tensión con §6 "un solo accent"). Fix: ink oscuro sobre el fill done (`#0E1116` sobre `#2FBF71` = 7.94:1 ✓). Fuente: WCAG 1.4.11; ux-ui-good-apps (semánticos solo para su significado). → **ajustar**.

**H4 · Spec ↔ código · la spec prescribe un CTA secundario que falla.** DESIGN.md §3: "CTA secundario: accent-soft bg + accent text" = **3.56:1** ✗. El código ya lo corrige con accent-ink (8.33:1 ✓, app.css:5653-5656). La spec es el documento defectuoso; el código tiene razón. → **reconstruir spec** (documentar accent-ink y washes como tokens).

### MEDIUM

**M1 · Componentes · 12 hexos hardcodeados saltan el token contract.** `Biomarkers.razor:535-536`, `Assessments.razor:414`, `ExerciseTypeDetails.razor:526,749`, `MuscleLoadBodyMap.razor:29,165-167`, `TrendChart.razor:50`, `SparklineChart.razor:32`. Los valores coinciden con la paleta hoy, pero son componentes aislados: cambiar el accent (H1) no los propaga. Viola Airbnb DLS "Unified" (ux-ui-good-apps §8: design tokens, no one-off components). El test de regresión (`SharedVisualFoundationTests.cs:107+`) fija `:root` pero no los hexos razor. → **ajustar** (var(--hs-*) o HealthspanTheme const en todo chart/SVG).

**M2 · Tipografía · tertiary (#78818F) bajo AA en card/input.** 4.39:1 sobre card, 3.87:1 sobre input. La spec lo asigna a texto real de 12.5px: columna "Anterior" del set table (`app.css:518-522`, DESIGN.md §3) y meta. Viola WCAG 1.4.3 para texto normal. Fix: aclara t3 a **#868FA0** (5.31 card / 4.68 input ✓). → **ajustar token**.

**M3 · Densidad · escalera de breakpoints incoherente.** 29 `@media` con ≥10 breakpoints ad-hoc (380, 420, 599/600, 699/700, 760, 768/769, 800, 950, 959/960, 999/1000) frente al único breakpoint de shell de la spec (1000px). Drift por página; riesgo de estados rotos en móvil (objetivo primario del usuario). Viola consistencia DLS (ux-ui-good-apps §8; popular-web-designs: todos los sistemas de referencia usan 2-3 escalones). → **reconstruir** como ladder 3 escalones (p.ej. 480 / 768 / 1000).

**M4 · Tokens · triple fuente de verdad.** `:root --hs-*` (app.css) + alias legacy → `--mud-palette-*` (app.css:22-35) + constantes C# en `HealthspanTheme.cs:13-24`. Hoy sincronizadas por test, pero un cambio de accent exige tocar 3 sitios. Viola design-md (tokens single-source, token references `{colors.primary}`) y Airbnb "Unified". → **reconstruir**: spec de tokens como fuente única que genere/alimente CSS y C#.

**M5 · Spec · DESIGN.md v1 no es machine-readable.** Es prosa sin front-matter de tokens: no se puede lintear, diffear ni validar contraste con herramienta (design-md skill: spec Google `@google/design.md` con lint + WCAG contrast-ratio). El nuevo design system debería escribirse como DESIGN.md compliant + export (Tailwind/DTCG). → **reconstruir formato**.

**M6 · Dark mode · PaletteLight = valores dark.** `HealthspanTheme.cs:30-33` aplica la paleta oscura también a `PaletteLight` (hack documentado, latente). Si el toggle claro llega (la propuesta t_38c41b61 pide dark/light), cualquier componente que lea la paleta light hereda superficies oscuras. → **reconstruir** la paleta light desde 0 en la nueva propuesta (ladder espejo + mismo accent).

### LOW

**L1 · Spec internamente contradictoria:** §1 "SIN sombras" vs §3 FAB glow `0 0 18px rgba(30,111,255,.45)` (implementado, app.css:462). Definir "glow ≠ elevación" o eliminarlo. → ajustar redacción.
**L2 · Chips decorativos con target 44px:** `.hs-chip` min-height 44px (app.css:3959) aplica también a badges estáticos (PR badge) → infla densidad. Separar chip-interactivo (44px) de badge-estático. → ajustar.
**L3 · Disabled por opacity** (`hs-cta:disabled opacity .55`, app.css:5637): funciona pero la spec no define token disabled (t3 sobre raised). → documentar.

## Veredicto por área

| Área | Veredicto |
|---|---|
| Color (ladder, semánticos, washes) | **Mantener** la arquitectura (washes pasan AA: done 5.53, PR 6.63, accent-ink 7.84) · **ajustar** accent #1E6FFF→#1B66E8, t3→#868FA0, ink sobre fills semánticos |
| Tipografía | **Mantener**: Inter, tabular-nums global verificado (app.css:89), hero/micro-labels fieles a spec; único ajuste = paso tertiary |
| Componentes | **Ajustar**: token contract estricto (M1), separar accent-fill/accent-ink (H2), chip vs badge (L2) |
| Densidad / breakpoints | **Reconstruir** la escalera (M3) |
| Dark mode | **Mantener** dark-first; **reconstruir** light palette real (M6) |
| Formato de la spec | **Reconstruir** como DESIGN.md con tokens (M5) |

## Lo que se sostiene (no reconstruir)

- Ladder de superficies sin sombras (app.css:64-70) — coherente con Apple Fitness/Linear (ux-ui-good-apps, popular-web-designs).
- Accent único + semánticos solo con wash de significado (app.css:3984-4001) — los chips semánticos pasan AA.
- Inter + tabular-nums global + override de typography MudBlazor (HealthspanTheme.cs:49-71).
- Touch targets 44px tokenizados y aplicados (20+ sitios), FAB 46px, safe-area insets.
- Nav: FAB = acción directa (no ruta duplicada con "Hoy", MainLayout.razor:189), sheet de cuenta en móvil — cumple los estándares del usuario.
- Tests de regresión del token contract (SharedVisualFoundationTests.cs) — práctica excelente; extenderla a los hexos razor (M1).
- `prefers-reduced-motion` (app.css:105) y focus-visible global (app.css:92-96).

Fuentes: WCAG 2.1 SC 1.4.3/1.4.11 (cálculo propio, fórmula relative luminance); ux-ui-good-apps (reglas 5, 6, 8); popular-web-designs (Linear, Stripe, Apple — accent sobre dark surfaces); design-md (Google spec: tokens machine-readable + lint de contraste); NN/g progressive disclosure (no violado en v1).
