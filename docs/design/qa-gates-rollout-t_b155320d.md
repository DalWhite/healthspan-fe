# Gates de QA y orden de rollout — design system (t_b155320d, 2026-10-23)

**Objetivo:** definir los **gates de QA** (quién verifica qué, con qué criterio medible y en qué momento) y el **orden de rollout** del nuevo design system sobre los tres mapas de work items ya entregados. Solo especificación: **ningún item contiene código**; los items describen tests/checklists a crear por qa sobre el trabajo de pau/ux-designer.

**Inputs:** propuesta t_38c41b61 (en cierre: QA t_68a74222 running, t_ba764d7a todo) + mapa de tokens `docs/design/workitems-tokens-base-css-t57a0e89b.md` (WI-T1..T8) + mapa de componentes `docs/design/workitems-componentes-design-system-tee4c71df.md` (WI-C1..C10) + mapa de páginas `docs/design/workitems-migracion-paginas-t5f21d034.md` (WI-P1..P10) + `docs/design/tokens-proposal.md` (§8 token contract, §9 tabla de contraste, §10 bloque CSS de referencia) + `docs/design/components-proposal.md` (§8 matriz componente×estado, §10 huecos abiertos para QA) + auditoría `docs/design/audit-design-system-v1-t511ecd88.md` (H1-H4 / M1-M6 / L1-L3).

**Qué NO es este documento:** no duplica t_68a74222 (verificación de accesibilidad **de la propuesta**, que produce `docs/design/accessibility-checklist.md`). Aquí los gates verifican la **implementación** a medida que aterriza, fase por fase.

---

## 1. Verificación hecha en código (2026-10-23) — baselines sobre los que se fijan los gates

| Medida | Valor actual | Cómo se midió |
|---|---|---|
| Suite de regresión FE en `main` | **246/246 verdes** (54 s) | `DOTNET_ROLL_FORWARD=Major dotnet test tests/healthspan-fe.tests/healthspan-fe.tests.csproj -c Release` |
| CI que ya existe | `dotnet test` en **PR y push a main** | `.github/workflows/azure-static-web-apps-salmon-island-045222d03.yml` |
| Tooling de tests | bUnit 1.36 + xunit 2.9.3 + FluentAssertions | `tests/healthspan-fe.tests/healthspan-fe.tests.csproj` |
| Harness táctil ya existente | `TouchTargetHarnessTests.cs` vuelca markup renderizado a disco con `HS_TOUCH_HARNESS_DIR` | `tests/…/Regression/TouchTargetHarnessTests.cs:22,52` |
| Tooling de visual-diff / Playwright | **no existe** en el repo | grep `playwright` = 0 hits fuera de `bin/` |
| Hook de tema light (`[data-theme=`) | **0 hits** en `wwwroot/`, `Shared/`, `Components/`, `Pages/` | grep |
| `@media` en `app.css` | **29 bloques, 15 valores px distintos** (600×7, 768×3, 999/960/959/769×2, 380/420/599/699/700/760/800/950/1000×1) | grep + conteo |
| Selectores `.hs-*` únicos | **585** en `app.css` (6109L) | grep -o \| sort -u |
| Referencias `var(--mud-palette-*)` | **89** en `app.css` | grep |
| `box-shadow` en `app.css` | **30** ocurrencias | grep |
| `color: var(--hs-accent)` (accent como texto) | **7** en `app.css` | grep |
| Hex literales fuera de la capa de tokens | **25** ocurrencias: 13 en 6 ficheros razor (MuscleLoadBodyMap 6, ExerciseTypeDetails 2, Biomarkers 2, Assessments 1, TrendChart 1, SparklineChart 1) + 12 en `Shared/HealthspanTheme.cs` (espejo del theme, no salto del contract) | grep `#[0-9A-Fa-f]{6}` |
| `style="` inline en razor | **6** | grep |
| `opacity: 0.55` como disabled | **app.css:5638** (`.hs-cta` disabled) | grep |
| Anclas que deben sobrevivir | `prefers-reduced-motion` ×1, `focus-visible` ×8, `tabular-nums` global, 18 refs `44px` | grep |

