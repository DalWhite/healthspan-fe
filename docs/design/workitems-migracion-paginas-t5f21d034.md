# Mapa de work items — Migración de páginas (t_5f21d034, 2026-10-23)

**Objetivo:** dividir la migración de las 26 páginas con ruta + identidad a las familias de componentes y tokens nuevos en work items despachables, cada uno ≤5-8 ficheros, sin código — especificaciones para despachar **después** de los mapas de tokens (WI-T1..T8) y componentes (WI-C1..C10).

**Inputs:** propuesta del design system t_38c41b61 (tokens-proposal.md con valores ya calculados: accent `#1B66E8`, accent-ink `#98BAF5`, t3 `#868FA0`, ink sobre fills `#0E1116`, ladder light espejo, breakpoints 480/768/1000; QA t_68a74222 y cierre t_ba764d7a aún abiertos → valores pendientes de aprobación final) + mapa de tokens `docs/design/workitems-tokens-base-css-t57a0e89b.md` (WI-T1..T8) + mapa de componentes `docs/design/workitems-componentes-design-system-tee4c71df.md` (WI-C1..C10) + auditoría `docs/design/audit-design-system-v1-t511ecd88.md` (M3 escalera de breakpoints).

**Qué es "migración de página" aquí:** migrar las clases de zona de cada página (`hs-today-*`, `hs-workout-*`, `hs-review-*`, …) y sus razor al contrato nuevo (superficies sin sombra, chip/badge split, accent-ink, ladder de 3 escalones, targets 44px). **No** se re-migra nada que ya poseen los items de componentes: CTA/chips/set-table/FAB/nav/inputs/cards/diálogos/charts quedan cerrados por WI-C1..C10 y sus consumidores razor; los items de página los declaran como dependencia y no los tocan.

**Verificación hecha en código (2026-10-23, grep sobre `Pages/`, `Components/`, `Shared/`, excluyendo `.worktrees/` y `obj/`):**
- 26 páginas con `@page` + Login/Register/Logout (36 rutas, varias con alias `/training/*`). Todas las páginas de app usan `PageShell`/`PageHeader`/`PageToolbar` (26 consumidores confirmados).
- Selectores `.hs-*` por familia en app.css (6109L): exercise 109 · workout 79 · today 76 · review 55 · profile 55 · biomarker 40 · insights 39 · training 35 · history 28 · health 28 · assessment 25 · page 24 · routine 16 · list 14 · login 9.
- Páginas más grandes (líneas razor): TodayWorkout 1425 · Workouts 1388 · ExerciseTypeDetails 936 · ReviewBloodTest 703 · BloodTests 676 · BloodTestDetails 564 · Biomarkers 562 · Exercises 522.
- Hexos hardcodeados restantes en páginas: ExerciseTypeDetails.razor:526/749, Biomarkers.razor:535-536, Assessments.razor:414 → **los elimina WI-C9**, no los toca ningún item de página.
- Páginas sin clases propias (renderizan vía componentes, solo se verifican): ExerciseTypes.razor, TrainedExerciseTypes, TraineeFavExerciseTypes, TraineeRoutineEditor.
- `TraineeExercises.razor` (304L) reutiliza 26 selectores `hs-workout-*` de Workouts.razor → comparten zona; se serializa P3→P9 y P9 no puede crear selectores fork.
- Bloque login app.css:2101-2300 es zona de WI-C6 (inputs) + WI-P10 (layout/card); Login.razor tiene 3 `style="…"` inline a retirar.

## Regla de migración común a todos los items

| Viejo | Nuevo | Motivo |
|---|---|---|
| clase de zona con hex o `var(--hs-accent)` como texto | token `--hs-*` de la fuente única / `--hs-accent-ink` | propaga desde tokens.json; cierra H2 en la zona |
| card/panel propio con `box-shadow` o borde ad-hoc | contrato WI-C7 (surface ladder + 1px border + radius token, shadow none) | L1 en todas las páginas |
| chip 44px usado como marca estática | `.hs-badge` compacto (WI-C3) | densidad en listas de historial/insights (L2) |
| `@media` ad-hoc dentro de la zona (420, 599/600, 699/700, 760, 800, 950, 959/960…) | ladder 480/768/1000 (WI-T5) | cierra M3; ningún item de página añade breakpoints nuevos |
| `style="…"` inline en razor | clase tokenizada en app.css | una sola fuente de estilo |
| estados empty/error/loading propios | contrato WI-C8 | consistencia entre páginas |

