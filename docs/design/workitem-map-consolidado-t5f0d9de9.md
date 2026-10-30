# Mapa consolidado de work items — rollout del nuevo design system (t_5f0d9de9, 2026-10-23)

**Objetivo:** un único mapa despachable que fusiona los cuatro mapas entregados (tokens `workitems-tokens-base-css-t57a0e89b.md`, componentes `workitems-componentes-design-system-tee4c71df.md`, páginas `workitems-migracion-paginas-t5f21d034.md`, QA `qa-gates-rollout-t_b155320d.md`) con la propuesta ya cerrada. 35 work items, todos ≤8 ficheros, sin código, sin duplicados, con dependencias y orden de ejecución. Este documento es la fuente para crear las tarjetas kanban tras la aprobación de Wall (root t_58fd2f34).

**Estado de la propuesta:** t_38c41b61 **cerrada** (2026-10-23 13:43) con valores finales verificados en `docs/design/new-design-system-proposal.md` (101 comprobaciones WCAG propias + verificación QA independiente 175 pares ±0.02). Los candidatos citados por los cuatro mapas quedan **confirmados o sustituidos** por los valores finales:

| Token | Valor final (dark / light) | Nota |
|---|---|---|
| `--hs-accent` (fills ONLY) | #1B66E8 / #1B66E8 | confirmado |
| `--hs-accent-ink` (texto) | #98BAF5 / #1450B8 | confirmado |
| `--hs-on-done` / ink sobre fills saturados | #0E1116 | confirmado; ink SOLO sobre fills saturados, nunca sobre washes |
| `--hs-t3` | #868FA0 / **#5D6878** | light corregido por QA P1-10 |
| `--hs-err-ink` | **#FA8088** / #C22B33 | **sustituye** al candidato #F4636A (fix QA P0-1) |
| `--hs-wash-err` | rgba(241,84,91,.14) → compuesto #35232A | alpha unificado 14 % (fix QA P1-5/6) |
| `--hs-focus-ring` | outline 2px accent-ink (≥3.0 sobre toda la ladder) | token nuevo (fix QA P0-2) |
| Breakpoints | 480 / 768 / 1000 | confirmado |

**Bloqueante G0 (único que queda):** aprobación de Wall de la propuesta + gate de mock light (`redesign-mock.html` renderizado con la paleta light por ux-designer, 1 fichero, despachable ya) + baseline visual capturado ANTES del primer commit de CSS (WI-Q4 parte 1). La suite baseline 246/246 ya está registrada.

---

## Decisiones de consolidación (orquestador)

1. **WI-T8 fusionado en WI-Q1.** Ambos definen el mismo entregable (contrato tokens.json↔app.css↔HealthspanTheme.cs + test de contraste WCAG) sobre los mismos ficheros (`SharedVisualFoundationTests.cs`, `ContrastContractTests.cs` nuevo, `tokens.json`). Un solo item, owner **qa**, dependencias WI-T1..T4. Total: 35 items (8 T + 10 C + 10 P + 7 Q).
2. **WI-T9 nuevo** (no estaba en ningún mapa): guards de accesibilidad CSS base exigidos por la propuesta §11.7 — `@media (hover: hover)` en las 17 reglas hover + bloque `forced-colors`. 1-2 ficheros.
3. **Fixes vivos de la propuesta §11 asignados** (ningún fix queda huérfano): 1 ErrorContrastText #0E1116 → **C8** · 2 accent+5 hexos razor+`--hs-shadow-focus` → **T3+C9** (mismo commit de fase) · 3 gap settable 4→8px + tokens `--hs-target/-dense/-gap-target` → **T1** (declaración) + **C4** (aplicación) · 4 sheets Escape+focus trap+restore → **C8** · 5 `--hs-shadow-sheet`+borde superior → **C8** · 6 font-size px→rem + settable en em → **T6+C4** · 7 hover guards+forced-colors → **T9** · 8 PaletteLight real → **T4** · 9 breakpoints 15→ladder → **T5**.
4. **Dueño único por zona:** cada item es dueño exclusivo de su familia de selectores en `app.css` y de sus ficheros razor; ningún item migra dos zonas. Cadenas de serialización por fichero compartido en §Reglas anti-colisión.
5. **Owners normalizados:** ux-designer → WI-T1 (+ gate de mock light G0). pau → WI-T2..T7, T9, C1..C10, P1..P10. qa → WI-Q1..Q7.
6. **Límite de ficheros verificado:** máximo 7 (C9, P9, P10, Q6); todos ≤8. Ningún item contiene dos cambios independientes.