**Límites de herramienta que condicionan los gates (declarados, no asumidos):**

1. **No hay visual-diff automático.** El gate visual se define como *baseline versionado + diff asistido*: markup snapshots bUnit + capturas headless revisadas con checklist de defectos. Es semi-automático y tiene un costo por fase; se presupuesta explícitamente.
2. **Auth:** la API solo autentica por Google OAuth (no existe login por password) → el QA visual **logueado** requiere credenciales guardadas en el vault; no es automatizable headless ni con cuenta seed local.
3. **Emulación móvil:** el harness de navegador no emula viewports móviles por defecto; se usa emulación explícita (400×850, mobile=True) tras la carga, y los breakpoints se verifican también contra el CSS desplegado (`curl <swa>/css/app.css`).
4. **Harness estático con DOM de prod:** hay que quitar `<base href="/">` y enlazar CSS con ruta absoluta, y **probar que el stylesheet cargó** (`getComputedStyle(document.documentElement).getPropertyValue('--hs-canvas')` no vacío) antes de fiar cualquier veredicto visual — si app.css no carga, los checks de color pasan por la razón equivocada.
5. **Flaky pre-existente conocido:** `ExerciseTypesPageTests` ha fallado en run completo sobre `main` limpio y pasado aislado. Un fallo aislado de esa clase **no** es regresión del rollout; se confirma corriéndola sola.

---

## 2. Gates de fase (G0..G4)

Un gate se cierra **solo** si todos sus criterios son verificables por comando o por checklist con evidencia adjunta (salida de test, salida de grep, captura + veredicto). "Se ve bien" no cierra un gate.

### G0 · Pre-rollout (antes del primer commit de diseño)
- **Entry:** t_38c41b61 cerrada y aprobada; `docs/design/tokens.json` (WI-T1) con los valores definitivos. *Hoy sigue abierto: t_68a74222 running, t_ba764d7a todo → ningún item de código es despachable todavía.*
- **Criterios:**
  1. Suite verde y su recuento registrado (baseline **246/246**).
  2. **Baseline visual capturado antes de tocar CSS**: 6 pantallas clave (Hoy, Historial, Librería, Detalle de ejercicio, Salud/Insights, Login) × 2 anchos (400px móvil emulado + 1280px) en dark, con prueba de stylesheet cargado.
  3. Baseline versionado en el repo (`docs/design/qa-baseline/`) con fecha y commit de origen.
- **No se abre WI-T3 si el baseline no está capturado** — sin él no existe forma de distinguir "regresión" de "cambio esperado".

### G1 · Tokens y CSS base (WI-T1..T8)
- **QA que cierra el gate:** WI-Q1 + WI-Q3 (parte base) + WI-Q4 (diff 1).
- **Criterios:**
  1. Test de contrato tokens.json ↔ `app.css :root` ↔ `HealthspanTheme.cs` en verde, **y falla deliberadamente** si un hex diverge (verificado con una edición intencional documentada).
  2. Test de contraste WCAG automatizado sobre los pares normativos de `tokens-proposal.md` §9 (43 pares) en **dark y light**, con tolerancia 0: texto ≥4.5:1, no-texto ≥3:1.
  3. Grep: hex literal fuera de la capa `:root`/theme = 0; `var(--mud-palette-*)` fuera de la capa de theme = 0 (WI-T7); `@media` de layout solo 480/768/1000 (queries no-layout permitidas: `prefers-reduced-motion`, `pointer:coarse`).
  4. Anclas intactas: `tabular-nums` global, `prefers-reduced-motion`, `focus-visible` (≥8), ladder sin sombras (`--hs-shadow-card/panel: none`).
  5. Suite verde con recuento ≥ baseline.
- **Riesgo declarado:** WI-T3 rompe **deliberadamente** `SharedVisualFoundationTests.cs:90-130` (fija `#1E6FFF`, `#78818F`). El gate exige que el test de contrato se actualice **en el mismo commit** que el hex; un commit que cambia un token con la suite roja no entra.