**Ningún item migra dos zonas ajenas ni reformatea selectores que no le pertenecen.** Cada item es dueño exclusivo de su familia de selectores en app.css.

---

## Work items (páginas)

### WI-P1 · Plantilla de página: shell, header, hero y toolbar
- **Objetivo:** migrar la plantilla compartida que usan las 26 páginas al contrato nuevo antes de migrar cualquier zona: `.hs-page-shell` app.css:305, `.hs-page-header` :317-357, `.hs-page-title` :329, `.hs-page-hero` :2323/:3310, `.hs-page-toolbar` :381-391, bloques header :607-636.
- **Qué migra:** densidad y tipografía del header (escala WI-T6), hero como hero-metrics del catálogo (wash + token, sin sombra), toolbar con targets 44px, responsive de la plantilla solo sobre la ladder 480/768/1000.
- **Acceptance:** cero hex y cero `var(--mud-palette-*)` en la zona `hs-page-*`/`hs-hero` (grep por zona); los `@media` de la plantilla son solo escalones 480/768/1000; título ≥4.5:1 y subtítulo t2 ≥4.5:1 en dark y light; las 26 páginas que usan PageShell renderizan sin cambios en sus razor; `dotnet test` en verde.
- **Ficheros (5):** wwwroot/css/app.css · Components/PageShell.razor · Components/PageHeader.razor · Components/PageToolbar.razor · tests/…/SharedVisualFoundationTests.cs
- **Dependencias:** WI-T1, WI-T3, WI-T4, WI-T5, WI-C1, WI-C7. **Owner:** pau.
- **Outcome:** base común de todas las páginas; sin esto cada zona reinventaría su header.

### WI-P2 · Hoy (sesión de entrenamiento)
- **Objetivo:** migrar la zona `hs-today-*` (76 selectores) y TodayWorkout.razor (1425L) al contrato nuevo: blocks de sesión, session bar, switcher, streak, recent, activation y empty.
- **Qué migra:** `.hs-today-block*` :5771+, `.hs-today-session*`, `.hs-today-recent*`, `.hs-today-activation`, `.hs-today-empty*` a superficies WI-C7 y washes tokenizados; chips de block como badge compacto (WI-C3); tertiary `#868FA0` en meta de recent (WI-T6). **No toca** CTA (:59), chips de log ni set table (WI-C1/C3/C4) ni TodayExerciseLog/TodaySessionSummary (ya migrados).
- **Acceptance:** grep hex=0 y `color: var(--hs-accent)`=0 en la zona; ningún `@media` nuevo en `hs-today-*` (ladder solo); empty state con contrato WI-C8; targets ≥44px en add/intent; streak y session bar legibles ≥4.5:1 dark+light; `dotnet test` en verde; checklist visual 360/480/768/1024.
- **Ficheros (3):** wwwroot/css/app.css · Pages/Exercises/TodayWorkout.razor · Components/TodayExerciseItem.razor
- **Dependencias:** WI-P1, WI-T5, WI-C1, WI-C2, WI-C3, WI-C4, WI-C8. **Owner:** pau.
- **Outcome:** la pantalla más usada de la app (1425L) queda entera bajo el contrato nuevo.

### WI-P3 · Historial de entrenamiento (Workouts + crear entreno)
- **Objetivo:** migrar `hs-workout-*` (79) + `hs-history-*` (28) y Workouts.razor (1388L): lista de sesiones, calendario, tiles, filtros y acciones; y la tabla MudTable de CreateWorkout.razor:29-45.
- **Qué migra:** session cards a contrato WI-C7 (WorkoutSessionCard.razor ya tocado por C7 — no re-editar); calendario/tiles con washes semánticos tokenizados; panel de filtros como sheet WI-C8; acciones duplicate/edit/add ≥44px; tabla de CreateWorkout con radius/border/focus token y header ≥4.5:1.
- **Acceptance:** grep hex=0 en la zona; cero `box-shadow` en `hs-history-*`/`hs-workout-*`; `aria-live="polite"` de Workouts.razor:140 intacto; expand/collapse de ejercicios conserva comportamiento; `dotnet test` en verde.
- **Ficheros (3):** wwwroot/css/app.css · Pages/Workouts.razor · Pages/CreateWorkout.razor
- **Dependencias:** WI-P1, WI-T5, WI-C3, WI-C7, WI-C8. **Owner:** pau.
- **Outcome:** historial denso y legible; la zona `hs-workout-*` queda lista para que TraineeExercises (P9) la reutilice sin fork.

