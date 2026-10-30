# Accessibility checklist — tokens v2 + catálogo de componentes

**QA t_68a74222** · revisa `docs/design/tokens-proposal.md` (t_0098e568) y
`docs/design/components-proposal.md` (t_3c855b9f) · **sin cambios de código** (verificado:
`git status` solo muestra `docs/` sin trackear).

## 0. Veredicto

**Las dos propuestas NO están listas para implementarse tal cual.** La paleta dark y la
tabla §9 son correctas (las reproduje de forma independiente: los 43 pares dan los mismos
ratios, ±0.02). Los problemas están en lo que §9 **no midió**:

| # | Hallazgo | Severidad |
|---|---|---|
| A | El accent propuesto `#1B66E8` **rompe el estado de foco** sobre superficies `input`/`raised` (3.46→2.98 y 3.01→2.59; req 3.0) | **P0** |
| B | Hueco #1 confirmado: texto `err` sobre wash err **falla AA** (4.33 card / 3.81 input) | **P0** |
| C | En light, los fills semánticos `done`/`pr` **fallan 1.4.11 como gráfico** (2.38 / 1.78) | **P0** |
| D | Borde de chip "color al 45 %" es **invisible en ambos temas** (1.21–2.97); el 3.93 citado en components §1 es un número equivocado | **P0** |
| E | Botón destructivo actual pinta **blanco sobre `#F1545B` = 3.41** (fallo vivo, no propuesta) | **P1** |
| F | §11 tiene un **error aritmético** en el wash err compuesto | **P1** |
| G | Conflicto de alpha: tokens dicen wash err **14 %**, componentes usan **16 %** | **P1** |
| H | `ink #0E1116` sobre washes dark = 1.15–1.84 → la regla "ink sobre fills semánticos" debe acotarse a fills saturados | **P1** |
| I | Sheet dark es **indistinguible de su scrim** (1.06–1.16) y §5 prohíbe la sombra | **P1** |
| J | `t3` light sobre `raised` = 4.23 < 4.5 | **P1** |
| K | Sheets propias: `role=dialog`+`aria-modal` sí; **sin Escape, sin focus trap, sin foco inicial/restore** | **P1** |
| L | 17 reglas `:hover`, **0 guards** `@media (hover: hover)` → hover pegado en táctil | **P2** |
| M | 11 `font-size` en px literales y `table-layout: fixed` con anchos en px → clipping a 200 % de fuente | **P2** |
| N | 15 breakpoints de viewport en uso (ninguno es 480) · 0 soporte `forced-colors` | **P2** |

## 1. Método

Script propio `a11y_contrast_qa.py` (scratch QA; salida completa en
`qa_contrast_out.txt`, 175 pares). Fórmula WCAG 2.x: luminancia relativa sRGB,
`(L1+0.05)/(L2+0.05)`. Composición alfa premultiplicada sobre superficie opaca (lo que
pinta el navegador). `color-mix(in srgb, A p%, B)` = mezcla lineal sRGB, igual que
`app.css:13`. Requisitos: texto 4.5:1 (1.4.3), gráfico/componente 3.0:1 (1.4.11).

**Validación del método contra el del autor:** los dos pares que §11 afirma compuestos
reproducen exactos — done 16 % sobre card = 5.51 (doc: 5.53) y pr 18 % = 6.61 (doc: 6.63).
Es decir: mismo método, mismos números. Todo lo que sigue diverge es porque §9 no lo midió.

## 2. §9 reproducido (dark y light)

Todos los pares declarados se reproducen. Confirmadas además las dos correcciones que el
autor dice haber hecho: `err #F1545B` como texto sobre input dark = 4.47 → `err-ink
#F4636A` = 4.96 ✓; `t3` light `#6B7684` sobre input = 4.30 → `#657080` = 4.68 ✓.

## 3. Hueco #1 — CTA danger sobre wash err (components §2 línea 82, 327)

Medido con los tokens finales. Texto = `--hs-err #F1545B` pintado sobre el wash compuesto:

| alpha | card | canvas | input | raised |
|---|---|---|---|---|
| 14 % (tokens §2) | **4.33 ✗** | 4.79 ✓ | **3.81 ✗** | **3.35 ✗** |
| 16 % (components) | **4.21 ✗** | 4.66 ✓ | **3.72 ✗** | **3.27 ✗** |

**Falla AA en 3 de 4 superficies.** El patrón "CTA danger = wash err + texto err" no es
implementable con `#F1545B` como texto.

Candidatos a `--hs-err-ink` dark (texto sobre wash 14 %):