---

## Fase 1 · Tokens y CSS base (cierra G1)

| ID | Título | Owner | Ficheros | Deps directas | Acceptance (resumen) |
|---|---|---|---|---|---|
| WI-T1 | Spec machine-readable: DESIGN.md v2 + tokens.json DTCG | ux-designer | 2 (DESIGN.md, tokens.json) | G0 (propuesta aprobada) | Todo token de `:root` nombrado; valores dark+light finales de `new-design-system-proposal.md` §2-§6 (incluye focus-ring, on-done, washes, target/gap tokens); tokens.json valida como DTCG |
| WI-T2 | Fuente única: tokens.json alimenta app.css + HealthspanTheme.cs | pau | 4 (tokens.json, app.css, HealthspanTheme.cs, SharedVisualFoundationTests.cs) | T1 | Cambiar un hex en tokens.json y regenerar sincroniza CSS+C# sin edición manual; cero hex duplicado fuera del generado; suite verde |
| WI-T3 | Paleta dark AA (accent #1B66E8, t3 #868FA0, err-ink #FA8088, on-done #0E1116, shadow-focus regenerado) | pau | 3 (app.css, HealthspanTheme.cs, SharedVisualFoundationTests.cs) | T1, T2 | Tabla de contrastes WCAG en la spec; contrato actualizado **en el mismo commit** (rompe deliberadamente :90-130); cero selector de componente tocado |
| WI-T4 | Paleta light real (elimina hack PaletteLight=dark HealthspanTheme.cs:30-33) | pau | 4 (HealthspanTheme.cs, app.css, tokens.json, SharedVisualFoundationTests.cs) | T1, T3 | PaletteLight ≠ PaletteDark; bloque `[data-theme="light"]` de tokens; regresión dark intacta; validación interna sin exponer el toggle (regla 5 de rollout) |
| WI-T5 | Escalera de breakpoints 480/768/1000 (29 @media → ladder) | pau | 1-2 (app.css) | T1 | Solo 3 valores de layout; nav→sidebar exacto a 1000px; queries no-layout intactas; checklist QA por bloque (29 bloques, riesgo declarado) |
| WI-T6 | Tipografía: escala 8 pasos tokenizada + aguanta font scaling 200 % | pau | 3 (app.css, index.html, HealthspanTheme.cs) | T1 | Escala completa en `:root`; 11 font-size px→rem (§11.6); tabular-nums global intacto; sin overflow al 200 % en 360px |
| WI-T7 | Limpieza alias legacy `--hs-color-*`→tokens v1 (app.css:23-36) | pau | 2 (app.css, SharedVisualFoundationTests.cs) | T2 | Cero `var(--mud-palette-*)` fuera de la capa theme; consumidores leen tokens v1 |
| WI-T9 | Guards de interacción: `@media (hover: hover)` en 17 hover rules + bloque `forced-colors` | pau | 1-2 (app.css) | T1 | Hover no sticky en táctil; bloque forced-colors con estados de foco y semántica; anclas prefers-reduced-motion intactas |

## Fase 2 · Componentes (cierra G2)

| ID | Título | Owner | Ficheros | Deps directas | Acceptance (resumen) |
|---|---|---|---|---|---|
| WI-C1 | CTA primario/secundario/danger: accent-fill vs accent-ink + token disabled | pau | 4-5 (app.css, TodayWorkout.razor, TodaySessionSummary.razor, HealthspanTheme.cs, tests) | T1, T3 | Label ≥4.5:1 dark+light; `:disabled` por token sin opacity (app.css:5638 desaparece); taxonomía §7 propuesta (primary/secondary/danger err-ink) |
| WI-C2 | FAB y FAB-done: ink #0E1116 sobre fill done (H3) | pau | 2-3 (app.css, MainLayout.razor, tokens.json) | T1, T3, **decisión Wall §12.1-2** | Icono/label ≥4.5:1 (7.94); glow según decisión Wall (eliminado o token único); target ≥44px; sin sombra nueva |
| WI-C3 | Chip interactivo 44px vs badge estático + variantes wash | pau | 6 (app.css, TodayExerciseLog, TodaySessionSummary, ExerciseCreationForm, ClinicalStatusChip, TodayWorkout) | T1, T3 | Ningún badge estático de 44px; chips táctiles ≥44px; washes ≥4.5:1; borde chip puro dark / *-ink light (fix QA P0-4) |
| WI-C4 | Set table: tokens + tertiary legible + gap 8px + em a 200 % fuente | pau | 2-3 (app.css, TodayExerciseLog.razor, tests) | T1, T3, T6 | tertiary 5.31; gap 4→8px con tokens target reales (§11.3); fila apilada ≥150 % fuente sin overflow |
| WI-C5 | Navegación: tabs/topbar/sidebar/sheet con accent-ink activo | pau | 3 (app.css, MainLayout.razor, NavMenu.razor) | T1, T3 | Tab/nav activo ≥4.5:1; fill selección ≥3:1; backdrop y z-index intactos; breakpoint 1000px sin tocar (es T5) |
| WI-C6 | Inputs y formularios: contrato MudBlazor→tokens | pau | 5-6 (app.css, HealthspanTheme.cs, ExerciseCreationForm, ExerciseForm, ExerciseTypeForm, Login.razor) | T1, T2, T6 | Cero mud-palette en selectores de componente; inputs radius/border/focus token y ≥44px; focus-ring ≥3.0 sobre input (fix QA P0-2); 16 consumidores MudTextField sin editar |
| WI-C7 | Cards y hero metrics: superficie sin sombras (ladder + borde 1px) | pau | 5-6 (app.css, WorkoutSessionCard, ReadinessCard, AdaptationSummaryPanel, WeeklyTrainingSummaryPanel, Workouts.razor solo clases historia) | T1, T3, T4 | Cero box-shadow en cards; ladder idéntico dark/light; hero con escala tipográfica |
| WI-C8 | Diálogos, sheets y estados (empty/error/loading) + fixes §11.1/4/5 | pau | 5 (app.css, FinishSessionDialog, ExerciseFilterDialog, EmptyState, ErrorState; Workouts.razor bloque sheet :327-421) | T1, T3 | Sheets Escape+focus trap+foco inicial+restore; `--hs-shadow-sheet`+borde superior; `palette.ErrorContrastText=#0E1116`; error con icono+texto ≥4.5:1; cierre ≥44px |
| WI-C9 | Charts y data-viz: cero hexos hardcodeados (M1) | pau | 7 (MuscleLoadBodyMap, TrendChart, SparklineChart, Biomarkers, Assessments, ExerciseTypeDetails, HealthspanTheme.cs) | T1, T2, T3 | grep hex en Components/+Pages/ = 0; rampa readiness 3 pasos con valores finales; gráficos light usan *-ink (fix QA P0-3) |
| WI-C10 | Sync indicator, toolbars y shell suelto + selectores deprecados | pau | 2-3 (app.css, SyncStatusIndicator.razor, DESIGN.md) | T1, T3, C5 | Cero `color: var(--hs-accent)` fuera de capa tokens; hs-widget-card/hs-table-stack/hs-today-rest retirados o documentados con grep |

## Fase 3 · Migración de páginas (cierra G3)

| ID | Título | Owner | Ficheros | Deps directas | Acceptance (resumen) |
|---|---|---|---|---|---|
| WI-P1 | Plantilla de página: shell, header, hero, toolbar | pau | 5 (app.css, PageShell, PageHeader, PageToolbar, tests) | T1, T3, T4, T5, C1, C7 | grep zona=0; ladder solo; título/subtítulo ≥4.5:1 dark+light; 26 consumidores PageShell sin editar razor |
| WI-P2 | Hoy (sesión de entrenamiento, zona hs-today-* 76 selectores) | pau | 3 (app.css, TodayWorkout.razor, TodayExerciseItem) | P1, T5, C1, C2, C3, C4, C8 | grep zona=0; empty state WI-C8; targets ≥44px; checklist 360/480/768/1024 |
| WI-P3 | Historial (Workouts + CreateWorkout; zona hs-workout-*/hs-history-*) | pau | 3 (app.css, Workouts.razor, CreateWorkout.razor) | P1, T5, C3, C7, C8 | grep zona=0; cero box-shadow; aria-live:140 intacto; tabla CreateWorkout tokenizada |
| WI-P4 | Librería de ejercicios (listado + crear/editar) | pau | 5 (app.css, Exercises, CreateExercises, EditExercise, ExerciseTypeGrid) | P1, T5, C3, C6, C7, C8 | grep zona=0; badges compactos en grid; forms sin cambio de flujo (tests verdes) |
| WI-P5 | Detalle de tipo de ejercicio (+ páginas de tipos) | pau | 6 (app.css, ExerciseTypeDetails, ExerciseTypes, TrainedExerciseTypes, CreateExerciseTypes, EditExerciseType) | P1, T5, C5, C7, C9 | grep hex fichero=0 (los hexos los cerró C9); KPI con escala tipográfica; tabs accent-ink |
| WI-P6 | Salud: overview, insights y perfil personal | pau | 4 (app.css, Dashboard, Profile, HealthProfile) | P1, T4, T5, C7, C9 | grep 3 zonas=0; rampa readiness con constantes C9; insights legible en light real |
| WI-P7 | Salud: analíticas (BloodTests, BloodTestDetails, ReviewBloodTest) | pau | 4 (app.css, BloodTests, BloodTestDetails, ReviewBloodTest) | P1, T5, C6, C7, C8 | grep=0; status wash+ink ≥4.5:1; tabular-nums en review grid; progreso announced |
| WI-P8 | Salud: biomarcadores y assessments | pau | 4 (app.css, Biomarkers, Assessments, tests) | P1, T5, C3, C7, C8, C9 | grep=0 (hexos locales los cerró C9); trend/sparkline sin edición propia; hs-inline-empty→C8 |
| WI-P9 | Trainees (flujo doble persona, sin fork hs-workout-*) | pau | 7 (app.css, Trainees, TraineeExercises, TraineeFavExerciseTypes, TraineeRoutines, TraineeRoutineEditor, RoutineEditor) | P1, **P3**, T5, C6, C7, C8 | grep hs-routine-*=0; cero selectores fork trainee (grep); aria-live:41 intacto; tests de rutina verdes |
| WI-P10 | Ajustes, mis rutinas e identidad (Settings, MyRoutines, login) | pau | 7 (app.css, Settings, MyRoutines, Login, Register, Logout, LoginLayout) | P1, **P9**, T5, C6, C7, C8 | grep hex=0 y `style="`=0 en Login.razor; login card sin sombra; flujo identidad sin cambios |

## Fase 4 · QA (gate por fase, no al final)

| ID | Título | Owner | Ficheros/tests | Deps directas | Cierra |
|---|---|---|---|---|---|
| WI-Q1 | Gate de cobertura de tokens (contrato + contraste automatizado; absorbe WI-T8) | qa | 2-3 (SharedVisualFoundationTests.cs, ContrastContractTests.cs nuevo, tokens.json) | T1, T2, T3, T4 | G1 |
| WI-Q2 | Gate de accesibilidad implementada (targets medidos, foco, font scaling, disabled sin opacity) | qa | 3-5 (TouchTargetHarnessTests.cs extendido, AccessibilityContractTests.cs nuevo, checklists) | T3, T4, T6, C1..C8 | parte G2 |
| WI-Q3 | Gate responsive (ladder única + checklist 360/480/768/1024 + emulación 400×850) | qa | 2-4 (test estático @media, checklist, capturas) | T5 (G1) · P1..P10 (G3) | parte G1 y G3 |
| WI-Q4 | Gate de regresión visual (baseline G0 + diff asistido tras G1/G2/G3) | qa | 3-6 (docs/design/qa-baseline/, script captura, checklist defectos) | G0 baseline; T/C/P por fase | parte G1, G2, G3 |
| WI-Q5 | Gate de paridad de componentes (catálogo×estado×tema, matriz §8) | qa | 4-6 (tests bUnit por familia, checklist matriz) | C1..C10 | G2 |
| WI-Q6 | Gate de smoke de migración por zona (flujos núcleo sin pasos nuevos) | qa | 5-7 (tests por zona + smoke nuevo) | P1..P10 | parte G3 |
| WI-Q7 | Gate de cierre global y release (greps globales=0, ladder, QA sobre SWA, rollback por fase) | qa | 2-3 (checklist cierre, informe) | T1..T9, C1..C10, P1..P10 | G4 |

---

## Orden de ejecución (waves)

```
G0  aprobación Wall + mock light (ux-designer, 1 fichero) + baseline visual WI-Q4 + suite verde
    └─ FASE 1: T1 → T2 → { T3 → T4 | T7 } ∥ { T5 ∥ T6 ∥ T9 }
         gate: Q1 + Q3(base) + Q4(diff 1)  →  G1
    └─ FASE 2: C1 → C2 · C3 → C4 · C5 → C10 · C8 · C9 · C6 · C7
         gate: Q5 + Q2 + Q4(diff 2)        →  G2
    └─ FASE 3: P1 → { P2, P4, P5, P6, P7, P8 disjuntos } → P3 → P9 → P10
         gate: Q6 + Q3(páginas) + Q4(diff 3) →  G3
    └─ FASE 4: Q7                            →  G4 (release)
```

**Stop-conditions (de t_b155320d):** suite roja · contrato de tokens roto · contraste <4.5:1/<3:1 en par tocado · breakpoint nuevo fuera de ladder · flujo Hoy/Historial con más taps · regresión visual sin justificación de spec → se para la fase.

**Regla de PR:** una fase = un PR a main con CI (`dotnet test`) verde; el test de contrato se actualiza en el mismo commit que el token que cambia. Suite baseline 246/246; flaky pre-existente `ExerciseTypesPageTests` se confirma aislada antes de atribuirla al rollout.

## Reglas anti-colisión (hotspot `wwwroot/css/app.css`, 6109L, tocado por 27 items)

- Máximo **2 items en paralelo**, solo con familias de selectores disjuntas. Prohibido reformatear/renombrar selectores de otra zona.
- Serializaciones obligatorias por fichero compartido:
  - app.css: `T3→T4→T7` · `C5→C10`
  - Workouts.razor: `C7/C8 (clases historia / bloque sheet :327-421) → P3 → P9`
  - TodayWorkout.razor: `C1/C3/C4 → P2` (P2 único dueño tras los componentes)
  - Login.razor: `C6 (inputs :2101-2300) → P10 (layout)`
  - Biomarkers/Assessments/ExerciseTypeDetails.razor: `C9 (hexos) → P5/P8 (zona)` — P5/P8 solo verifican grep=0, no re-tocan hexos.

## Cobertura (ningún hallazgo ni fix huérfano)

- Auditoría: H1→C1 · H2→C5,C10 · H3→C2 · H4→C1,T1 · M1→C9 · M2→C4 · M3→T5 · M4→T7 · M5(triple fuente)→T2 · M6(spec prosa)→T1 · L1→C2,C7 · L2→C3 · L3→C1.
- Propuesta §11 (9 fixes vivos): asignados en Decisión 3. §11.7 → WI-T9 (nuevo).
- QA P0/P1 de `accessibility-checklist.md`: err-ink→T3/C1 · focus-ring→T1/C6 · light *-ink→T4/C9 · borde chip→C3 · wash err 14 %→T1 · ink acotado→T1/C2 · ErrorContrastText→C8 · sheet shadow+borde+Escape/trap→C8 · t3 light→T1/T4 · timer paused t2→C4.

## Decisiones previas al dispatch (Wall / producto)

1. **FAB→CTA primario en sidebar ≥1000px** (recomendación UX: sí) — bloquea el alcance de WI-C2 y WI-C5; decidir antes de despachar Fase 2.
2. **Glow del FAB**: eliminar (recomendación UX) o token único `--hs-fab-glow` — afecta a WI-C2.
3. **Mock light** en `redesign-mock.html` antes del primer commit (ux-designer, 1 fichero) — entrada de G0.
4. Gate visual sin visual-diff automático: diff asistido por fase (~1 pase). Automatizarlo (Playwright/screenshot-diff) es decisión de producto aparte, no está en este mapa.
5. QA visual logueado requiere credencial Google OAuth en vault (no hay cuenta seed).

## Camino crítico mínimo (si hay que recortar)

`T1 → T2 → T3 → C1 → C2 → C5 → P1 → P2 → Q7` — cierra los 4 HIGH de la auditoría (H1 CTA, H2 accent-texto, H3 FAB, H4 spec) con el menor número de items; light real, charts, densidad chip/badge y escalera completa quedan para fases posteriores sin bloquear release.

---

**Checklist de aceptación de este entregable:** 35 items (8 T + 10 C + 10 P + 7 Q) · todos ≤8 ficheros (máx 7) · dedupe verificado (T8→Q1; hexos razor propiedad exclusiva de C9; zonas de página de propiedad exclusiva P1..P10) · ningún item con dos cambios independientes · dependencias referencian IDs existentes y el grafo es acíclico · orden tokens→componentes→páginas→QA con gates G0..G4 y stop-conditions · sin código (este documento y los 4 mapas de origen son los únicos artefactos).