### WI-P4 · Librería de ejercicios (listado + crear/editar)
- **Objetivo:** migrar `hs-exercise-grid`, `hs-main-exercise-card`, `hs-rep-chip`/`hs-volume-chip`, `hs-exercise-create-*`, `hs-exercise-edit-*` y sus 4 ficheros.
- **Qué migra:** Exercises.razor (522L) grid a WI-C7 + chips de métricas como badge compacto (WI-C3); CreateExercises.razor y EditExercise.razor (390L) superficies a WI-C7 con inputs WI-C6 (sin cambio de estructura ni flujo); ExerciseTypeGrid.razor (562L) cards a WI-C7; confirmación de delete a diálogo WI-C8.
- **Acceptance:** grep hex=0 en la zona; ningún badge estático de 44px en el grid (densidad L2); forms sin cambios funcionales (tests de creación/edición en verde); `dotnet test` en verde.
- **Ficheros (5):** wwwroot/css/app.css · Pages/Exercises/Exercises.razor · Pages/Exercises/CreateExercises.razor · Pages/Exercises/EditExercise.razor · Components/ExerciseTypeGrid.razor
- **Dependencias:** WI-P1, WI-T5, WI-C3, WI-C6, WI-C7, WI-C8. **Owner:** pau.
- **Outcome:** CRUD de ejercicios coherente con el catálogo; los formularios no cambian de flujo.

### WI-P5 · Detalle de tipo de ejercicio
- **Objetivo:** migrar la zona `hs-exercise-detail-*`/`hs-exercise-hero`/`hs-exercise-kpi*`/`hs-exercise-history-*`/`hs-exercise-trend*`/`hs-exercise-related*` (ExerciseTypeDetails.razor, 936L) y las páginas de tipos (ExerciseTypes, TrainedExerciseTypes, CreateExerciseTypes, EditExerciseType — las dos primeras sin clases propias: verificar render, no editar).
- **Qué migra:** hero + KPI grid como hero-metrics del catálogo; tabs de detalle con accent-ink activo (patrón WI-C5); historia de sesiones con badges (WI-C3); trend/overview/related cards con WI-C7. **Los hexos del fichero (:526/:749) los elimina WI-C9** — P5 no los vuelve a tocar, solo verifica grep=0.
- **Acceptance:** grep hex=0 en el fichero; KPI value/label con escala tipográfica WI-T6; tabs activos ≥4.5:1; cards sin sombra; `dotnet test` en verde.
- **Ficheros (6):** wwwroot/css/app.css · Pages/ExerciseTypes/ExerciseTypeDetails.razor · Pages/ExerciseTypes/ExerciseTypes.razor · Pages/ExerciseTypes/TrainedExerciseTypes.razor · Pages/ExerciseTypes/CreateExerciseTypes.razor · Pages/ExerciseTypes/EditExerciseType.razor
- **Dependencias:** WI-P1, WI-T5, WI-C5, WI-C7, WI-C9. **Owner:** pau.
- **Outcome:** la ficha más rica de la app (936L) unificada al contrato; sin fork de "cards de análisis".

### WI-P6 · Salud: overview, insights y perfil personal
- **Objetivo:** migrar `hs-health-overview-*`, `hs-readiness-panel`, `hs-coverage-panel`, `hs-attention-panel` (Dashboard.razor, 396L), `hs-insights-*` (39 selectores; Profile.razor, 334L) y `hs-personal-profile-*` (HealthProfile.razor, 232L).
- **Qué migra:** panels a WI-C7; rampa readiness low/mid/high con las constantes de WI-C9 (no hexos locales); KPIs insights como hero-metrics; ranked rows densas con badge (WI-C3); cobertura/radar sin sombra (ReadinessCard.razor y AdaptationCoverageRadar ya tocados por C7 — verificar, no re-editar).
- **Acceptance:** grep hex=0 en las tres zonas; la rampa readiness conserva 3 pasos semánticos con valores de tokens.json; insights legible en light real (WI-T4); `dotnet test` en verde.
- **Ficheros (4):** wwwroot/css/app.css · Pages/Health/Dashboard.razor · Pages/Health/Profile.razor · Pages/Health/HealthProfile.razor
- **Dependencias:** WI-P1, WI-T4, WI-T5, WI-C7, WI-C9. **Owner:** pau.
- **Outcome:** insights y readiness se leen igual en hoy, salud e insights — jerarquía por superficie.

