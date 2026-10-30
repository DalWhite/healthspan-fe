# Propuesta final — Nuevo design system Healthspan (DESIGN.md v2)

Fecha: 2026-10-23 · Autor: UX (ux-designer) · Tarjeta: t_ba764d7a (hijo de t_38c41b61)
Estado: **propuesta — no aplicada en código** (verificable: `git status` solo añade `docs/design/`; ningún componente ni CSS modificado).

Este documento **integra** los cuatro entregables del rediseño en una única propuesta coherente y cierra los hallazgos P0/P1 de QA:

| Insumo | Documento | Rol en esta propuesta |
|---|---|---|
| Auditoría v1 (t_511ecd88) | `docs/design/audit-design-system-v1-t511ecd88.md` | evidencia de qué falla hoy (H1–H4, M1–M6, L1–L3) |
| Benchmark 6 apps (t_75c7b777) | `docs/design/research-ux-apps-2026.md` | patrones de producto y navegación (ledger [1]–[23]) |
| Literatura WCAG/M3/HIG (t_8a8f583d) | comentario + `investigacion3.md` (ledger [25]–[54]) | contrato de aceptación (7 requisitos no negociables) |
| Matriz de evidencia (t_45b423e0) | `docs/design/evidence-matrix.md` | filas P/T/E/C/X/M — cada decisión cita una fila |
| Tokens v2 (t_0098e568) | `docs/design/tokens-proposal.md` | base de tokens, con fixes QA aplicados aquí |
| Componentes (t_3c855b9f) | `docs/design/components-proposal.md` | catálogo base, con fixes QA aplicados aquí |
| Checklist accesibilidad (t_68a74222) | `docs/design/accessibility-checklist.md` | 4 P0 + 7 P1 + 6 P2 — **todos resueltos en §2–§6** |

Convención de cita (idéntica a la matriz de evidencia): **A** = auditoría, **B** = benchmark [n], **L** = literatura [25]–[54], **Q** = QA de citas [F1]–[F11], **M** = matriz de evidencia (filas P/T/E/C/X/M), **V** = verificación numérica propia de este documento (script `contrast_final_ba764d7a.py`, 101 comprobaciones WCAG, fórmula luminancia relativa + composición alfa premultiplicada; salida reproducible). Los contrastes citados abajo están recalculados con los **valores finales** de §2, no heredados.

---

## 1. Principios y contrato de aceptación

El nuevo design system se acepta solo si cumple los 7 requisitos no negociables de la literatura (M-decisión cruzada 2; L [25]–[54]):

1. Targets táctiles ≥44 dp/pt (suelo WCAG 2.5.8 [25] nunca como diseño; HIG 44 pt [30], Android 48 dp [28]).
2. Contraste ≥4.5:1 texto y ≥3:1 gráficos/componentes **en ambos temas** (WCAG 1.4.3 [27], 1.4.11 [39], Material [45]).
3. Tipografía tokenizada + layout que aguanta 200 % de font scaling (WCAG 1.4.4 [40], M3 [41], HIG [47], Android 14 [46]).
4. Modo oscuro como parte del sistema de color (roles + ladder + overlays 0–16 %) [43][F7].
5. Flujo de registro de un paso, mínimo esfuerzo (iMHere: −35 % tiempo, −82.8 % errores [49][50]).
6. Notificaciones/recordatorios como palanca de adherencia [54].
7. Retención realista: benchmark 7 % a 30 días; co-diseño 45.8 % [51].

Principios de diseño que ordenan las decisiones (de la matriz de evidencia):