### G2 · Componentes (WI-C1..C10)
- **QA que cierra el gate:** WI-Q5 + WI-Q2 + WI-Q4 (diff 2).
- **Criterios:**
  1. Paridad catálogo×estado×tema (WI-Q5) verde.
  2. Contraste medido en dark **y** light de cada par texto/fill tocado por la fase (no solo los de la propuesta).
  3. Grep hex literal en `Components/` + `Pages/` = 0 al cerrar WI-C9; `color: var(--hs-accent)` fuera de la capa de tokens = 0 al cerrar WI-C10.
  4. Controles táctiles ≥44px medidos sobre markup renderizado + CSS real; disabled por token de color, no por `opacity` (app.css:5638 desaparece).
  5. Huecos de `components-proposal.md` §10 cerrados con medición: wash danger, `accent-ink` recalculado con accent nuevo, focus trap del sheet, hover sticky en táctil.
  6. Suite verde; los 7 usos actuales de accent-como-texto migrados.

### G3 · Migración de páginas (WI-P1..P10)
- **QA que cierra el gate:** WI-Q6 + WI-Q3 (parte páginas) + WI-Q4 (diff 3).
- **Criterios:**
  1. Smoke funcional por zona verde (WI-Q6) — el flujo núcleo de cada zona se completa sin pasos nuevos.
  2. Greps por zona = 0: hex, accent-como-texto, `box-shadow` en cards, `style="` inline.
  3. Ningún `@media` nuevo fuera de la ladder; nav→sidebar exactamente a 1000px.
  4. Diff visual por zona sin regresión sin justificación de spec.
  5. `PageShell`/`PageHeader`/`PageToolbar` intactos en las 26 páginas consumidoras; `aria-live="polite"` preservado (Workouts.razor:140, TraineeExercises.razor:41).
  6. Suite verde; taps y líneas netas por pantalla **no aumentan** (regla de resta del producto).

### G4 · Cierre global y release
- **QA que cierra el gate:** WI-Q7.
- **Criterios:**
  1. Greps globales = 0 (hex en `Pages/`+`Components/`, accent-como-texto fuera de capa tokens, `box-shadow` en cards, inline style en razor) y `@media` reducido a la ladder (29 → 3 escalones de layout).
  2. Selectores sin consumidor (`hs-widget-card` app.css:2503, `hs-table-stack` :2863, `hs-today-rest` :5771) retirados o documentados como deprecados (WI-C10).
  3. QA visual sobre la app **desplegada** (SWA) con emulación móvil 400×850 en las 6 pantallas clave, contra el baseline de G0.
  4. Sin regresión de rendimiento en librería (warm re-entry ≤1.65 s con la receta de medición ya documentada).
  5. Plan de rollback declarado: revert por **fase** (merge commit de fase), no por commit suelto.

**Stop-conditions (falla el gate → se para el rollout, no se sigue):** suite roja · contrato de tokens roto · contraste <4.5:1/<3:1 en cualquier par tocado · breakpoint nuevo fuera de la ladder · flujo de Hoy/Historial con más taps que antes · regresión visual sin justificación de spec.

---

## 3. Work items de QA (WI-Q1..WI-Q7)

Todos con owner **qa**; ficheros/tests estimados ≤5-8; ninguno contiene código de producto (son tests de verificación y checklists).

### WI-Q1 · Gate de cobertura de tokens (contrato + contraste automatizado)
- **Objetivo:** que ningún token pueda divergir de la spec sin romper CI, y que la tabla de contraste deje de ser una tabla y sea un test.
- **Acceptance:** `dotnet test` en verde; el test **falla deliberadamente** si se cambia un hex en `app.css` o `HealthspanTheme.cs` sin actualizar `tokens.json` (verificado con una edición intencional y su revert); los 43 pares de `tokens-proposal.md` §9 codificados como test con tolerancia 0 en dark y light; grep hex fuera de la capa `:root`/theme = 0.
- **Ficheros/tests (2-3):** `tests/…/Regression/SharedVisualFoundationTests.cs` (extender) · `tests/…/Regression/ContrastContractTests.cs` (nuevo) · `docs/design/tokens.json`.
- **Dependencias:** WI-T1, WI-T2, WI-T3, WI-T4, WI-T8. **Cierra:** G1.