| valor | card | input | canvas | raised | verdict |
|---|---|---|---|---|---|
| `#F4636A` (propuesto hoy) | 4.81 | **4.24 ✗** | 5.32 | **3.72 ✗** | solo card/canvas |
| `#F8747B` | 5.46 | 4.82 | 6.04 | **4.23 ✗** | casi |
| **`#FA8088`** | 5.98 | 5.27 | 6.61 | 4.62 | **PASS en las 4** |
| `#F2838B` | 5.90 | 5.20 | 6.52 | 4.56 | PASS en las 4 |

**Ajuste recomendado:** `--hs-err-ink` dark → `#FA8088` (mantiene el rol, aclara el rojo
solo en dark; light `#C22B33` ya pasa: 5.70 card / 5.31 input / 4.88 wash ✓). Alternativa
más conservadora: fijar wash err 14 % y restringir el CTA danger a superficies `card`.

## 4. Hueco #2 — accent-ink con el accent nuevo

`color-mix(in srgb, #1B66E8 45%, #FFFFFF)` = **`#98BAF5` exacto** (el valor que §3 declara).
Ratios con el accent nuevo: card 8.81 · input 7.77 · wash 7.98 · nav/canvas 9.62. Todos
≥4.5. **Veredicto: `#98BAF5` sigue siendo correcto; no hay que recalcular el token**, solo
recalcular los usos que no usan el token (ver §6 A).

## 5. Washes compuestos (§11)

| wash | doc §11 | recalculado | |
|---|---|---|---|
| done 16 % sobre card | `#1A352F` | `#1A352F` | ✓ |
| pr 18 % sobre card | `#3E3727` | `#3E3727` | ✓ |
| err 14 % sobre card | `#2A1D24` | **`#35232A`** | **✗ error aritmético** |

El hex del wash err equivale a ~9 % de alpha, no 14 %. Corregir el hex o el alpha.
Además: **`ink #0E1116` sobre cualquier wash dark = 1.15–1.84** (invisible). La regla
"ink oscuro sobre fills semánticos" debe decir explícitamente *fills saturados*
(`done`/`pr`/`err` puros), no washes; sobre washes dark el texto es `t1`/`t2`/`*-ink`.

## 6. Hallazgos nuevos de contraste

**A. Accent sobre superficies `input` y `raised` (1.4.11)** — no está en §9:

| superficie | v1 `#1E6FFF` | propuesto `#1B66E8` |
|---|---|---|
| `input #1F2630` | 3.46 ✓ | **2.98 ✗** |
| `raised #283040` | 3.01 ✓ | **2.59 ✗** |

Consecuencia real y actual: `.hs-settable-input:focus-visible { outline: 2px solid
var(--hs-accent) }` (`app.css:549-551`) deja de cumplir 1.4.11 en cuanto el accent se
migre. **Ajuste:** definir `--hs-focus-ring` dedicado (o usar `--hs-accent-ink` en dark,
9.62 sobre input) y exigir ≥3.0 sobre *todas* las superficies de la ladder, no solo card.

**B. Fills semánticos light como gráfico** — la regla "mismo color en ambos temas"
(tokens §4) se rompe:

| fill | card | canvas | input | raised |
|---|---|---|---|---|
| `done #2FBF71` | **2.38 ✗** | **2.16 ✗** | **2.22 ✗** | **2.01 ✗** |
| `pr #F5B83D` | **1.78 ✗** | **1.62 ✗** | **1.66 ✗** | **1.50 ✗** |
| `err #F1545B` | 3.41 ✓ | 3.10 ✓ | 3.18 ✓ | **2.87 ✗** |

Afecta a barras de progreso, puntos de readiness, bordes de chip y al FAB "done" light
(2.16 sobre canvas). Como texto también fallan (done 2.38, pr 1.78, ramp sobre canvas
light 2.16/1.62/3.10). **Ajuste:** en light, todo gráfico semántico usa `*-ink`
(done-ink 5.38/4.88, pr-ink 5.91/5.37, err-ink 5.70/5.17 ✓); el fill saturado se reserva
para superficies donde el texto encima sea `ink #0E1116` (7.94 / 10.63 ✓).

**C. Borde de chip "1px color al 45 %"** (components §1 línea 117, §4):

| mix 45 % | dark card | light card |
|---|---|---|
| accent | **1.64** | **1.96** |
| done | **2.48** | **1.48** |
| pr | **2.97** | **1.30** |
| err | **1.96** | **1.74** |

