# Mapa de work items — Componentes del design system (t_ee4c71df, 2026-10-23)

**Objetivo:** dividir la implementación de **componentes** del nuevo design system en work items despachables, cada uno ≤5-8 ficheros, sin migración de páginas y sin código — especificaciones para que pau despache tras los items de tokens.

**Inputs:** propuesta de design system t_38c41b61 (aún en redacción; hijos t_0098e568/t_3c855b9f/t_68a74222/t_ba764d7a abiertos) + mapa de tokens/CSS base t_57a0e89b (`docs/design/workitems-tokens-base-css-t57a0e89b.md`, items WI-T1..WI-T8) + auditoría t_511ecd88 (`docs/design/audit-design-system-v1-t511ecd88.md`, hallazgos H1-H4 / M1-M6 / L1-L3).

**Dependencia global:** ningún item de componentes se despacha antes de **WI-T1** (nombres de token en la spec) y, para todo lo que toque color, antes de **WI-T3** (paleta dark AA). Los hexos definitivos (#1B66E8, #868FA0, ink sobre fills) salen de la propuesta aprobada t_38c41b61; aquí se citan como candidatos de la auditoría.

**Verificación hecha en código (2026-10-23):** un solo fichero CSS (`wwwroot/css/app.css`, 6109L) concentra todas las clases `.hs-*` (799 selectores). Consumidores por familia verificados con grep sobre `Pages/`, `Components/`, `Shared/` (excluyendo `.worktrees/` y `obj/`): CTA → TodayWorkout.razor:59 + TodaySessionSummary.razor:53; chip → TodayWorkout, ExerciseCreationForm:180, TodayExerciseLog:43/48/101/112, TodaySessionSummary:32/122-123; set table → solo TodayExerciseLog.razor:57-112; FAB y bottom nav y account sheet → solo MainLayout.razor:18-106; nav-menu → NavMenu.razor; MudTable → solo CreateWorkout.razor:29-45; 16 ficheros razor usan MudTextField/MudNumericField; hexos hardcodeados confirmados en 6 ficheros (13 literales).

---

## Regla de migración común a todos los items

| Viejo | Nuevo | Motivo (outcome) |
|---|---|---|
| literal hex en razor/SVG (`#1E6FFF`, `#2FBF71`, `#F5B83D`, `#F1545B`) | `var(--hs-*)` en CSS o constante de `HealthspanTheme.cs` en C# | cambiar un token en la fuente única propaga a todos los componentes (elimina M1) |
| `--hs-accent` como **color de texto** | `--hs-accent-ink` | texto AA ≥4.5:1 en tabs, nombres de ejercicio, sync status (elimina H2) |
| `--hs-accent` / `--hs-done` como **fill** con icono/label `#fff` | fill semántico + ink oscuro encima | icono y label legibles ≥3:1 / ≥4.5:1 (elimina H1, H3) |
| `--hs-color-*` (alias legacy, app.css:23-36) | `--hs-*` v1 directo | una sola fuente de color (elimina M4; el retiro del alias lo hace WI-T7) |
| `opacity: .55` como señal de disabled | token `--hs-disabled-*` (fill + ink) | disabled legible y announced, no "apagado" (elimina L3) |
| `.hs-chip` min-height 44px aplicado también a badges estáticos | `.hs-chip` (interactivo, 44px) ≠ `.hs-badge` (estático, compacto) | densidad recuperada en listas sin perder targets táctiles (elimina L2) |

**Ningún item migra páginas enteras**: solo el selector de componente y sus consumidores directos. Las páginas se migran en su propio mapa.

---

## Work items (componentes)

### WI-C1 · CTA primario/secundario: accent-fill vs accent-ink + token disabled
- **Objetivo:** aplicar la regla accent-fill/accent-ink al botón más usado de la app y sustituir `opacity:.55` por token disabled. Zona: `.hs-cta` app.css:5620-5660 (primary :5642, secondary :5653, disabled :5637).
- **Qué migra:** primary = fill accent corregido + label `#fff` (5.11:1); secondary = wash accent-soft + texto accent-ink (8.33:1); disabled = fill/ink tokenizados en vez de opacity.
- **Acceptance:** contraste medido ≥4.5:1 en label primario y secundario en dark y light; `:disabled` sin `opacity` y con color propio ≥3:1; focus-visible intacto (:5647/:5658); los dos consumidores renderizan sin cambio de layout; test de contrato en verde.
- **Ficheros (4-5):** wwwroot/css/app.css · Pages/Exercises/TodayWorkout.razor · Components/TodaySessionSummary.razor · Shared/HealthspanTheme.cs (Button) · tests/…/SharedVisualFoundationTests.cs
- **Dependencias:** WI-T1, WI-T3. **Owner:** pau.
- **Outcome:** el CTA de "Empezar entreno" (Hoy) deja de fallar AA — es el botón más tocado de la app (H1).

### WI-C2 · FAB y FAB-done: ink oscuro sobre fill semántico
- **Objetivo:** fix H3 — el icono `#fff` sobre `--hs-done` (2.38:1) pasa a ink oscuro sobre el fill done; el glow se redefine como acento, no elevación (resuelve L1 junto con WI-T1). Zona: `.hs-fab` app.css:448-468, `.hs-fab-done` :464-468 y :5977-5981.
- **Acceptance:** contraste del icono y del label "Finalizar" sobre done ≥4.5:1 (candidato ink `#0E1116` → 7.94:1); FAB sigue siendo acción directa sin ruta duplicada (MainLayout.razor:189 intacto); target ≥44px y safe-area insets intactos; sin sombra nueva añadida.
- **Ficheros (2-3):** wwwroot/css/app.css · Shared/MainLayout.razor:87-100 · docs/design/tokens.json (si el ink sobre fills se declara como token)
- **Dependencias:** WI-T1, WI-T3. **Owner:** pau.
- **Outcome:** "Finalizar entreno" pasa de ilegible a legible con guante/sudor (H3, WCAG 1.4.11).

### WI-C3 · Chip interactivo vs badge estático + variantes semánticas
- **Objetivo:** separar `.hs-chip` (control táctil, min-height 44px) de `.hs-badge` (marca estática: PR, estado clínico) y declarar las variantes semánticas (accent/done/pr/more) sobre washes. Zona: `.hs-chip` app.css:3958-4008, `.hs-today-rest` :5771-5790, `.mud-chip-close-button` :145-146.
- **Acceptance:** ningún badge estático ocupa 44px de alto (densidad recuperada en resumen de sesión y fichas de salud); todo chip con acción mantiene ≥44px; los 4 variantes de wash pasan ≥4.5:1 en su texto; el chip de contador de series y el chip de quitar serie siguen siendo táctiles; `ClinicalStatusChip` (5 páginas de salud) renderiza como badge sin target inflado.
- **Ficheros (6):** wwwroot/css/app.css · Components/TodayExerciseLog.razor · Components/TodaySessionSummary.razor · Components/ExerciseCreationForm.razor · Components/Health/ClinicalStatusChip.razor · Pages/Exercises/TodayWorkout.razor
- **Dependencias:** WI-T1, WI-T3. **Owner:** pau.
- **Outcome:** listas de logging más densas sin perder targets táctiles (L2); el color solo aparece cuando carries meaning.

### WI-C4 · Set table (log de series): tokens + columna tertiary legible
- **Objetivo:** migrar la tabla de registro de series a tokens y arreglar la columna "Anterior" (12.5px, tertiary 4.39:1 → #868FA0). Zona: `.hs-settable` app.css:493-594, `.hs-settable-input` :535-552, fila done :587-594.
- **Acceptance:** texto tertiary ≥4.5:1 sobre card; inputs de kg/repes con radius y border token y focus-visible propio; row done legible (wash + ink, no opacity); steppers de intent siguen siendo 2×20px dentro del target 44px (app.css:150); tabular-nums alineado en columnas numéricas; `aria-label` de la tabla intacto (TodayExerciseLog.razor:57).
- **Ficheros (2-3):** wwwroot/css/app.css · Components/TodayExerciseLog.razor · tests/…/SharedVisualFoundationTests.cs
- **Dependencias:** WI-T1, WI-T3, WI-T6 (paso tertiary de la escala tipográfica). **Owner:** pau.
- **Outcome:** la pantalla de logging en medio del entreno se lee con luz de gimnasio y mano sudorosa (M2).

### WI-C5 · Navegación: tabs activos accent→accent-ink, sidebar, topbar, account sheet
- **Objetivo:** aplicar accent-ink a todo estado activo de navegación y tokenizar topbar/sidebar/sheet de cuenta. Zona: `.hs-bottom-nav` app.css:412-446 (tab activo :444-446 usa `var(--hs-accent)` → 4.23:1), `.hs-topbar` :168-234, `.hs-account-sheet` :209-233, `.hs-sidebar` :241-292, `.hs-nav-menu` :280-292 (nav-link activo :290).
- **Acceptance:** tab activo y nav-link activo ≥4.5:1 con accent-ink; el fill de selección sigue visible ≥3:1 (1.4.11); backdrop-filter de la topbar y su capa hija (:188) intactos; sheet de cuenta sigue por encima de la bottom nav (z-index :204) y conserva los 44px de sus items; `aria-label` del account button intacto (MainLayout.razor:27-29); breakpoint de cambio nav→sidebar 1000px sin tocar (es WI-T5).
- **Ficheros (3):** wwwroot/css/app.css · Shared/MainLayout.razor · Shared/NavMenu.razor
- **Dependencias:** WI-T1, WI-T3. **Owner:** pau.
- **Outcome:** el estado "dónde estoy" de la app se ve sin esfuerzo (H2 en tabs).

### WI-C6 · Inputs y formularios: contrato MudBlazor → tokens
- **Objetivo:** unificar inputs, steppers y superficies de formulario bajo tokens (radius 8, border, dense sizing) y eliminar la dependencia de `--mud-palette-*` en la capa de componentes. Zona: overrides Mud app.css:133-158, radius inputs :2090-2100, `.hs-form-surface` :373, `.hs-form-primary-action` :377, bloque login :2101-2300.
- **Acceptance:** cero `var(--mud-palette-*)` en selectores de componente fuera de la capa de theme (coherente con WI-T7); todos los inputs con radius/border/focus token y ≥44px de alto; label de error de campo legible ≥4.5:1; el formulario de creación de ejercicio y el de login no cambian de estructura ni de flujo; los 16 consumidores de MudTextField/MudNumericField no requieren edición (el fix vive en CSS/theme, no en las páginas).
- **Ficheros (5-6):** wwwroot/css/app.css · Shared/HealthspanTheme.cs (Typography/Inputs) · Components/ExerciseCreationForm.razor · Components/ExerciseForm.razor · Components/ExerciseTypeForm.razor · Components/Identity/Login.razor
- **Dependencias:** WI-T1, WI-T2, WI-T6. **Owner:** pau.
- **Outcome:** registrar peso/repes sin fricción visual ni táctil; un solo lugar donde cambiar el estilo de input.

### WI-C7 · Cards y hero metrics: contrato de superficie sin sombras
- **Objetivo:** fijar el contrato de card (surface ladder + 1px border + radius token, shadow `none`) y aplicarlo a hero stats, cards de ejercicio, widget cards y paneles de perfil. Zona: `.hs-surface` app.css:366, `.hs-hero` :470-490, `.hs-hero-stat` :2394-2412, cards de ejercicio :1896-1990, `.hs-widget-card` :2503-2556, `.hs-workout-history-*` :741-838.
- **Acceptance:** ningún componente de card declara `box-shadow` (L1 cerrado con la definición glow≠elevación de WI-T1); ladder de superficies idéntico dark y light; hero value/label con tokens de la escala tipográfica; hover de widget card sin elevación (cambio de border/surface); cards con foto lazy (:1928) intactas.
- **Ficheros (5-6):** wwwroot/css/app.css · Components/Profile/WorkoutSessionCard.razor · Components/Training/ReadinessCard.razor · Components/Profile/AdaptationSummaryPanel.razor · Components/Profile/WeeklyTrainingSummaryPanel.razor · Pages/Workouts.razor (solo clases de historia, no migración de página)
- **Dependencias:** WI-T1, WI-T3, WI-T4 (el contrato se valida también en light). **Owner:** pau.
- **Outcome:** jerarquía visual consistente entre hoy, historia e insights — la profundidad se lee por superficie, no por sombra.

### WI-C8 · Diálogos, sheets y estados (empty / error / loading)
- **Objetivo:** tokenizar los contenedores transitorios y los estados vacíos, con foco y cierre accesibles. Zona: `.hs-account-sheet` app.css:209-233, diálogos Mud (FinishSessionDialog, ExerciseFilterDialog), `.hs-empty-state`/`.hs-error-state`/`.hs-loading-state` :703-738.
- **Acceptance:** diálogo y sheet con surface/border/radius token y título ≥4.5:1; botón de cierre ≥44px con nombre accesible; estado de error con ink danger ≥4.5:1 y no solo color (icono + texto); `aria-live="polite"` de los resúmenes intacto (Workouts.razor:140, TraineeExercises.razor:41); `prefers-reduced-motion` respetado en la animación del sheet (app.css:105).
- **Ficheros (4-5):** wwwroot/css/app.css · Components/FinishSessionDialog.razor · Components/ExerciseFilterDialog.razor · Components/EmptyState.razor · Components/ErrorState.razor
- **Dependencias:** WI-T1, WI-T3. **Owner:** pau.
- **Outcome:** finalizar sesión y los estados de error se entienden de un vistazo sin depender del color.

### WI-C9 · Charts y data-viz: eliminar los 12 hexos hardcodeados (M1)
- **Objetivo:** que todo SVG/gráfico lea la fuente única: `var(--hs-*)` en estilos y constantes de `HealthspanTheme.cs` en C# (los rampas RGB del body map). Zona: MuscleLoadBodyMap.razor:29 y :165-167 (`LoadLow/LoadMid/LoadHigh`), TrendChart.razor:50, SparklineChart.razor:32, Biomarkers.razor:535-536, Assessments.razor:414, ExerciseTypeDetails.razor:526/749.
- **Acceptance:** cero literales hex en ficheros razor de componentes (grep limpio en `Components/` y `Pages/`); el test de regresión extiende el contrato a los hexos razor y falla si uno diverge de tokens.json; la rampa readiness (low/mid/high) conserva los 3 pasos semánticos con los valores corregidos; los gráficos siguen siendo `aria-hidden`/con texto alternativo donde ya lo tenían.
- **Ficheros (7):** Components/Profile/MuscleLoadBodyMap.razor · Components/Health/TrendChart.razor · Components/Health/SparklineChart.razor · Pages/Health/Biomarkers.razor · Pages/Health/Assessments.razor · Pages/ExerciseTypes/ExerciseTypeDetails.razor · Shared/HealthspanTheme.cs
- **Dependencias:** WI-T1, WI-T2 (constantes C# generadas), WI-T3. **Owner:** pau.
- **Outcome:** cambiar el accent en la fuente única propaga a gráficos e insights (M1 / "Unified" de Airbnb DLS).

### WI-C10 · Sync indicator, toolbars y controles sueltos de shell
- **Objetivo:** cerrar los consumidores restantes de accent-como-texto y alias legacy fuera de las familias anteriores. Zona: `.hs-sync-syncing` app.css:5422 (`color: var(--hs-accent)`), `.hs-page-toolbar` :381-391, `.hs-toolbar` :381, border accent :799/:804/:6033, `.hs-table-stack` :2863 y `.hs-widget-card` :2503-2556 (selectores sin consumidor en el árbol actual → confirmar o retirar).
- **Acceptance:** cero `color: var(--hs-accent)` en app.css fuera de la capa de tokens (los usos restantes migran a accent-ink o a fill); los selectores sin consumidor razor quedan documentados como deprecados o eliminados (con grep que lo verifique); toolbars con targets 44px intactos (app.css:121-128).
- **Ficheros (2-3):** wwwroot/css/app.css · Shared/SyncStatusIndicator.razor · docs/design/DESIGN.md (registro de selectores deprecados)
- **Dependencias:** WI-T1, WI-T3, WI-C5 (mismo patrón accent→accent-ink, evitar tocar la misma zona dos veces). **Owner:** pau.
- **Outcome:** la regla accent-fill/accent-ink queda aplicada al 100% del CSS, no solo a las familias grandes.

---

## Orden de ejecución

```
WI-T1 + WI-T3 (tokens dark AA)
  ├─ WI-C1 (CTA) ── WI-C2 (FAB)          [mismos tokens, zonas distintas]
  ├─ WI-C3 (chip/badge) ── WI-C4 (set table, +WI-T6)
  ├─ WI-C5 (nav) ── WI-C10 (shell suelto, después de C5)
  └─ WI-C8 (diálogos/estados)
WI-T2 + WI-T3 → WI-C9 (charts)
WI-T2 + WI-T6 → WI-C6 (inputs/forms)
WI-T4 (light real) → WI-C7 (cards, se valida en ambos temas)
```

Cada item es independiente de los demás salvo las dependencias de tokens indicadas; C5→C10 comparten la zona de navegación y se ejecutan en ese orden para no pisarse.

## Gates de QA por item (para t_b155320d)

- Contraste calculado (herramienta de la auditoría) en dark **y** light para cada par texto/fill tocado.
- Checklist visual a 360 / 480 / 768 / 1024px (no introduce breakpoints nuevos).
- Grep de literales hex en `Components/` y `Pages/` = 0 al cerrar WI-C9.
- `dotnet test` en verde en cada item (el contrato de tokens no puede romperse a mitad del rollout).

## Hotspot declarado

`wwwroot/css/app.css` es un único fichero de 6109 líneas tocado por los 8 items de tokens **y** por los 10 de componentes. Es el punto de colisión más probable del rollout: los items que tocan la misma familia de selectores deben serializarse (C5→C10, T3→T4→T7) y ningún item debe reformatear zonas que no le pertenecen.

## Fuera de alcance de este mapa

- Migración de páginas a las clases nuevas (mapa aparte).
- Cambios de IA, navegación o flujos de onboarding (propuesta t_38c41b61).
- Definición de valores finales de tokens (WI-T1..WI-T4).
- Gates de rollout y orden de QA → t_b155320d.

**Ningún item contiene código ni migra páginas; son especificaciones para despachar.**