### WI-Q2 · Gate de accesibilidad implementada (contraste real, targets, foco, font scaling)
- **Objetivo:** verificar sobre código real lo que la propuesta declara: contraste de los pares efectivamente renderizados, targets táctiles, estados de foco y resistencia a 200 % de fuente.
- **Acceptance:** targets ≥44px **medidos** sobre markup renderizado + `app.css` real (harness `HS_TOUCH_HARNESS_DIR` existente, extendido a los controles de las familias WI-C1..C8); 0 controles con disabled por `opacity`; foco visible preservado en CTA, chip, input, sheet y FAB; set table legible a 200 % de fuente en 360px sin overflow (§10.3); sheet de cuenta con Escape y restauración de foco reales (§10.4); hover sin estado sticky tras tap en móvil (§10.5); wash danger medido (§10.1); `accent-ink` recalculado con el accent nuevo verificado en chips selected, CTA secundario y nombres de ejercicio (§10.2).
- **Ficheros/tests (3-5):** `tests/…/Regression/TouchTargetHarnessTests.cs` (extender) · `tests/…/Regression/AccessibilityContractTests.cs` (nuevo) · checklist de foco/font-scaling en `docs/design/` · `docs/design/accessibility-checklist.md` (output de t_68a74222, como input).
- **Dependencias:** WI-T3, WI-T4, WI-T6, WI-C1..WI-C8. **Cierra:** parte de G2.

### WI-Q3 · Gate responsive (ladder de 3 escalones + checklist móvil)
- **Objetivo:** que la consolidación de breakpoints no introduzca regresión visual ni viewports huérfanos, y que la ladder sea la única regla de layout.
- **Acceptance:** grep `@media` de layout = solo 480/768/1000 (queries no-layout excluidas explícitamente); checklist visual 360/480/768/1024 px sin clipping, sin solapes y sin destinos de navegación duplicados; emulación móvil 400×850 en las 6 pantallas clave; cambio nav↔sidebar exactamente a 1000px; safe-area insets del FAB y bottom nav intactos.
- **Ficheros/tests (2-4):** test estático de `@media` sobre `app.css` (extensión de la suite) · checklist responsive en `docs/design/` · capturas de referencia por fase.
- **Dependencias:** WI-T5 (base, G1) y WI-P1..WI-P10 (páginas, G3). **Cierra:** parte de G1 y parte de G3.

### WI-Q4 · Gate de regresión visual (baseline + diff por fase)
- **Objetivo:** dar la única forma fiable de distinguir "mejora según la spec" de "roto".
- **Acceptance:** baseline capturado y versionado **antes** del primer cambio de CSS (G0), con prueba de stylesheet cargado; diff asistido tras G1, G2 y G3 sobre las 6 pantallas × 2 anchos; cada diferencia clasificada como *esperada por spec* (con referencia al item WI-*) o *regresión* (abre hallazgo con repro); 0 regresiones abiertas al cerrar cada fase.
- **Ficheros/tests (3-6):** `docs/design/qa-baseline/` (markup + capturas) · script de captura headless · checklist de defectos (contraste chip/label, peso del affordance de acción primaria, legibilidad de texto secundario y fechas, espacios muertos, nav con slot duplicado) · informe de diff por fase.
- **Dependencias:** baseline en G0; se ejecuta tras WI-T1..T8 (G1), WI-C1..C10 (G2) y WI-P1..P10 (G3). **Cierra:** parte de G1, G2 y G3.

### WI-Q5 · Gate de paridad de componentes (catálogo × estados × temas)
- **Objetivo:** que los 7 componentes del catálogo rendericen exactamente los estados declarados, en dark y en light, sin variantes locales.
- **Acceptance:** matriz componente×estado de `components-proposal.md` §8 verificada por test/checklist (default/hover/pressed/disabled/loading/error/selected/empty con N/A explícitos) en ambos temas; `.hs-chip` interactivo ≥44px y `.hs-badge` estático compacto (grep `min-height` — ningún badge estático de 44px); CTA primario label ≥4.5:1 y secundario con `accent-ink`; FAB-done con ink oscuro sobre fill; card/hero sin `box-shadow` y con ladder idéntico dark/light; sheet y diálogo con surface token, cierre ≥44px con nombre accesible y `prefers-reduced-motion` respetado; estados empty/error/loading con contrato WI-C8 (icono + texto, nunca solo color).
- **Ficheros/tests (4-6):** tests bUnit por familia (CTA, FAB, chip/badge, set table, nav, inputs, cards, sheet/estados) · checklist de matriz componente×estado en `docs/design/`.
- **Dependencias:** WI-C1..WI-C10 (y WI-T1/T3/T4 como prerequisitos de valores). **Cierra:** G2.