El 3.93 que cita components §1 es `accent` puro sobre card, no el borde mezclado. Para
llegar a 3.0 harían falta: accent 86–91 % (dark) / 70–74 % (light); done 53–57 % dark y
**imposible en light**; pr 45–48 % dark e **imposible en light**; err 67–74 % dark /
89–97 % light. **Ajuste:** borde = color semántico puro en dark y `*-ink` en light
(ambos ≥3.0 en las cuatro superficies); el 45 % solo como tinte decorativo, nunca como
señal de estado. Esto sostiene la afirmación "el estado se lee por borde+peso+color".

**D. `t3` light sobre `raised` = 4.23 < 4.5** — `raised` es fondo pressed y skeleton; si
algún label vive ahí (timer `paused`, chips sobre raised) falla. Restringir texto en
raised a `t2` (6.37 ✓) o subir `t3` light a `#5D6878`.

**E. Blanco sobre fill err = 3.41** — ver §7 (es un fallo vivo, no una propuesta).

**F. Separación sheet/scrim** — §5 prohíbe sombra salvo sheet, pero el sheet no tiene
borde:

| caso | ratio |
|---|---|
| dark: sheet `#161B22` vs canvas+scrim `rgba(0,0,0,.55)` | **1.16** |
| dark: sheet vs backdrop real `rgba(8,26,38,.52)` (`app.css:1089`) | **1.06** |
| light: sheet `#FFF` vs canvas+scrim | 3.16 ✓ |
| light: sheet `#FFF` vs card+scrim | **2.92** |

En dark el sheet es literalmente indistinguible de su fondo. No es un fallo estricto de
1.4.11 para una superficie de diálogo, pero la "elevación funcional" declarada no existe.
**Ajuste:** sheet con borde superior 1px `--hs-border` (o superficie `raised`) además de
`--hs-shadow-sheet`, que hoy no se aplica (el código usa `--hs-shadow-panel`, `app.css:1102`).

## 7. Fallos vivos en el código actual (no en la propuesta)

| dónde | qué pinta | ratio | req |
|---|---|---|---|
| `.hs-workout-sheet-button.is-danger` (`app.css:1204-1208`) | `--mud-palette-error-text` = **blanco** sobre `--hs-color-danger #F1545B` | **3.41** | 4.5 ✗ |

Evidencia: `Shared/HealthspanTheme.cs:90` fija solo `palette.Error = Err`;
`ErrorContrastText` queda en el default de MudBlazor 6.21 (`Palette.cs` →
`Colors.Shades.White`). **Ajuste mínimo:** `palette.ErrorContrastText = "#0E1116"` → 5.55 ✓
(coherente con la regla "ink oscuro sobre fills semánticos").

Otros drifts que la migración de accent arrastrará:

- `--hs-shadow-focus: 0 0 0 3px rgba(30,111,255,.35)` (`app.css:68`) hardcodea el accent
  v1; no se actualiza con el token.