### WI-P7 · Salud: registros de analíticas (blood tests)
- **Objetivo:** migrar `hs-list-card*`, `hs-upload-*`, `hs-marker-*`, `hs-status-card-*` y `hs-review-*` (55 selectores) en BloodTests.razor (676L), BloodTestDetails.razor (564L) y ReviewBloodTest.razor (703L).
- **Qué migra:** list cards a WI-C7; status pending/problem/processed con wash + ink semántico ≥4.5:1 (no color solo); dropzone/upload con inputs WI-C6; grid de revisión con tabular-nums, range/result fields tokenizados y save bar sticky; split desktop/mobile de review solo sobre escalones de la ladder; delete confirmation a WI-C8.
- **Acceptance:** grep hex=0 en las tres páginas; `hs-review-grid` numérico alineado (tabular-nums heredado, app.css:89); danger-zone ink ≥4.5:1 dark+light; progreso de revisión announced (no solo color); `dotnet test` en verde.
- **Ficheros (4):** wwwroot/css/app.css · Pages/Health/BloodTests.razor · Pages/Health/BloodTestDetails.razor · Pages/Health/ReviewBloodTest.razor
- **Dependencias:** WI-P1, WI-T5, WI-C6, WI-C7, WI-C8. **Owner:** pau.
- **Outcome:** el flujo OCR→revisión→guardar, el más denso en inputs, queda bajo un solo contrato.

### WI-P8 · Salud: biomarcadores y assessments
- **Objetivo:** migrar `hs-biomarker-*` (40) y `hs-assessment-*` (25) en Biomarkers.razor (562L) y Assessments.razor (542L).
- **Qué migra:** period filters y filter toggles como chip/badge según WI-C3; listas de mediciones y accordion de assessments a WI-C7; current/detail con tertiary `#868FA0` (WI-T6); soft alert y danger zone con ink WI-C8. **Los hexos locales (:535-536 y :414) los elimina WI-C9.**
- **Acceptance:** grep hex=0 en ambas páginas; trend/sparkline renderizan con las constantes de WI-C9 sin edición propia; `hs-inline-empty` sustituido por el contrato WI-C8; `dotnet test` en verde.
- **Ficheros (4):** wwwroot/css/app.css · Pages/Health/Biomarkers.razor · Pages/Health/Assessments.razor · tests/…/SharedVisualFoundationTests.cs
- **Dependencias:** WI-P1, WI-T5, WI-C3, WI-C7, WI-C8, WI-C9. **Owner:** pau.
- **Outcome:** series temporales de salud legibles y consistentes con charts.

### WI-P9 · Trainees (flujo doble persona)
- **Objetivo:** migrar `hs-routine-*` (16; RoutineEditor.razor 513L + TraineeRoutines.razor) y verificar las páginas de trainee que reutilizan zonas ya migradas: Trainees.razor, TraineeExercises.razor (304L, 26 selectores `hs-workout-*` compartidos con P3), TraineeFavExerciseTypes, TraineeRoutineEditor.
- **Qué migra:** cards y editor de rutinas a WI-C7/WI-C6/WI-C8; TraineeExercises **no crea selectores fork** — hereda la zona `hs-workout-*` migrada en P3 y solo se corrigen clases propias; todas las páginas usan PageShell de P1.
- **Acceptance:** grep hex=0 en `hs-routine-*`; cero selectores nuevos `hs-workout-*` duplicados con prefijo trainee (grep lo verifica); `aria-live="polite"` de TraineeExercises.razor:41 intacto; editor de rutinas funcional sin cambio de flujo (tests de rutina en verde); `dotnet test` en verde.
- **Ficheros (7):** wwwroot/css/app.css · Pages/Trainees/Trainees.razor · Pages/Trainees/TraineeExercises.razor · Pages/Trainees/TraineeFavExerciseTypes.razor · Pages/Trainees/TraineeRoutines.razor · Pages/Trainees/TraineeRoutineEditor.razor · Components/Routines/RoutineEditor.razor
- **Dependencias:** WI-P1, WI-P3 (zona compartida — ejecutar después), WI-T5, WI-C6, WI-C7, WI-C8. **Owner:** pau.
- **Outcome:** el modo entrenador (patrón Hevy Trainer del benchmark) se ve igual que el modo propio, sin CSS paralelo.