### WI-Q6 · Gate de smoke de migración (flujos funcionales por zona)
- **Objetivo:** que migrar una zona no rompa lo que la zona hace, y que la migración no añada fricción.
- **Acceptance:** por zona migrada — la página renderiza con `PageShell` intacto; el flujo núcleo se completa sin pasos nuevos (Hoy: registrar serie → finalizar; Historial: crear/duplicar entreno; Librería: crear/editar ejercicio; Analíticas: subir → revisar → guardar; Biomarcadores/Assessments: filtro → detalle; Trainees: heredar la zona `hs-workout-*` sin crear fork; Identidad: login/register sin cambio de flujo); tests de regresión de esa zona verdes; taps del flujo no aumentan (>4 taps exige justificación explícita) y líneas netas por pantalla no suben; 0 `style="` inline en la zona.
- **Ficheros/tests (5-7):** tests existentes por zona (`TodayWorkoutTests`, `WorkoutsPageTests`, `ExerciseCreationFormTests`, `BloodTestsPageTests`, `AssessmentsPageTests`, `RoutineEditorTests`, `LoginPageTests`) + smoke nuevo por zona migrada.
- **Dependencias:** WI-P1..WI-P10 (y WI-C1..C10 cerrados). **Cierra:** parte de G3.

### WI-Q7 · Gate de cierre global y release
- **Objetivo:** cerrar el rollout con evidencia global, no con 28 evidencias parciales.
- **Acceptance:** greps globales = 0 (hex en `Pages/`+`Components/`, `color: var(--hs-accent)` fuera de la capa de tokens, `box-shadow` en cards, `style="` inline en razor); `@media` reducido a la ladder de 3 escalones de layout; selectores deprecados retirados o documentados; suite verde con recuento ≥246; QA visual sobre la app desplegada con emulación móvil contra el baseline de G0; sin regresión de rendimiento en librería; plan de rollback por fase declarado.
- **Ficheros/tests (2-3):** script/checklist de cierre en `docs/design/` · informe de cierre con salidas de grep y tests adjuntadas.
- **Dependencias:** WI-T1..T8, WI-C1..C10, WI-P1..P10. **Cierra:** G4.

---

## 4. Orden de rollout recomendado

```
G0  baseline visual + suite verde + tokens.json aprobado
    └─ WI-T1 → WI-T2 → { WI-T3 → WI-T4 → WI-T8 | WI-T7 | WI-T5 ∥ WI-T6 }
         [WI-Q1 + WI-Q3 base + WI-Q4 diff 1]  →  G1
    └─ WI-C1 → C2 · C3 → C4 · C5 → C10 · C8 · C9 · C6 · C7
         [WI-Q5 + WI-Q2 + WI-Q4 diff 2]        →  G2
    └─ WI-P1 → { P2, P4, P5, P6, P7, P8 disjuntos } → P3 → P9 → P10
         [WI-Q6 + WI-Q3 páginas + WI-Q4 diff 3] →  G3
    └─ WI-Q7                                     →  G4  (release)
```

**Reglas de ejecución (anti-colisión):**

