# UX Results — KPIs mensuales de salud del trainee (t_33c412cf)

**Status:** DESIGN FINAL · implementable por Pau sin decisiones abiertas.
**Contract parent:** `docs/results-health-kpis-t_358e09e9.md` (decision-lock de @pau — NO se re-decide nada aquí; este doc solo especifica la capa FE).
**Prototipo:** `docs/design/prototypes/results-kpis-t_33c412cf.html` (3 frames: ok / estados / 4º botón).
**Screenshots QA:** `results-kpis-t_33c412cf-390.png`, `results-kpis-t_33c412cf-1024.png` (audit medido por DOM a 390×844 y 1024×900).

## 1. Objetivo UX

Que el entrenador vea en **una pantalla y un escaneo** el impacto de su coaching del mes (sesiones, peso, fuerza), y que el trainee, al abrir su perfil, perciba progreso medible → argumento de producto WHOOP-like y retención. Cada decisión de abajo está justificada por ese resultado.

## 2. IA y navegación (decision-lock, sin cambios)

- Ruta nueva **`/trainees/{id}/results`** (`TraineeRoute.Results(id)`), accesible solo con `[TraineeAccess]` (FE asume 200; ver §6).
- 4º botón **"Results"** en la tarjeta del trainee en `/trainees`, tras Exercises / Favourites / Rutinas (frame C del prototipo). Botón `height:44px`, radio 10, mismo estilo que los 3 existentes → cero fricción de aprendizaje.
- En la página: `h1 "Results"` + subtítulo `"{Mes año} · {Nombre}"`. Un selector de mes = chip ← → o `<input type="month">` nativo (MesActual por defecto; meses futuros no seleccionables). Cambio de mes = una sola llamada `/month` + refresh de summary; la serie `/series?months=6` se pide una vez por trainee.

## 3. Anatomía de la página (mobile-first, summary-first)

Orden vertical (mobile) / fila de 3 (≥1000px):

1. **Línea resumen** (`hs-results-summary`, card de 14px, una línea): `"En {mes}: {N} sesiones · {score}% de constancia · {±delta} kg · {±index}% fuerza"` — plantilla fija FE, construida SOLO con campos no-null del JSON; cualquier KPI en estado no-`ok` se omite de la línea (nunca "0"). *Outcome: el coach entiende el mes en 3 s sin abrir tarjetas.*
2. **Card 1 Consistencia** — hero `%`, chip delta `±N pp` vs mes anterior de la serie, chip sesiones `"N sessions"`, mini-bars 6 meses (CSS puro, aria-hidden; datos en `<ul class="sr-only">`), eje Abr…Sep.
3. **Card 2 Peso** — hero `last_kg`, chip delta firmado (`±x,y kg`, palabra "since last registration"), sparkline SVG inline aria-hidden con **gap real** (línea discontinua punteada donde no hay lectura — nunca interpolado). sr-only lista lecturas + "Sin lectura en {mes}".
4. **Card 3 Fuerza** — hero `index_pct` (`+4,2 %`), línea why `"media mensual vs primer registro · mes anterior"` (sin chip duplicado del mismo número), 5 chips por tipo `basic` con **su unidad propia** (kg / reps / % — nunca mezcladas) y delta firmado; plateau visible como `± 0 kg`.

Sin LLM, sin interpretación médica, sin colores de juicio: deltas en chip neutro con **signo + flecha decorativa aria-hidden** (WCAG 1.4.1: el color nunca es el único canal; el signo está en el texto).

## 4. Estados (copy exacto del decision-lock; NUNCA null→0)

| Estado KPI | Qué se renderiza |
|---|---|
| `consistency.not_started` | `"Todavía no completó sessions"` (card entera, sin hero ni bars) |
| `weight.current_only` | hero `last_kg` + `"Still no previous registration"` — valor visible, sin delta |
| `weight.insufficient_data` / `strength.insufficient_data` | `"Insufficient data"` + why `"Needs at least two sessions with registered load."` (fuerza) / `"Needs at least two weight registrations."` (peso) |
| delta = 0 legítimo | `± 0,0 kg` / `± 0 kg` chip `.chip-flat` (gris, no verde/rojo) — plateau ≠ insufficient |
| series con hueco | barra/sparkline omiten el mes sin lectura; sr-only dice `"Sin lectura en {mes}"` |
| carga | skeleton 3 tarjetas con la **misma geometría** (209/172/266px medidos a 390px) hasta que lleguen BOTH llamadas; CLS ≤ 0.1 por altura reservada |
| error de red | card-level retry inline (`Reintentar`), no toast vacío |
| acceso (403/404) | reutiliza el estado vacío de app-shell; no se crea copy nuevo |

## 5. Tokens y reglas visuales (design system v1, sin librerías nuevas)

- Tokens existentes de `app.css` (canvas `#0E1116`, card `#161B22`, accent `#1E6FFF`, radius 14/10/8, Inter, `tabular-nums` en todos los números).
- Hero 34/800, label 11/700 uppercase t2, why 12.5/500 t2, chip 12.5/700.
- Wash de chips ≤18% de accent (patrón existente). Bars/SVG inline: 0 requests, 0 imágenes → **presupuesto WASM ≤2 MB intacto** (0 KB nuevos).
- `@media (prefers-reduced-motion: reduce)` → `transition:none` en bars.

## 6. Mapeo API → UI (cero matemática en cliente)

`GET api/results/{traineeId}/month?month=YYYY-MM` alimenta hero+chips+copy de estado; `GET api/results/{traineeId}/series?months=6` alimenta delta pp, bars y sparkline (gap = mes ausente en `weight_series`). FE renderiza campos tal cual: `score_pct`, `delta_pp`, `completed_sessions`, `last_kg`, `delta_kg`, `index_pct`, `types[].delta[_pct]|unit`. El formateo es presentación (coma decimal es-ES, `%` tras número con NBSP); el redondeo lo fija la API.

## 7. Auditoría de accesibilidad (medida en navegador real, no declarativa)

- **390×844:** `scrollWidth == 390` (0 overflow), todos los targets ≥44px, chips de tipo wrap a 2 columnas sin truncar.
- **Contraste medido (todos ≥ AA 4.5):** label/state/why 8.01 · hero 15.71 · summary 15.71 · delta chip 12.02 · chip flat 7.05 · session chip 8.31 · type chips 13.84 · eje months 4.39 (decorativo, texto real en sr-only).
- **Decorativos ocultos:** 9 flechas `aria-hidden` + 6/6 SVG `aria-hidden`; datos gráficos en listas `sr-only` (`bars-data-a`, `line-data-a`).
- **1.4.1 color-not-alone:** todo delta lleva signo tipográfico (`+`, `−`, `±`); flat usa gris, no rojo/verde.

## 8. Out of scope (para no desbordar la card)

Opt-out `AnalyticsPermission` (futuro, como dice el spec), export/descarga, comparación entre trainees, any chart lib, any LLM line. Nada de esto bloquea el MVP.

## 9. Criterios de aceptación (para QA t_268ca483)

1. `/trainees` muestra 4º botón "Results" que navega a `/trainees/{id}/results`.
2. Con seed `qa@dev.local` mes completo: summary + 3 cards con los valores del JSON sin recálculo.
3. E1–E9 reproducen el copy exacto de §4 (null≠0, plateau `±0`, gap visible).
4. Auditoría §7 replicable: 390px sin overflow, contraste ≥4.5, 6/6 SVG aria-hidden, reduced-motion ok.