- **El log es el producto** (M1): la activación "registrar el entreno de hoy en un gesto" es el núcleo intocable.
- **La spec, no el código, era el documento defectuoso** (A-H2/H4; decisión cruzada 1): el código ya había corregido contrastes que la spec v1 prescribía mal. La v2 es machine-readable, con lint de contraste.
- **Un concepto = un color, siempre** (P7, WHOOP [6]); nunca color como única señal (X4, Apple [23]).
- **Dark-first como decisión de producto declarada** (P10 [Q-#13]), no como hallazgo de benchmark; la paleta light real es deuda pagada en esta propuesta (P5).
- **La paleta es propia de Healthspan** (P6 [Q-§3]): se elimina toda atribución a Hevy.

---

## 2. Tokens de color — valores FINALES (fixes QA aplicados)

### 2.1 Ladder de superficies DARK (se mantiene la arquitectura v1)

| Token | Valor | Estado vs v1 | Justificación (cita) |
|---|---|---|---|
| `--hs-canvas` | #0E1116 | igual | fondo base del ladder sin sombras (P9 [F7][44]) |
| `--hs-card` | #161B22 | igual | profundidad por luminosidad + borde 1px (P9) |
| `--hs-card-border` | #262C36 | igual | borde como único separador (A-veredicto) |
| `--hs-input` | #1F2630 | igual | superficie 2, campos editables (A-inventario) |
| `--hs-raised` | #283040 | igual | pressed/disabled (A-L3) |
| `--hs-scrim` | rgba(0,0,0,.55) | igual | overlay modal (v1 §1) |

### 2.2 Accent y semánticos DARK

| Token | Valor | Estado vs v1 | Justificación (cita) |
|---|---|---|---|
| `--hs-accent` (fills ONLY) | **#1B66E8** | ajusta #1E6FFF | CTA primario pasaba 4.40 ✗ → 5.11 ✓; fill ≥3:1 sobre card/canvas (P1/A-H1; V) |
| `--hs-accent-ink` (texto) | **#98BAF5** | nuevo token documentado | todo accent-text: 8.80 card / 7.75 input / 7.96 wash (P2/A-H2-H4; V) |
| `--hs-accent-wash` | #14233F | igual (renombrado de "accent soft") | fondo CTA-sec/chips con accent-ink (P2) |
| `--hs-done` (fill saturado) | #2FBF71 | igual | rampa readiness con hexes propios declarados (P8 [F5]) |
| `--hs-on-done` | #0E1116 | nuevo | ink oscuro sobre fills saturados: 7.94 (P3/A-H3; V) |
| `--hs-wash-done` | rgba(47,191,113,.16) | tokenizado | texto done sobre wash 5.51 ✓ (V; A-veredicto) |
| `--hs-pr` / `--hs-on-pr` | #F5B83D / #0E1116 | igual + ink | ink sobre pr 10.63 ✓ (V) |
| `--hs-wash-pr` | rgba(245,184,61,.18) | tokenizado | texto pr sobre wash 6.61 ✓ (V) |
| `--hs-err` (fill/borde) | #F1545B | igual | 5.07 sobre card como gráfico ✓ (V) |
| `--hs-err-ink` (texto) | **#FA8088** | **fix QA P0-1** (reemplaza el candidato #F4636A) | #F1545B como texto sobre wash err = 4.33 ✗; #FA8088 pasa en las 4 superficies: 5.98 wash/card · 5.27 wash/input · 4.62 wash/raised · 7.01 card (QA §3; V) |
| `--hs-wash-err` | rgba(241,84,91,**.14**) | **alpha unificado 14 %** (fix QA P1-6; components decía 16 %) | compuesto sobre card = **#35232A** (fix QA P1-5: el hex v1 #2A1D24 era error aritmético, equivalía a ~9 %) (V) |
| `--hs-t1` / `--hs-t2` | #F2F4F8 / #A9B1BE | igual | 15.71 / 8.01 sobre card (V) |
| `--hs-t3` | **#868FA0** | ajusta #78818F | tertiary como texto real (columna "Anterior") fallaba 4.39/3.87 → 5.31/4.68 (P4/A-M2; V) |

### 2.3 Ladder de superficies LIGHT (reconstrucción desde 0 — deuda P5 pagada)

`PaletteLight` heredaba valores dark (`HealthspanTheme.cs:30-33`); la v2 define un **ladder espejo** (en light la profundidad baja de luminosidad), mismo accent, borde 1px (P5 [43]; V).

| Token | Valor | Rol |
|---|---|---|
| `--hs-canvas` | #F2F4F8 | fondo base |
| `--hs-card` | #FFFFFF | superficie 1 (máxima luminosidad = máxima elevación) |
| `--hs-card-border` | #D9DFE7 | borde 1px |
| `--hs-input` | #F5F7FA (borde #C6CFDB) | superficie 2 recessed |
| `--hs-raised` | #E7ECF3 | pressed/disabled (presionar = oscurecer) |
| `--hs-scrim` | rgba(20,24,31,.45) | overlay modal |

### 2.4 Accent y semánticos LIGHT

| Token | Valor | Justificación (cita) |
|---|---|---|
| `--hs-accent` (fills) | #1B66E8 (idéntico dark) | blanco 5.11 ✓; fill 5.11 card / 4.64 canvas / 4.76 input / 4.30 raised (V) |
| `--hs-accent-ink` | #1450B8 | 7.32 card / 6.38 wash (V) |
| `--hs-accent-wash` | #E8F0FC | fondo CTA-sec/chips (V) |
| `--hs-done-ink` | #157A46 | **fix QA P0-3**: en light los gráficos semánticos usan `*-ink` (done fill 2.38 ✗; ink 5.38 card / 4.88 canvas / 5.01 input / 4.53 raised ✓) (V) |
| `--hs-done-wash` | #E3F4EB | wash fila completada; done-ink sobre wash 4.71 ✓ (V) |
| `--hs-pr-ink` / `--hs-pr-wash` | #8A5A12 / #FBEEDB | pr fill 1.78 ✗ → ink 5.91/5.37/5.51/4.98 ✓ (V) |
| `--hs-err-ink` / `--hs-err-wash` | #C22B33 / #FCE9EA | err fill sobre raised 2.87 ✗ → ink 5.70/5.17/5.31/4.80 ✓ (V) |
| `--hs-t1` / `--hs-t2` | #14181F / #4B5563 | 17.79 / 7.56 sobre card (V) |
| `--hs-t3` | **#5D6878** | **fix QA P1-10**: #657080 sobre raised 4.23 ✗ → #5D6878 pasa en las 4 superficies: 5.65 card / 5.26 input / 5.13 canvas / 4.76 raised ✓ (V) |

### 2.5 Reglas de color obligatorias (v2)

1. **Dos papeles del accent**: `accent` solo fills/gráfico ≥3:1; todo texto accent usa `accent-ink` (P2). Excepción normativa nueva (fix QA P0-2): el accent puro **no** es válido como señal sobre `input`/`raised` dark (2.98/2.59 ✗); en esas superficies la señal usa `accent-ink` (V).
2. **Ink oscuro (#0E1116) solo sobre fills SATURADOS** (`done`/`pr`/`err` puros: 7.94/10.63/5.55 ✓). Sobre washes dark el ink es invisible (1.28–1.44 ✗): sobre washes el texto es `t1`/`t2`/`*-ink` (fix QA P1-7; V).
3. **En light, todo gráfico semántico usa `*-ink`** (barras, puntos, bordes, FAB, rampa del hero); el fill saturado solo existe con `ink #0E1116` encima (fix QA P0-3; V).
4. **Borde de estado** = color semántico puro en dark (done 7.26 / pr 9.73 / err 5.07 sobre card ✓) y `*-ink` en light (todos ≥4.5 como gráfico ✓); el accent puro solo sobre card/canvas (3.39/3.70 ✓) — sobre `input` dark usa accent-ink (2.98 ✗; V). El "color al 45 %" queda como tinte decorativo, **nunca** como señal de estado (fix QA P0-4; el 3.93 citado en components §1 era accent puro, no el borde: 1.21–2.97 ✗).
5. Un concepto = un color, siempre; nunca color como única señal (color+icon+text) (P7 [6], X4 [23]).

---

## 3. Tipografía

| Token | Valor | Estado vs v1 | Cita |
|---|---|---|---|
| `--hs-font-family` | Inter 400/500/600/700/800, fallback system-ui | mantiene | A-veredicto; `app.css:38`, `index.html:10-12` |
| `font-variant-numeric` | tabular-nums **global** (body) | mantiene; atribución corregida: feature OpenType de Inter, no "de Hevy" | T1 [F4]; `app.css:88-90` |
| `--hs-type-hero` | 4.75rem/800 lh 1 tracking −0.045em | mantiene como patrón; **nuevo requisito**: `clamp(3rem, 12vw, 4.75rem)` + validación sin recorte a 200 % de fuente (QA M: 152 px a 200 % en 360 px) | T3 [F5]; QA §10 |
| `--hs-type-metric` | 1.5rem/800 lh 1.1 | mantiene | v1 §2 |
| `--hs-type-screen-title` | 1.875rem/800 lh 1.2 tracking −0.02em | mantiene | v1 §2 |
| `--hs-type-section-title` | 1.0625rem/700 | mantiene | v1 §2 |
| `--hs-type-item-title` | 0.9375rem/700 (accent-ink si es affordance) | ajusta (regla §2.5-1) | A-H2 |
| `--hs-type-body` | 0.9375rem/500 lh 1.5 | mantiene | v1 §2 |
| `--hs-type-meta` | 0.78125rem/500 color t3 | ajusta vía token de color (§2.2/§2.4), no de tamaño | P4/A-M2 |
| `--hs-type-micro-label` | 0.6875rem/700 UPPERCASE tracking 0.08em | mantiene (solo columnas/chips/hero-labels) | `app.css:484-490` |

Requisito no negociable: la escala existe como tokens (tamaño/lh/tracking/peso por paso) y **toda plantilla se valida al 200 % de fuente** sin overflow ni pérdida (T2 [40][41][46][47]; X6). Consecuencias de implementación registradas (QA M): los 11 `font-size` en px literales (`app.css:3229,3636,3642,5481,5666,5703,5722,5782,5900,5948,5987`) pasan a `rem`; anchos de `.hs-settable` (hoy fixed 42/78/44 px, `app.css:497-516`) pasan a `em` con variante de fila apilada a ≥150 % de fuente.

## 4. Espaciado, targets y breakpoints

| Token | Valor | Estado vs v1 | Cita |
|---|---|---|---|
| `--hs-space-1…7` | 4/8/12/16/24/32/48 px | mantiene (ahora nombrado en spec) | `app.css:54-60` |
| `--hs-target` | **44 px** (mínimo de diseño) | nuevo token (hoy `--hs-touch-target` 44 px, `app.css:71`) | X1 [30][25] |
| `--hs-target-dense` | **48 px** (listas densas, pantallas en movimiento) | nuevo token (hoy solo literales `app.css:3351,3880`) | X1 [28]; E4 [3] |
| `--hs-gap-target` | **8 px** mínimo entre targets adyacentes | nuevo token; **fix**: gap real en la tabla de series es 4 px (`--hs-space-1`, `app.css:527`) → subir a 8 px (QA §9) | E2 [28][48] |
| `--hs-breakpoint-sm/md/lg` | **480 / 768 / 1000 px** | reconstruye los 29 `@media` / 15 breakpoints ad-hoc (QA §14: ninguno es 480 → es consolidación real; mapear los pares 599/600 y 959/960/999/1000 y probar 360/480/768/1000) | E1/A-M3 |
| Shell: bottom-nav 68 / topbar 52 / sidebar 264 / content-max 1280 px | mantiene | `app.css:70-74` |
| Chip interactivo ≥44 px; badge estático tamaño por contenido | split (ver §7.3) | E3/A-L2 |

## 5. Radios

`--hs-radius-input/chip` 8 · `--hs-radius-button` 12 (unifica el duplicado 10/12 del código) · `--hs-radius-card` 14 · `--hs-radius-sheet` 20. Regla: radio creciente con el tamaño de la superficie; sin radios ad-hoc (evidencia `app.css:46-52`; aprobado Wall v1).

## 6. Elevación, estados y foco

| Token | Valor v2 | Decisión | Cita |
|---|---|---|---|
| `--hs-shadow-card` / `--hs-shadow-panel` | **none** | profundidad por ladder + borde 1px | P9 [F7][44] |
| `--hs-shadow-sheet` | dark `0 -12px 32px rgba(0,0,0,.6)` · light `0 -12px 32px rgba(20,24,31,.18)` | **única sombra del sistema, ahora aplicada**: el código usa `--hs-shadow-panel` (`app.css:1102`) en lugar del token declarado (drift). **Fix QA P1-9**: el sheet dark es indistinguible de su scrim (1.06–1.16) → sheet = superficie card + **borde superior 1px** `--hs-card-border` + shadow-sheet aplicado | QA §6F; drift `app.css:1092-1102` |
| `--hs-focus-ring` | **outline 2px `accent-ink`** (dark #98BAF5: 9.62/8.80/7.75/6.73 sobre canvas/card/input/raised ✓; light #1450B8: 6.65/7.32/6.82/6.17 ✓) + halo decorativo `rgba(27,102,232,.35)` explícitamente **no normativo** (1.41–1.67) | **fix QA P0-2**: el accent nuevo como outline fallaba 1.4.11 sobre input/raised (2.98/2.59); el anillo box-shadow nunca fue indicador. Requisito: señal de foco ≥3.0 sobre **todas** las superficies de la ladder; confirmar que MudBlazor pinta señal ≥3.0 en `.mud-input-slot` (hoy `outline:none`) | QA §6A/§8; V |
| `--hs-disabled-fg` / `--hs-disabled-bg` | t3 sobre raised (dark 4.06 ✓ / light 4.76 ✓) | reemplaza `opacity .55` sin token; válido para componente, no para texto legible | C7/A-L3; V |
| `--hs-glow-fab` | **eliminado** (recomendación) | el glow (`app.css:462`) contradice "sin sombras" (L1/C7). Pendiente de visto bueno Wall (§12) | A-L1 |
| `--hs-transition-fast` | 160 ms ease; `prefers-reduced-motion` obligatorio y extendido al glow del FAB y al timer | mantiene + extiende | X5; `app.css:105` |
| Hover | todas las reglas hover bajo `@media (hover: hover) and (pointer: fine)` | **fix QA P2-12**: hoy 17 reglas `:hover`, 0 guards → hover pegado en táctil | QA §12 |
| `forced-colors` | bloque obligatorio para foco y semántica (outline/borde explícitos) | **fix QA P2-15**: hoy 0 bloques; en Windows High Contrast los estados desaparecen (1.4.11) | QA §8/§14 |

---

## 7. Catálogo de componentes base (7) con estados

Anatomía y comportamiento completos en `docs/design/components-proposal.md` (t_3c855b9f); aquí queda la decisión final por componente **con los fixes QA aplicados**. Todos los componentes consumen solo `var(--hs-*)` (token contract §10).

### 7.1 Card
Variantes container/interactive/selectable/row. Estados: default · hover (solo fine pointer) · pressed = `raised` · selected = wash accent 16 % + borde accent + texto accent-ink · **disabled por token** (raised + t3 + `aria-disabled`, reemplaza opacity .55) · loading skeleton con forma · error inline · empty con guía + un CTA secondary. Interactive card = un solo tab stop. Cero chips/botones en card de Biblioteca.

### 7.2 CTA
Taxonomía única: **primary** (fill accent + blanco 5.11 ✓) · **secondary** (accent-wash + accent-ink 7.96/6.38 ✓ — documenta lo que el código ya corregía; la spec v1 prescribía 3.56 ✗) · **danger** (**fix QA P0-1**: wash err 14 % + texto `--hs-err-ink` — dark #FA8088 5.98/5.27/4.62 ✓, light #C22B33 4.88 ✓ — nunca `#F1545B` como texto: 4.33 ✗) · **done** (fill done + ink #0E1116 7.94 ✓). Estados: hover aclara · pressed oscurece · disabled token · loading spinner en icon slot con label intacto + `aria-busy` · error inline bajo el botón. Una primary por pantalla; <480 full-width.

### 7.3 Chip (interactivo) + Badge (estático) — split fundacional
Split E3/A-L2: chip-interactivo target ≥44 px con estados; badge-estático (PR, racha, bloque) tamaño por contenido (~24 px) — hoy `.hs-chip` infla badges a 44 px (`app.css:3959`). **Fix QA P0-4**: borde = color semántico puro dark (accent puro solo sobre card/canvas; sobre input usa accent-ink) / `*-ink` light; el mix 45 % queda decorativo. Selected = wash + borde + ink con `aria-pressed` (el estado se lee por borde+peso+color, no solo tinte). Grupo de chips con loading (skeleton) y empty ("Sin resultados — limpiar filtros").

### 7.4 Bottom sheet
Única sombra del sistema **aplicada** + borde superior 1px (fix QA P1-9, §6). **Fix QA P1-11 (sheets propias, `Workouts.razor:327-421`)**: añadir Escape + focus trap + foco inicial + restauración del foco al trigger (hoy tienen `role=dialog`/`aria-modal`/Cancel pero ninguna de las cuatro; con `aria-modal=true` y sin trap la declaración es falsa). Campos: label arriba del input (nunca placeholder-as-label), validación on-blur. Sheet de cuenta móvil contiene todas las entradas de cuenta + logout. ≥768: dialog centrado 440 px.

### 7.5 FAB
46 px, radio 14, aria-label con verbo de acción. Variantes action (accent + blanco 5.11 ✓) y done (fill done + **ink #0E1116 7.94 ✓**, reemplaza el blanco actual 2.38 ✗, A-H3). Glow eliminado (pendiente Wall, §12). <1000: slot central de la bottom nav, nunca ruta duplicada (C6). ≥1000: **se convierte en CTA primario persistente al pie del sidebar** — decisión nueva, pendiente de visto bueno Wall (§12).

### 7.6 Timer / log de serie (el corazón del producto)
Set row como tabla real (`th scope="col"`): Serie · **Anterior** (prefill de la última sesión, t3 ajustado 4.68 ✓, M9) · Reps/kg (input, radio 8) · ✓ toggle 48 px · PR badge inline ("★ PR personal", nunca solo gold) · error inline por fila · **gap vertical 8 px** (fix §4). Estados: default/editing/done (wash 10 % + check ink + `aria-pressed` + texto "completada")/**disabled = read-only** (doble persona trainer↔trainee, misma fila con permisos, C4 [15]) · loading 3 filas skeleton · empty = ghost CTA "Añadir serie". Rest timer (C5 [12]): pill countdown tabular-nums + accent-ink sobre wash accent (7.96 ✓), estados idle/running/paused/finished; **paused usa t2 sobre raised** (6.12/6.37 ✓; t3 light 4.76 ✓ pero t2 es el paso normativo ahí, QA §11); `aria-live="polite"` solo al terminar, nunca los ticks. **Fix QA M (hueco #3)**: anchos de celda en `em` + fila apilada a ≥150 % de fuente (hoy fixed px: a 200 % caben 3 caracteres, `app.css:497-538`).

### 7.7 Hero metrics
Un hero por pantalla (M5 [6][22]). Valor 4.75rem/800 tabular-nums con `clamp(3rem,12vw,4.75rem)`; micro-label + delta + **status word** cuando hay rampa (X4: nunca rampa sola). **Fix QA P0-3**: en light el valor rampa usa `done-ink`/`pr-ink`/`err-ink` (5.38/5.91/5.70 ✓), nunca el fill (2.16/1.62/3.10 ✗); en dark la rampa pura pasa (7.94/10.63/5.55 ✓). Estados: loading skeleton con forma del número · **empty ≠ 0 desnudo** (guía + CTA, M4 [9]) · error + "Reintentar". Valor+label agrupados para lector de pantalla.

### 7.8 Matriz componente × estado
Idéntica a components-proposal §8 (card/CTA/chip/badge/sheet/FAB/set row/rest timer/hero × default/hover/pressed/disabled/loading/error/selected/empty, con N/A explícitos y hover solo bajo `pointer: fine`). Los fixes QA no cambian la matriz: cambian los tokens que consumen selected (borde, §7.3), danger (§7.2), sheet (§7.4) y timer paused (§7.6).

---

## 8. Accesibilidad verificada (criterio de aceptación)

Método: script propio `contrast_final_ba764d7a.py` (101 comprobaciones sobre los **valores finales** de §2; luminancia relativa sRGB WCAG + composición alfa premultiplicada; mismo método que QA, cuyos 175 pares reproducen §9 ±0.02). **Todos los pares normativos PASS**; las filas de descarte se listan para documentar por qué se rechazan los valores alternativos.

### 8.1 Contraste — DARK (texto ≥4.5 / gráfico ≥3.0)

| Par | Ratio | | Par | Ratio |
|---|---|---|---|---|
| blanco on accent #1B66E8 | 5.11 ✓ | | accent-ink on canvas/card/input/raised (foco) | 9.62/8.80/7.75/6.73 ✓ |
| accent on card / canvas (fill) | 3.39/3.70 ✓ | | ink #0E1116 on done / pr / err (fills saturados) | 7.94/10.63/5.55 ✓ |
| accent-ink on card / input / wash | 8.80/7.75/7.96 ✓ | | bordes chip puros on card (done/pr/err) | 7.26/9.73/5.07 ✓ |
| t1 on card / canvas | 15.71/17.17 ✓ | | t2 on card / raised (timer paused) | 8.01/6.12 ✓ |
| t3 #868FA0 on card / input | 5.31/4.68 ✓ | | done texto on wash-done/card · pr on wash-pr/card | 5.51/6.61 ✓ |
| **err-ink #FA8088 on wash-err card/input/raised/canvas** | **5.98/5.27/4.62/6.61 ✓** | | err-ink on card / input / raised | 7.01/6.17/5.36 ✓ |
| disabled t3 on raised (componente) | 4.06 ✓ | | washes compuestos: done #1A352F · pr #3E3727 · **err #35232A** | (hexes exactos, V) |

### 8.2 Contraste — LIGHT

| Par | Ratio | | Par | Ratio |
|---|---|---|---|---|
| blanco on accent | 5.11 ✓ | | accent-ink on canvas/card/input/raised (foco) | 6.65/7.32/6.82/6.17 ✓ |
| accent fill on card/canvas/input/raised | 5.11/4.64/4.76/4.30 ✓ | | done-ink on card/canvas/input/raised | 5.38/4.88/5.01/4.53 ✓ |
| accent-ink on card / wash | 7.32/6.38 ✓ | | pr-ink on card/canvas/input/raised | 5.91/5.37/5.51/4.98 ✓ |
| t1 on card / canvas | 17.79/16.16 ✓ | | err-ink on card/canvas/input/raised | 5.70/5.17/5.31/4.80 ✓ |
| t2 on card / raised | 7.56/6.37 ✓ | | done-ink/pr-ink/err-ink on su wash | 4.71/5.17/4.88 ✓ |
| **t3 #5D6878 on card/input/canvas/raised** | **5.65/5.26/5.13/4.76 ✓** | | disabled t3 on raised | 4.76 ✓ |

### 8.3 Pares descartados (por qué no se adoptan; V)

accent puro on input/raised dark 2.98/2.59 ✗ (→ foco usa accent-ink) · err-ink candidato #F4636A on wash-err/input 4.24 ✗ (→ #FA8088) · done/pr fill light como gráfico 2.38/1.78 ✗ y err fill light on raised 2.87 ✗ (→ `*-ink`) · t3 light candidato #657080 on raised 4.23 ✗ (→ #5D6878) · ink on washes dark 1.28–1.44 ✗ (→ regla acotada a fills saturados) · blanco on #F1545B 3.41 ✗ (→ `palette.ErrorContrastText = #0E1116` = 5.55 ✓, fix del fallo vivo QA §7).

### 8.4 Touch targets, dynamic type y resto del checklist

- **Targets**: 44 px diseño / 48 px denso + gap 8 px como tokens reales (§4); `@media (pointer: coarse)` oculta spinners de 20 px (patrón correcto ya en código, `app.css:152-157`); split chip/badge preserva densidad sin reducir targets.
- **Dynamic type**: escala tokenizada + validación 200 % por plantilla (§3); fixes de px→rem y anchos en `em`/fila apilada registrados como deuda WI (§11).
- **Foco**: `--hs-focus-ring` dedicado ≥3.0 sobre toda la ladder (§6); bloque `forced-colors` obligatorio; halo decorativo declarado no normativo.
- **Teclado/sheets**: Escape + focus trap + foco inicial + restore en sheets propias (§7.4); interactive card = un tab stop.
- **Motion**: `prefers-reduced-motion` cubre entrada de sheet, pressed, count-up, glow y timer.
- **Nunca color solo**: rampa + status word; PR con texto; error icono+texto; done check+texto.

Checklist operativo de verificación por pantalla (para QA en la implementación): los 16 puntos de `accessibility-checklist.md` §15, ya resueltos en diseño y asignados a WIs en §11.

---

## 9. Comparación explícita DESIGN.md v1 → propuesta v2

### 9.1 SE MANTIENE (verificado en la auditoría; no se toca)

| Elemento v1 | Por qué se mantiene |
|---|---|
| Ladder de 4 superficies sin sombras + scrim (P9) | pasa AA; coherente con HIG dark mode [F7] y Material overlays [44]; es la decisión central del sistema |
| Accent único + semánticos solo para su significado (P7) | "Color is used with absolute consistency… no legend ever needed" (WHOOP [6]) |
| Hexes de semánticos done/pr/err y rampa readiness (P8) | hexes propios ya declarados; los oficiales WHOOP son agresivos sobre fondo oscuro [F5] |
| Washes semánticos como fondo de texto | done 5.51 / pr 6.61 ✓ (V) |
| Inter + tabular-nums global (T1) | verificado en código (`app.css:88-90`); patrón fiel a spec |
| Radios 8/12/14/20, padding card 16, micro-labels UPPERCASE solo en labels/chips | jerarquía verificada; aprobado por Wall 2026-10-21 |
| Bottom nav 5 destinos + FAB de acción sin ruta duplicada + sidebar ≥1000 (C6) | patrón validado por WHOOP [8]; evidencia `MainLayout.razor:189` |
| Spec de pantallas (Hoy/Entreno/Resumen/Insights/Historial/Biblioteca), ≤4 taps, errores inline on-blur sin culpar (NN/g) | núcleo del producto (M1) y flujo de un paso (X3 [49][50]) |
| `focus-visible` global + `prefers-reduced-motion` (X5) | ya funcionan; se elevan a requisito de spec |
| Dark-first | decisión de producto declarada (P10 [Q-#13]), ya no "hallazgo de benchmark" |

### 9.2 SE AJUSTA (misma arquitectura, valor o regla corregida)

| # | v1 | v2 | Por qué (evidencia) |
|---|---|---|---|
| 1 | accent #1E6FFF | **#1B66E8** | CTA primario 4.40 ✗ → 5.11 ✓ (A-H1/P1) |
| 2 | accent crudo como texto (tabs, nombres) | regla **accent-fill vs accent-ink** (#98BAF5 dark / #1450B8 light) | texto accent 4.23/3.93 ✗; el código ya lo había resuelto sin documentarlo (A-H2/H4) |
| 3 | tertiary #78818F | **#868FA0** dark / **#5D6878** light | 4.39/3.87 ✗ (A-M2); light sobre raised 4.23 ✗ (QA J) |
| 4 | texto err = #F1545B | token **err-ink #FA8088** dark / #C22B33 light | err sobre wash/input 4.33/3.81 ✗ (QA B/P0-1) |
| 5 | disabled = opacity .55 | token explícito raised + t3 + `aria-disabled` | estado sin definición en la app (A-L3/C7) |
| 6 | FAB done icono blanco | **ink #0E1116** sobre fills saturados | 2.38 ✗ → 7.94 ✓ (A-H3/P3) |
| 7 | `.hs-chip` único | split **chip-interactivo / badge-estático** | badges inflados a 44 px destruyen densidad (A-L2/E3) |
| 8 | FAB glow contradice "sin sombras" | glow **eliminado** (salvo decisión Wall) | contradicción interna de la propia spec (A-L1/C7) |
| 9 | sheet sombra prescrita pero no aplicada | shadow-sheet **aplicado + borde superior** | drift `app.css:1092-1102`; sheet indistinguible del scrim 1.06–1.16 (QA I) |
| 10 | tipografía como prosa con tamaños sueltos | **8 pasos tokenizados** + requisito 200 % font scaling | M3/HIG/Android 14 [41][46][47]; WCAG 1.4.4 [40] |
| 11 | wash err sin alpha unificado | **14 %** en tokens y componentes; hex compuesto **#35232A** | conflicto 14/16 % (QA G); error aritmético §11 (QA F) |
| 12 | "ink sobre fills semánticos" sin acotar | acotada a **fills saturados**; sobre washes dark texto t1/t2/*-ink | ink sobre washes 1.28–1.44 ✗ (QA H) |
| 13 | foco = outline accent + halo | **--hs-focus-ring dedicado** (accent-ink) ≥3.0 sobre toda la ladder; halo declarado decorativo | accent nuevo sobre input/raised 2.98/2.59 ✗ (QA A/P0-2) |

### 9.3 SE RECONSTRUYE (no existía en v1; se define desde 0)

| # | Qué | Por qué |
|---|---|---|
| 1 | **Paleta light completa** (ladder espejo, §2.3/§2.4) | `PaletteLight` heredaba valores dark (`HealthspanTheme.cs:30-33`); el modo oscuro debe ser parte del sistema de color [43] (P5/A-M6) |
| 2 | **Ladder de breakpoints 480/768/1000** | 29 `@media` con 15 breakpoints ad-hoc (QA §14) → consolidación real con mapeo explícito (E1/A-M3) |
| 3 | **Spec machine-readable** (DESIGN.md v2 + `tokens.json` DTCG + lint de contraste) | la spec v1 era prosa no validable y prescribía contrastes que fallaban (A-M5/H4; decisión cruzada 1) |
| 4 | **Token contract de fuente única** | hoy hay 3 fuentes de verdad (`:root` + alias `--mud-palette-*` + `HealthspanTheme.cs`) y 12 hexos hardcodeados en razor/charts (A-M1/M4; C1/C2) |
| 5 | **Estados loading/empty/error por componente** (skeletons con forma, empty con guía, error inline) | v1 solo los mencionaba como regla general §6 |
| 6 | **Rest timer como componente con estados** idle/running/paused/finished | no existía en código ni en spec; C5 [12] |
| 7 | **Fila read-only (doble persona trainer↔trainee)** | variante disabled=read-only de la misma fila; C4 [15] |
| 8 | **Tokens de target reales** (`--hs-target` 44 / `--hs-target-dense` 48 / `--hs-gap-target` 8) | hoy 44 token + dos literales 48 + gap real 4 px en la tabla de series (QA §9) |
| 9 | **Contrato de sheets**: Escape + focus trap + foco inicial + restore | sheets propias no tienen ninguna de las cuatro (QA K) |
| 10 | **Soporte `forced-colors` + guards `@media (hover: hover)`** | 0 bloques HCM; 17 hover sin guard (QA L/N) |
| 11 | **Regla light para gráficos semánticos** (`*-ink`; fill saturado solo con ink encima) | fills light fallan 1.4.11 (2.38/1.78/2.87) (QA C/P0-3) |
| 12 | **Regla de borde de estado** (puro dark / `*-ink` light; 45 % solo decorativo) | borde 45 % invisible 1.21–2.97 (QA D/P0-4) |

### 9.4 Atribuciones corregidas (la v1 citaba mal; la v2 no repite)

| Atribución v1 | Corrección | Evidencia |
|---|---|---|
| Paleta atribuida a Hevy | **paleta propia de Healthspan**; el pack "reverse-engineered" no es fuente fiable y #1E6FFF no existe en assets de Hevy | Q-§3; B-§10 |
| "tabular-nums = feature de Hevy" | feature OpenType de **Inter** | T1 [F4] |
| "dark-first = Hevy" | decisión de producto apoyada en **WHOOP + HIG** (Hevy es claro por defecto) | Q-#13 [Q-§1] |
| Rampa = rampa WHOOP | adaptación propia con hexes propios; la oficial (#16EC06/#FF0026) es referencia conceptual | Q-#8 [F5] |
| Hero 76/800 como patrón WHOOP | patrón sostenido (cifras con tratamiento propio [F5]) pero **la escala es decisión propia** (sin captura de benchmark) | Q-#5 |

---

## 10. Token contract (reglas de consumo)

1. Ningún hexo literal en componentes (incluidos SVG/charts): todo color sale de `var(--hs-*)` o constantes del theme (C1).
2. **Una sola fuente de verdad**: DESIGN.md v2 (este documento, convertido a tokens.json DTCG) alimenta CSS y C#; se retiran los 14 alias legacy `--hs-color-*`→`--mud-palette-*` y las constantes duplicadas (C2).
3. **Lint de contraste**: la tabla §8 se convierte en test automatizado (extender `SharedVisualFoundationTests.cs` a los hexos nuevos y a los hexos razor) (A-M5/X2).
4. Roles obligatorios por uso: accent-fill solo fills; accent-ink para todo texto accent y foco; ink oscuro solo sobre fills saturados; washes solo vía token; cada componente declara los tokens que consume.

## 11. Notas de implementación para Pau (NO aplicadas aquí — work items)

Fixes vivos de código que la propuesta exige al implementar (con WI del mapa `workitems-tokens-base-css-t57a0e89b.md` / `workitems-componentes-design-system-tee4c71df.md`):

1. `palette.ErrorContrastText = "#0E1116"` en `HealthspanTheme.cs` (fallo vivo actual: blanco sobre #F1545B = 3.41 ✗ → 5.55 ✓; QA §7).
2. Migrar accent #1E6FFF→#1B66E8 **junto con** los 5 hexos razor hardcodeados (`SparklineChart.razor:32`, `TrendChart.razor:50`, `ExerciseTypeDetails.razor:526,749`, `Assessments.razor:414`) y `--hs-shadow-focus: rgba(30,111,255,.35)` (`app.css:68`) — si no, los gráficos se quedan en el azul viejo (QA §7).
3. Gap de la tabla de series 4→8 px (`app.css:527`); crear tokens `--hs-target`/`--hs-target-dense`/`--hs-gap-target` reales (QA §9).
4. Sheets propias: Escape + focus trap + foco inicial + restore (`Workouts.razor:327-421`) (QA §13).
5. Aplicar `--hs-shadow-sheet` + borde superior en sheets (`app.css:1102` hoy aplica shadow-panel) (QA §6F).
6. 11 `font-size` px→rem; anchos `.hs-settable` en `em` + fila apilada ≥150 % fuente (QA §10).
7. Guards `@media (hover: hover)` en las 17 reglas hover; bloque `forced-colors` (QA §8/§12).
8. Reconstruir `PaletteLight` con los valores §2.3/§2.4 (hoy hack dark) antes de validar el tema claro en runtime (QA §7).
9. Consolidación de breakpoints: mapear los 15 actuales a 480/768/1000 y probar 360/480/768/1000 (QA §14).

## 12. Decisiones abiertas (Wall / producto — no técnicas)

1. **FAB → CTA primario en sidebar desktop (≥1000 px)**: recomendación UX sí (el flotante solapa contenido y la sidebar ya tiene el destino; la acción queda a un toque). Necesita visto bueno de Wall (hueco #6 de components-proposal).
2. **Glow del FAB**: recomendación UX eliminarlo (contradicción L1). Si Wall lo conserva, se declara token único `--hs-fab-glow` de uso exclusivo del FAB (hueco #7).
3. Mock de validación previa a implementación: renderizar el toggle light en `redesign-mock.html` para que Wall vea la paleta light antes de escribir código (gate de mock, ux-ui-good-apps).

## 13. Criterios de aceptación (de t_38c41b61) — cumplimiento

| Criterio | Dónde se cumple |
|---|---|
| Tokens completos dark/light (ladder, accent único, semánticos), tipografía, espaciado, radios, elevación — cada decisión citada | §2–§6 (filas P/T/E/C/X/M + fichero:línea + V) |
| Catálogo de componentes base (card, CTA, chip, bottom sheet, FAB, timer/log, hero) con estados | §7 + matriz §7.8 (completa en components-proposal §8) |
| Accesibilidad verificada: contraste AA en ambas paletas, touch targets, dynamic type | §8 (101 comprobaciones sobre valores finales, todos los pares normativos PASS) + §8.4 |
| Comparación explícita DESIGN.md v1 → nueva propuesta (mantiene / reconstruye / por qué) | §9 (10 mantiene · 13 ajusta · 12 reconstruye + atribuciones corregidas) |
| Documento en docs/design/, propuesta sin cambios en código | este fichero; `git status`: solo `docs/` sin trackear, ningún componente/CSS modificado |

## 14. Reproducibilidad

- Script de verificación final: `~/.hermes/profiles/ux-designer/cache/scratch/contrast_final_ba764d7a.py` (101 comprobaciones; salida: 90 PASS normativos + 11 filas de descarte/acotación documentadas en §8.3).
- Coincide con la verificación independiente de QA (`a11y_contrast_qa.py`, 175 pares, `±0.02` sobre §9 de tokens-proposal).
- Los 4 P0 y 7 P1 de `accessibility-checklist.md` §15 quedan resueltos en diseño: P0-1→§2.2/§7.2 · P0-2→§6 · P0-3→§2.4/§7.7 · P0-4→§2.5/§7.3 · P1-5/6→§2.2 · P1-7→§2.5 · P1-8→§11.1 · P1-9→§6/§7.4 · P1-10→§2.4 · P1-11→§7.4. Los 6 P2 quedan registrados como deuda WI (§11.3–§11.7).