1. **Nada de código antes de G0.** Los tres mapas citan hexos candidatos (`#1B66E8`, `#98BAF5`, `#868FA0`, `#0E1116`) que salen de `tokens-proposal.md`; solo son despachables cuando t_38c41b61 quede cerrada con esos valores aprobados.
2. **Hotspot `wwwroot/css/app.css` (6109L, tocado por los 28 items):** máximo **2 items en paralelo** y solo con familias de selectores disjuntas. Serializaciones obligatorias ya declaradas por los mapas: `T3→T4→T7`, `C5→C10`, `C6→P10`, `P3→P9`. Prohibido reformatear o renombrar selectores de otra zona.
3. **Una fase = un PR a `main`** con la CI existente (`dotnet test`) en verde; el contrato de tokens se actualiza en el **mismo commit** que el token que cambia.
4. **QA no es una fase al final:** WI-Q1/Q3/Q4 corren dentro de G1, WI-Q2/Q5 dentro de G2, WI-Q6 dentro de G3. Un item WI-T/C/P no se marca done sin su gate asociado.
5. **Dark-first, light sin exponer hasta G2:** hoy no existe `[data-theme=` en el repo (0 hits). WI-T4 entrega el hook y la validación interna del tema light; **la exposición del toggle al usuario es una decisión de producto posterior a G2**, con el mock (`docs/design/redesign-mock.html`) como gate de aprobación visual antes de exponerlo. Motivo: los componentes solo se validan en light a partir de WI-C7/WI-T4, y exponer el tema antes de G2 multiplica la superficie de QA sin beneficio de producto.
6. **Rollback por fase**, revert del merge commit de fase; la fuente única `tokens.json` permite regenerar CSS y C# desde la spec si una fase se revierte.

**Orden mínimo si hay que recortar (camino crítico):** WI-T1 → WI-T2 → WI-T3 → WI-C1 → WI-C2 → WI-C5 → WI-P1 → WI-P2 → WI-Q7. Es el tramo que cierra los 4 hallazgos HIGH de la auditoría (H1 CTA, H2 accent-como-texto, H3 FAB, H4 spec defectuosa) con el menor número de items; el resto (light real, charts, densidad chip/badge, escalera completa de breakpoints) se puede entregar en fases posteriores sin bloquear el release.

---

## 5. Riesgos abiertos y dependencias

- **Valores finales de tokens pendientes:** t_38c41b61 sigue en `todo` con hijos t_68a74222 (running) y t_ba764d7a (todo). Ningún gate de G1 puede cerrarse con valores candidatos.
- **QA visual logueado:** requiere credenciales de Google OAuth guardadas en vault; no hay cuenta seed local. El QA visual de G0/G4 necesita esa credencial o se limita a páginas públicas (login) + markup bUnit.
- **Coste del gate visual:** sin visual-diff automático, cada diff de fase es trabajo manual asistido (~1 pase por fase). Si se quiere automático, la decisión es añadir Playwright/screenshot-diff como work item aparte (fuera del alcance de este mapa; lo decide pau).
- **Suite flaky pre-existente:** `ExerciseTypesPageTests` en run completo. Baseline de hoy 246/246; un fallo aislado de esa clase se confirma corriéndola sola antes de atribuirlo al rollout.
- **Hotspot declarado:** `wwwroot/css/app.css` (flagado ya en los comentarios de t_ee4c71df y t_5f21d034). Este mapa añade la regla de serialización y el límite de 2 items en paralelo.

## 6. Fuera de alcance de este mapa

- Valores finales de tokens y ladder → WI-T1..T4 / t_38c41b61.
- Accesibilidad **de la propuesta** (contraste teórico, targets declarados, dynamic type) → t_68a74222 / `docs/design/accessibility-checklist.md`.
- Implementación de tests (código) → pau, tras despachar los items WI-Q1..WI-Q7.
- Decisiones de producto (exponer tema light, glow del FAB, FAB→CTA en sidebar) → propuesta t_38c41b61 / Wall.
- Cambios de flujo, IA u onboarding → propuesta t_38c41b61.

**Checklist de aceptación de este entregable:** 7 items QA con objetivo + acceptance medible + ficheros/tests 2-7 (todos ≤8) + dependencias explícitas sobre items WI-T/WI-C/WI-P + owner qa · 5 gates de fase con criterios verificables y stop-conditions · orden de rollout integrado con los tres mapas y reglas de serialización · sin código y sin cambios en componentes, páginas ni CSS (verificable: `git status` solo añade `docs/design/qa-gates-rollout-t_b155320d.md`).