- `#1E6FFF` hardcodeado en 5 sitios Razor: `SparklineChart.razor:32`, `TrendChart.razor:50`,
  `ExerciseTypeDetails.razor:526,749`, `Assessments.razor:414` (+ `MuscleLoadBodyMap` con
  `#2FBF71/#F5B83D/#F1545B` en constantes C#). Tras migrar accent, los gráficos se quedan
  en el azul viejo.
- `--hs-color-primary: var(--mud-palette-primary)` (`app.css:23-31`) → la fuente de verdad
  real es `HealthspanTheme.cs`, no `:root`. §10 afirma lo contrario. Hay **tres** fuentes.
- `PaletteLight` sigue con el hack de valores dark (`HealthspanTheme.cs`, acknowledged en
  tokens §4 línea 63): cualquier componente que lea la paleta light hereda superficies
  oscuras. El ladder light propuesto no se puede validar en runtime hasta reconstruir el
  theme.

## 8. Estados de foco

| punto | estado actual | veredicto |
|---|---|---|
| `:focus-visible` global outline 2px (`app.css:93`) | usa `--mud-palette-primary` | funciona pero es 3ª fuente de verdad; con accent nuevo falla sobre input/raised (§6 A) |
| anillo `box-shadow` 35 % alpha | 1.41–1.67 vs superficies | **no es indicador normativo**; es decoración. El indicador lo carga el outline |
| `.mud-input-slot:focus-visible { outline:none }` | delega en MudBlazor | hay que confirmar que Mud pinta el suyo con ≥3.0; si no, el input pierde toda señal de foco |
| CTA primary focus (`app.css:5646`) | `t1` sobre accent fill | 4.64 ✓ |
| `forced-colors` (High Contrast) | **0 bloques** | en Windows HCM se ignoran los colores: sin `outline`/`border` explícitos varios estados desaparecen (1.4.11) |
| `prefers-reduced-motion` | 1 bloque (`app.css:105`) | ✓ existe; falta cubrir el glow del FAB y el timer |

## 9. Touch targets

- Token real: `--hs-touch-target: 44px` (`app.css:71`), usado en 12 reglas ✓.
- **48 px solo existe como literal** en `app.css:3351` y `app.css:3880` (y `--hs-space-7`).
  Los tokens propuestos `--hs-target` / `--hs-target-dense` **no existen** en código →
  drift de nombre a resolver en el token contract.
- `gap-target` 8 px: no existe como token; `--hs-space-1` = 4 px se usa entre targets de
  la tabla de series (`app.css:527`) → **por debajo del mínimo declarado**.
- ✓ correcto y verificable: `@media (pointer: coarse)` oculta los spinners numéricos de 20
  px (`app.css:152-157`) — dos botones de 20 px dentro de un campo de 44 px no pueden
  cumplir nunca 44; la solución elegida (el campo es el target) es la correcta.
- Badges inflados: `.hs-chip` con `min-height: var(--hs-touch-target)` (`app.css:3959`)
  aplicado a badges estáticos ✓ coincide con el split chip/badge propuesto.

## 10. Dynamic type (200 % de fuente)

- Unidades: 137 `font-size` en `rem` ✓, **11 en px literales** (`app.css:3229` 34px,
  `3636`/`3642` 10px, `5481` 10.5px, `5666`/`5722`/`5948` 11px, `5703` 12px, `5782`/`5900`
  11.5px, `5987` 9.5px). Esos no escalan con la preferencia del usuario (1.4.4).
- 25 `overflow: hidden` + 10 `text-overflow: ellipsis`: el texto se recorta en lugar de
  reflowar al escalar.
- **Hueco #3 (set table a 200 % en 360 px) — medido, y falla.** `.hs-settable` es
  `table-layout: fixed` con columnas de 42 / 78 / 44 px (`app.css:497-516`) y los inputs
  `flex: 1 1 0; min-width: 0` (`app.css:535-538`). En 360 px con padding de página, la
  columna de carga queda en ~164 px → dos inputs de ~78 px → ~58 px de contenido. Con
  fuente al 200 % (32 px, ~19 px/dígito tabular) caben **3 caracteres**: `12.5` o `3x12`
  se cortan. Los anchos fijos en px no escalan con la fuente → **no hay reflow posible**.
  Ajuste: anchos en `em` o variante de fila apilada a partir de ~150 % de fuente (no solo
  en <480 px).
- Hero (`app.css:476-482`): `4.75rem`, `line-height: 1`, `letter-spacing: -0.045em`,
  weight 800. A 200 % = 152 px de alto en una fila de línea 1 → riesgo de colisión con el
  label y de recorte vertical en 360 px. `line-height: 1` con tracking negativo es
  legible para números pero frágil para texto.

## 11. Timer / log de serie y hero metrics

- No existe componente de rest timer en código (0 matches) → todo lo siguiente es
  requisito a fijar en la spec, no verificación: número `tabular-nums` (ya hay 6 usos del
  property ✓), texto ≥`--hs-font-size-md` (16 px) para lectura a distancia de brazo con
  pantalla sucia, contraste del pill sobre `card` (wash accent 16 % + accent-ink ✓ 7.96),
  estado `paused` sobre `raised` → **usar `t2`, no `t3`** (dark 6.0 ✓ / light 6.37 ✓;
  `t3` light 4.23 ✗), anuncio `aria-live="polite"` al terminar (no assertivo), y número
  que no cambie de ancho al contar hacia atrás (tabular-nums lo garantiza).
- Hero: `t1` sobre canvas 17.17 ✓; el **color del valor por concepto** (readiness ramp)
  falla en light como texto (2.16 / 1.62 / 3.10) → en light el valor del hero debe usar
  `done-ink`/`pr-ink`/`err-ink`, no el fill. En dark el ramp puro sí pasa (7.94 / 10.63 /
  5.55 ✓).

## 12. Estados de componente

- `disabled`: token propuesto correcto (fg `t3` sobre `raised`: dark 4.06, light 4.23 —
  ≥3.0 para componente; **no** usar para texto legible). Falta en código: no existe
  `--hs-disabled-*`.
- `pressed`: sin token de superficie dedicada; `raised` es el candidato y arrastra el
  problema §6 A (accent 2.59 sobre raised).
- `hover`: **17 reglas `:hover`, 0 guards `@media (hover: hover)`** → en táctil el estado
  queda pegado tras el tap (y compite con `pressed`). Ajuste: envolver las 17 en
  `@media (hover: hover) and (pointer: fine)`.
- `selected`: depende del borde (ver §6 C) y del wash; con el borde invisible el estado
  pasa a depender solo del tinte → **color como única señal** (1.4.1), que la propia spec
  prohíbe.
- `loading`/`empty`/`error`: definidos en la spec; `error` necesita el fix de §7 y el de
  §3 antes de implementarse.

## 13. Sheets y diálogos

- Sheets propias (`Pages/Workouts.razor:327-331, 379-383, 417-421`): `role="dialog"`,
  `aria-modal="true"`, `aria-labelledby` ✓ y botón Cancel ✓ (cerrable por teclado).
- **Faltan:** cierre con `Escape`, focus trap (con `aria-modal="true"` el fondo se declara
  inerte pero sigue alcanzable con Tab → la declaración es falsa), foco inicial dentro del
  sheet y restauración del foco al trigger al cerrar. Hueco #4: los `MudDialog`
  (`FinishSessionDialog.razor`, `ExerciseFilterDialog.razor`) sí delegan en el provider de
  MudBlazor 6.21; las sheets propias no tienen ninguna de las cuatro cosas.
- Cierre por tap en scrim ✓ implementado (`@onclick` en backdrop + `stopPropagation` en
  la sheet).

## 14. Breakpoints

29 bloques `@media`, **15 breakpoints de viewport distintos**: 380, 420, 599, 600, 699,
760, 768, 800, 959, 960, 999 (max) y 700, 769, 950, 1000 (min). **Ninguno es 480.**
Confirmado el hallazgo de la auditoría: la ladder 480/768/1000 es una consolidación real,
no un rename. Riesgo a vigilar al consolidar: 599/600 y 959/960/999/1000 son pares
"justo antes de" que hoy definen layouts distintos; mapear cada uno a 480/768/1000 y
probar los 4 anchos (360, 480, 768, 1000) tras el cambio.

## 15. Ajustes recomendados (orden de aplicación)

**P0 — bloquean la implementación**
1. `--hs-err-ink` dark → `#FA8088` (o restringir CTA danger a `card` con wash 14 %).
2. Token `--hs-focus-ring` dedicado, verificado ≥3.0 sobre canvas/card/input/raised en
   ambos temas; no reutilizar el accent para el outline.
3. Regla nueva: en light, gráficos semánticos (barras, puntos, bordes, FAB) usan `*-ink`;
   el fill saturado solo con `ink #0E1116` encima.
4. Borde de estado = color semántico puro (dark) / `*-ink` (light); eliminar el "45 %"
   como señal de estado y corregir el 3.93 de components §1.

**P1 — corregir antes de cerrar las specs**
5. §11: hex del wash err → `#35232A` (o alpha 9 %).
6. Unificar alpha del wash err: 14 % (tokens) vs 16 % (components).
7. Acotar la regla "ink oscuro sobre fills semánticos" a fills saturados.
8. `palette.ErrorContrastText = "#0E1116"` (fallo vivo 3.41 → 5.55).
9. Sheet: borde superior o superficie `raised`; aplicar la sombra declarada.
10. `t3` light no se usa sobre `raised` (usar `t2`) o subirlo a `#5D6878`.
11. Sheets propias: Escape + focus trap + foco inicial + restore.

**P2 — deuda de implementación a registrar**
12. `@media (hover: hover)` en las 17 reglas hover.
13. `--hs-target` / `--hs-target-dense` / `--hs-gap-target` como tokens reales (hoy 44 px
    token + dos literales de 48 px y gap de 4 px en la tabla de series).
14. Los 11 `font-size` en px → `rem`; anchos de `.hs-settable` en `em` o fila apilada a
    ≥150 % de fuente.
15. Bloque `forced-colors` para estados de foco y semántica.
16. Sustituir los 5 hexos hardcodeados en Razor y el `rgba(30,111,255,.35)` de
    `--hs-shadow-focus` por tokens; declarar en §10 que MudBlazor es fuente de verdad
    (o eliminar esa dependencia).
17. Decisión de producto pendiente (no es QA): FAB→CTA en sidebar desktop y glow del FAB.

## 16. Reproducibilidad

- Script: `a11y_contrast_qa.py` (175 pares, incluye los 43 de §9 y los no cubiertos).
- Salida completa: `qa_contrast_out.txt` (adjunta a la tarjeta t_68a74222).
- Tokens de entrada copiados literalmente de tokens-proposal §2/§3/§4/§10; superficies de
  código verificadas contra `wwwroot/css/app.css` (líneas citadas en cada hallazgo).