### WI-P10 · Ajustes, mis rutinas e identidad
- **Objetivo:** migrar Settings.razor (183L), MyRoutines.razor (`hs-routine-view`) y la zona de identidad `hs-login-layout`/`hs-login-card`/`hs-login-title`/`hs-login-formerror*` (app.css:2108-2200; Login.razor 344L, Register, Logout, LoginLayout.razor).
- **Qué migra:** Settings a PageShell + WI-C7; login card a WI-C7 con inputs WI-C6 (el bloque de inputs :2101-2300 lo posee C6 — no re-editar, serializar después); retirar los 3 `style="…"` inline de Login.razor a clases tokenizadas; MyRoutines hereda `hs-routine-*` de P9.
- **Acceptance:** grep hex=0 y grep `style="` = 0 en Login.razor; card de login sin sombra con border token; errores de formulario ≥4.5:1 (WI-C8); flujo de login/register sin cambios (tests de identidad en verde); `dotnet test` en verde.
- **Ficheros (7):** wwwroot/css/app.css · Pages/Settings.razor · Pages/Routines/MyRoutines.razor · Components/Identity/Login.razor · Components/Identity/Register.razor · Components/Identity/Logout.razor · Shared/LoginLayout.razor
- **Dependencias:** WI-P1, WI-P9 (zona routine), WI-T5, WI-C6 (bloque inputs), WI-C7, WI-C8. **Owner:** pau.
- **Outcome:** primera impresión (login) y ajustes bajo el mismo contrato que el resto de la app.

---

## Orden de ejecución

```
WI-T1..T8 + WI-C1..C10 (cerrados)
→ WI-P1 (plantilla compartida — prerequisito de todas)
→ WI-P2 · WI-P4 · WI-P5 · WI-P6 · WI-P7 · WI-P8   (zonas disjuntas; app.css serializado)
→ WI-P3 → WI-P9                                    (zona hs-workout-* compartida)
→ WI-P10                                           (necesita routine zone de P9 y inputs de C6)
```

Cada item es una sola zona de selectores + sus páginas; ningún item contiene dos cambios independientes. Máximo 2 items de página en paralelo y solo si sus familias de selectores son disjuntas.

## Gates de QA por item (para t_b155320d)

- Contraste calculado dark **y** light en cada par texto/fill de la zona.
- Checklist visual 360 / 480 / 768 / 1024px; ningún item introduce breakpoints fuera de la ladder 480/768/1000.
- Grep por zona: hex literal = 0, `color: var(--hs-accent)` = 0, `box-shadow` = 0 en cards, `style="` inline = 0 en razor de la zona.
- `dotnet test` en verde en cada item (contrato de tokens intacto a mitad del rollout).
- Cierre global: grep hex en `Pages/` + `Components/` = 0 y 29 `@media` reducidos a la ladder (verifica M3 cerrado).

## Hotspot declarado

`wwwroot/css/app.css` (6109L) es tocado también por los 10 items de página. Regla anti-colisión: cada item es dueño exclusivo de su familia de selectores; los edits de app.css se serializan en el orden de arriba; está prohibido reformatear o renombrar selectores de otra zona. Ficheros razor de alta colisión: Workouts.razor (P3→P9), TodayWorkout.razor (P2 único dueño tras C1/C3/C4), Login.razor (C6 inputs → P10 layout).

## Fuera de alcance de este mapa

- Componentes y sus consumidores directos → WI-C1..C10.
- Valores finales de tokens y ladder de breakpoints → WI-T1..T8 / propuesta t_38c41b61 (QA t_68a74222 y cierre t_ba764d7a aún abiertos; los hexos citados son los de tokens-proposal.md pendientes de aprobación final).
- Selectores deprecados sin consumidor (`hs-widget-card`, `hs-table-stack`, `hs-today-rest`) → WI-C10.
- Gates de rollout y QA → t_b155320d. Cambios de flujo/IA/onboarding → propuesta t_38c41b61.

**Ningún item contiene código ni migra dos zonas; son especificaciones para despachar tras la aprobación de la propuesta.**
