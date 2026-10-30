# Propuesta de design tokens v2 — Healthspan (t_0098e568)

Fecha: 2026-10-23 · Autor: UX (ux-designer) · Estado: **propuesta — no aplicada en código**
Insumo para: t_68a74222 (QA accesibilidad), t_ba764d7a (propuesta final), t_38c41b61 (design system) y los work items WI-T1…WI-T8 de `docs/design/workitems-tokens-base-css-t57a0e89b.md`.

**Fuentes y convención de cita** (las mismas que `docs/design/evidence-matrix.md`):
- **A** = auditoría `docs/design/audit-design-system-v1-t511ecd88.md` (hallazgos H1–L3, evidencia `fichero:línea`)
- **B** = benchmark `docs/design/research-ux-apps-2026.md` (ledger [1]–[23])
- **L** = literatura (comentario t_8a8f583d; ledger [25]–[54])
- **Q** = QA de citas `docs/design/benchmark-citations.md` ([F1]–[F11])
- **M** = matriz de evidencia `docs/design/evidence-matrix.md` (filas P/T/E/C/X)
- **V** = verificación propia de esta propuesta: script WCAG `contrast_v2.py` (scratch; fórmula relative luminance WCAG, misma que A); tabla completa en §9.

Todos los contrastes citados en este documento están recalculados para los valores propuestos (§9), no heredados de la auditoría.

---

## 0. Principios que ordenan los tokens

1. **Dark-first como decisión de producto declarada**, no como hallazgo de benchmark. Ninguna app del benchmark es dark-only (Hevy claro por defecto; Apple alterna); WHOOP sí es dark-native (72–94 % píxeles oscuros). La paleta light se define en esta propuesta como deuda pagada. (M-P10 [Q-#13]; M-P5; L-HIG dark mode [F7].)
2. **Un solo accent accionable; los semánticos solo para su significado.** "Color is used with absolute consistency across the entire app, no legend ever needed" — WHOOP [6]. (M-P7; DESIGN.md v1 §6.)
3. **Regla de dos papeles del accent: `accent` (fill) y `accent-ink` (texto).** El accent crudo como texto falló AA en v1 (4.23:1 sobre nav-bg, 3.93:1 sobre card); el código ya lo había resuelto sin que la spec lo documentara. (A-H2/H4; M-P2.)
4. **La spec es la fuente única de verdad**: todo token con nombre, valores dark y light explícitos, machine-readable (design-md + export DTCG), con lint de contraste. (A-M4/M5; M-C2; decisión cruzada 1.)
5. **La paleta es propia de Healthspan**: se elimina toda atribución a Hevy (el pack "reverse-engineered" no es fuente fiable; `#1E6FFF` no existe en assets de Hevy). (M-P6 [Q-§3, B-§10].)
6. **AA es contrato de aceptación, en ambos temas**: ≥4.5:1 texto, ≥3:1 iconos/componentes. (M-X2; WCAG 1.4.3 [27], 1.4.11 [39]; Material "texto grande/graphics ≥3:1, texto pequeño ≥4.5:1" [45].)

---

## 1. Color — ladder de superficies DARK (se mantiene la arquitectura v1)

| Token | Valor v1 | **Valor propuesto** | Justificación (una línea) | Cita |
|---|---|---|---|---|
| `--hs-canvas` | #0E1116 | **#0E1116** (igual) | Fondo base del ladder sin sombras; coherente con la plataforma y cercano a la superficie oscura recomendada por Material. | A "Lo que se sostiene"; M-P9; L-M2 dark theme [44] |
| `--hs-card` | #161B22 | **#161B22** (igual) | Superficie elevada 1: profundidad por luminosidad + borde 1px, no por sombra. | HIG dark mode: fondos base y elevated "to enhance the perception of depth when one dark interface is layered above another" [F7]; M-P9 |
| `--hs-card-border` | #262C36 | **#262C36** (igual) | Borde 1px como único separador; decorativo (no necesita cumplir 3:1). | A veredicto; M-P9 |
| `--hs-input` | #1F2630 | **#1F2630** (igual) | Superficie 2 para campos editables de la set table. | A inventario (`app.css:4-7`) |
| `--hs-raised` | #283040 | **#283040** (igual) | Superficie 3 pressed/raised; también base del estado disabled (§7). | A-L3 |
| `--hs-scrim` | rgba(0,0,0,.55) | **rgba(0,0,0,.55)** (igual) | Overlay modal; en dark el scrim negro es la única "elevación" admitida además del sheet. | DESIGN.md v1 §1 |

**Decisión clave:** se mantiene el ladder de 4 superficies sin sombras (pasa AA y es coherente con Apple Fitness/Linear); se reformula la redacción "hairline borders" como **"profundidad por luminosidad + borde 1px"** para resolver la contradicción interna de v1 (sombras vs glow). (A veredicto + A-L1; M-P9.)

## 2. Color — accent y semánticos DARK (ajustes justificados)

| Token | Valor v1 | **Valor propuesto** | Justificación | Cita |
|---|---|---|---|---|
| `--hs-accent` (solo fills) | #1E6FFF | **#1B66E8** | El CTA primario (el botón más usado de la app) fallaba AA: blanco sobre #1E6FFF = 4.40:1. Con #1B66E8: blanco 5.11:1 ✓ y el fill sigue ≥3:1 sobre card/canvas (1.4.11 ✓). | A-H1; WCAG 1.4.3 [27]; V: 5.11 / 3.39 / 3.70 (§9) |
| `--hs-accent-ink` (texto) | indocumentado (#9ABEFF) | **#98BAF5** (color-mix accent 45 % + blanco) | Todo accent-text (tabs, nombres de ejercicio, timer, chips, CTA-sec) usa ink: 8.80:1 sobre card, 7.75 sobre input, 7.96 sobre wash — todo AA. La regla "crudo solo fills" elimina la clase de fallo H2. | A-H2/H4; M-P2/C3; V §9 |
| `--hs-accent-wash` | #14233F | **#14233F** (igual) | Fondo tintado de CTA-sec/chips; con accent-ink pasa AA (antes, con accent crudo, 3.56:1 ✗). | A-H4; V: 7.96 §9 |
| `--hs-done` (fill) | #2FBF71 | **#2FBF71** (igual) | Rampa de readiness con hexes propios declarados (adaptación de la rampa WHOOP, no copia: los oficiales #16EC06/#FF0026 son agresivos sobre fondo oscuro). | M-P8 [F5]; M-P6 |
| `--hs-on-done` (ink sobre fill) | — (blanco ✗) | **#0E1116** | Icono blanco sobre done = 2.38:1 (fallaba incluso 1.4.11); ink oscuro = 7.94:1 ✓. | A-H3; WCAG 1.4.11 [39]; V: 7.94 §9 |
| `--hs-pr` (fill) | #F5B83D | **#F5B83D** (igual) | Semántico PR/hito; ink oscuro sobre él = 10.63:1 ✓. | V §9 |
| `--hs-err` (fill/borde) | #F1545B | **#F1545B** (igual) | Semántico error; sobre card 5.07:1 ✓. | V §9 |
| `--hs-err-ink` (texto) | — | **#F4636A** (nuevo) | #F1545B como texto sobre input = 4.47:1, justo bajo AA; el ink claro pasa (4.96 input / 5.63 card). Los mensajes de error inline (regla NN/g) necesitan AA sobre cualquier superficie. | V §9; ux-ui-good-apps regla 5 |
| `--hs-t1` | #F2F4F8 | **#F2F4F8** (igual) | Texto primario: 15.7 sobre card. | V §9 |
| `--hs-t2` | #A9B1BE | **#A9B1BE** (igual) | Texto secundario: 8.0 sobre card. | V §9 |
| `--hs-t3` | #78818F | **#868FA0** | Tertiary se usaba como texto real de 12.5px (columna "Anterior") y fallaba (4.39 card / 3.87 input); el ajuste pasa 5.31 / 4.68. Es ajuste de **token de la escala**, no hexo local. | A-M2; M-P4/T4; V §9 |
| Washes (fills semánticos tenues) | done 16 %, PR 18 % | **done rgba(47,191,113,.16) · PR rgba(245,184,61,.18) · err rgba(241,84,91,.14)** | Los washes de v1 pasan AA como fondo de texto (done 5.53, PR 6.63 en A); se declaran tokens para que chips/CTA-sec los usen sin hexos sueltos. | A veredicto; M-C3 |

**Regla de uso (obligatoria en la spec):** un concepto = un color, siempre (done/PR/err/accent); nunca decorativos; nunca color como única señal (color+icon+text). (M-P7 [6]; M-X4 [23].)

## 3. Color — ladder de superficies LIGHT (reconstrucción desde 0)

`PaletteLight` heredaba los valores dark (hack documentado); si llegaba el toggle, todo componente que leyera la paleta light heredaría superficies oscuras. La reconstrucción es un **ladder espejo**: en dark la profundidad sube de luminosidad (canvas→card→input→raised); en light baja (canvas gris → card blanco → input ligeramente recessed → raised presionado más gris), siempre con borde 1px y **el mismo accent**. Un color scheme cubre roles "across light and dark themes" — el modo oscuro es parte del sistema de color, no un tema aparte. (A-M6; M-P5; M3 choosing a scheme [43].)

| Token | **Valor propuesto** | Rol | Justificación | Cita |
|---|---|---|---|---|
| `--hs-canvas` | **#F2F4F8** | Fondo base (gris frío derivado de la temperatura de la paleta dark) | Espejo del canvas dark; da contraste a las cards blancas sin sombras. | M-P5; M3 [43] |
| `--hs-card` | **#FFFFFF** | Superficie 1 | Máxima luminosidad = máxima elevación en light (espejo del ladder dark). | M-P9 [F7] (principio base/elevated) |
| `--hs-card-border` | **#D9DFE7** | Borde 1px | Único separador en light, igual que en dark. | M-P9 |
| `--hs-input` | **#F5F7FA** (borde #C6CFDB) | Superficie 2 campos | Input recessed sobre card blanca (distinción por relleno + borde, delta verificado 1.07). | V §9 |
| `--hs-raised` | **#E7ECF3** | Superficie 3 pressed/disabled | Espejo invertido de raised dark: en light presionar = oscurecer. | V §9 |
| `--hs-scrim` | **rgba(20,24,31,.45)** | Overlay modal | Mismo rol que dark, tinta derivada de t1 light. | — |

### Accent y semánticos LIGHT (mismo accent, ink espejo)

| Token | **Valor propuesto** | Justificación | Cita |
|---|---|---|---|
| `--hs-accent` (fills) | **#1B66E8** (idéntico al dark) | El fill no cambia entre temas: blanco sobre él 5.11:1 ✓; sobre card 5.11 y sobre canvas 4.64 (≥3 ✓). Un solo accent en ambos temas. | V §9; M-P1 |
| `--hs-accent-ink` (texto) | **#1450B8** | En light el ink se oscurece (espejo de #98BAF5): 7.32 sobre card, 6.38 sobre wash ✓. | V §9; M-P2 |
| `--hs-accent-wash` | **#E8F0FC** | Fondo tintado CTA-sec/chips en light. | V §9 |
| `--hs-done` / `--hs-on-done` | **#2FBF71 / #0E1116** (idénticos) | Los fills semánticos son idénticos en ambos temas (mismo concepto, mismo color); ink oscuro sobre done 7.94 ✓. | M-P7; V §9 |
| `--hs-done-ink` (texto) | **#157A46** | #2FBF71 como texto sobre blanco ≈2.2 ✗; el ink pasa 5.38 card / 4.71 wash. | V §9; WCAG 1.4.3 [27] |
| `--hs-done-wash` | **#E3F4EB** | Wash de fila completada en light. | V §9 |
| `--hs-pr-ink` / `--hs-pr-wash` | **#8A5A12 / #FBEEDB** | #F5B83D como texto sobre blanco falla; ink propio 5.91 card / 5.17 wash ✓. | V §9 |
| `--hs-err-ink` / `--hs-err-wash` | **#C22B33 / #FCE9EA** | Texto de error AA sobre card/input/wash (5.70 / 5.31 / 4.88). | V §9 |
| `--hs-t1` / `--hs-t2` / `--hs-t3` | **#14181F / #4B5563 / #657080** | t3 calibrado para pasar AA sobre las tres superficies claras (5.02 card / 4.68 input / 4.56 canvas); es el espejo exacto del fix dark de A-M2. | V §9; A-M2 |

**Nota de método:** los hexes light se eligieron por delta de luminancia espejo del ladder dark y se validaron numéricamente (§9); no son copia de ninguna app (ninguna referencia del benchmark tiene paleta light documentada con hexes — Q-§5). Son decisión propia, igual que la rampa de readiness (M-P8).

## 4. Tipografía

| Token | Valor | Justificación | Cita |
|---|---|---|---|
| `--hs-font-family` | **Inter** (400/500/600/700/800), fallback system-ui | Familia verificada en código y fiel a spec; práctica sostenida por la auditoría. | A veredicto; evidencia `app.css:38`, `index.html:10-12` |
| `font-variant-numeric` | **tabular-nums global (body)** | Ningún número (series, kg, timer) debe reflowar al cambiar. Atribución corregida: es feature OpenType de Inter, no "de Hevy". | T1: Inter (rsms) "tabular numbers" [F4]; evidencia `app.css:88-90` |
| `--hs-type-hero` | **4.75rem (76px) / 800 / lh 1 / tracking −0.045em** | Números hero con tratamiento propio dentro de la escala (patrón WHOOP: cifras ≠ palabras); la escala concreta es decisión propia (sin captura de benchmark). | T3: WHOOP "Prefer to use DINPro for numbers" [F5]; Q-#5; evidencia `app.css:476-482` |
| `--hs-type-metric` | 1.5rem (24px) / 800 / lh 1.1 | Valor de tile. | DESIGN.md v1 §2 |
| `--hs-type-screen-title` | 1.875rem (30px) / 800 / lh 1.2 / tracking −0.02em | Título de pantalla. | DESIGN.md v1 §2 |
| `--hs-type-section-title` | 1.0625rem (17px) / 700 | Nombre de entreno / sección. | DESIGN.md v1 §2 |
| `--hs-type-item-title` | 0.9375rem (15px) / 700 | Nombre de ejercicio (en accent-ink, §2). | A-H2 |
| `--hs-type-body` | 0.9375rem (15px) / 500 / lh 1.5 | Cuerpo. | DESIGN.md v1 §2 |
| `--hs-type-meta` | 0.78125rem (12.5px) / 500 / color t3 | Meta y columna "Anterior" (el paso que fallaba AA se corrige vía token de color, no de tamaño). | A-M2; M-T4 |
| `--hs-type-micro-label` | 0.6875rem (11px) / 700 / uppercase / tracking 0.08em | Solo labels de columna, chips, hero-labels; nunca body ni títulos. | evidencia `app.css:484-490` |

**Requisito no negociable asociado:** la escala existe como tokens (tamaño/line-height/tracking/peso por paso) y el layout de cada plantilla debe aguantar **200 % de amplificación de fuente** sin pérdida de contenido ni funcionalidad. M3 define una única escala tokenizada (30 estilos); HIG exige Dynamic Type/AX sizes; Android 14 escala hasta 200 %. (M-T2: M3 [41], HIG [47], WCAG 1.4.4 [40], Android 14 [46]; M-X6.)

## 5. Espaciado, targets y breakpoints

| Token | Valor | Justificación | Cita |
|---|---|---|---|
| `--hs-space-1…7` | **4 / 8 / 12 / 16 / 24 / 32 / 48 px** (se mantiene) | Ladder de 4px ya implementado y coherente con la rejilla de múltiplos de 4 de Material; se nombra en la spec (hoy existe en código sin nombrar en spec). | evidencia `app.css:54-60`; A inventario (drift código→spec) |
| `--hs-gap-target` | **8 px mínimo entre targets adyacentes** | En listas densas (sets/reps, steppers) la separación es parte del token de componente, no un resto de layout: "con ≥8 dp de separación" (Android) y "el espaciado importa tanto como el tamaño" (HIG). | M-E2: Android [28], HIG [48] |
| `--hs-target` | **44 px** (mínimo de diseño) | Convergencia M3↔HIG: HIG "a button needs a hit region of at least 44x44 pt"; WCAG 2.5.8 (24 px) es solo el suelo normativo, nunca el diseño. | M-X1: HIG [30], WCAG [25] |
| `--hs-target-dense` | **48 px** (listas densas y pantallas en movimiento) | Android 48dp; en contexto gym (guantes, sudor) los objetivos pequeños obligan a gestos de precisión — Strava documenta que "buttons (placement, padding, and size)… are the top areas of concern for cyclists". | M-X1 [28]; M-E4 [3] |
| `--hs-breakpoint-sm/md/lg` | **480 / 768 / 1000 px** (3 escalones) | v1 tiene 29 `@media` con ≥10 breakpoints ad-hoc (380…1000) → riesgo de estados rotos en móvil (objetivo primario). Todos los sistemas de referencia usan 2–3 escalones. | A-M3; M-E1; ux-ui-good-apps §8 |
| `--hs-bottom-nav-height` / `--hs-topbar-height` / `--hs-sidebar-width` / `--hs-content-max` | 68 / 52 / 264 / 1280 px (se mantiene) | Shell ya implementado y probado; solo se nombra en la spec. | evidencia `app.css:70-74` |
| Chip vs badge | chip interactivo ≥44 px; badge estático tamaño por contenido | `.hs-chip` min-height 44px infla densidad aplicado a badges estáticos (PR badge). | A-L2; M-E3 |

## 6. Radios

| Token | Valor | Justificación | Cita |
|---|---|---|---|
| `--hs-radius-input` / `--hs-radius-chip` | **8 px** | Jerarquía v1 verificada en código (input/chip pequeños). | evidencia `app.css:46-52` |
| `--hs-radius-button` | **12 px** | CTA primario v1 (radio 12); se unifica el valor 10/12 duplicado hoy en código. | DESIGN.md v1 §3; `app.css:48` |
| `--hs-radius-card` | **14 px** | Card hero de la interfaz; aprobado por Wall en v1 y coherente con la plataforma. | DESIGN.md v1 (aprobado 2026-10-21) |
| `--hs-radius-sheet` | **20 px** (esquina superior) | Bottom sheet; radio mayor = superficie mayor. | DESIGN.md v1 §3 |

Regla: radio creciente con el tamaño de la superficie (8→12→14→20); sin radios ad-hoc por componente (token contract, §8).

## 7. Elevación, estados y foco

| Token | Valor | Justificación | Cita |
|---|---|---|---|
| `--hs-shadow-card` / `--hs-shadow-panel` | **none** | Profundidad por ladder de superficies + borde 1px, no por sombras (decisión central del sistema; Material comunica elevación con overlays 0–16 % — aquí la elevación es la propia ladder). | M-P9 [F7]; L-M2 dark theme [44] |
| `--hs-shadow-sheet` | dark `0 -12px 32px rgba(0,0,0,.6)` · light `0 -12px 32px rgba(20,24,31,.18)`` | Única sombra admitida: el bottom sheet flota sobre scrim; en light la sombra se tinta con t1 (una sombra negra pura sobre fondo claro se ve sucia). | DESIGN.md v1 §3; V (decisión propia, sin benchmark de hexo) |
| `--hs-glow-fab` | **permitido solo en FAB, declarado "affordance ≠ elevación"** (o eliminado) | v1 se contradice a sí misma (§1 "SIN sombras" vs glow del FAB implementado). La spec nueva zanja: el glow es señal de acción primaria, no elevación, y solo existe en el FAB. | A-L1; M-C7; evidencia `app.css:462` |
| `--hs-focus-ring` | `0 0 0 3px rgba(27,102,232,.35)` + outline 2px (accent-ink en light) | Estado de foco visible global ya funcionando; se lleva a la spec como requisito, no como suerte de implementación. El anillo usa el nuevo accent. | M-X5; evidencia `app.css:92-97` |
| `--hs-disabled-fg` / `--hs-disabled-bg` | **t3 sobre raised** (ambos temas) | v1 resuelve disabled por opacity .55 sin token; la spec define el estado explícitamente (t3 sobre raised: 4.06 dark / 4.23 light). | A-L3; M-C7; V §9 |
| `--hs-transition-fast` | 160 ms ease; **`prefers-reduced-motion` obligatorio** | Reduce motion es requisito AA-adjacente del sistema (Apple lo lista entre sus cinco funciones visuales; v1 ya lo implementa). | M-X4 [23], M-X5; evidencia `app.css:105` |

## 8. Token contract (reglas de consumo de los tokens)

1. **Ningún hexo literal en componentes** (incluidos SVG/charts): todo color sale de `var(--hs-*)` o de constantes del theme. Hoy 12 hexos hardcodeados en 6 componentes razor saltan el contract y no se propagarían con el cambio de accent. (A-M1; M-C1; ux-ui-good-apps §8 "Unified".)
2. **Una sola fuente de verdad**: la spec de tokens (DESIGN.md v2 machine-readable + `tokens.json` DTCG) alimenta CSS y C#; se retiran los 14 alias legacy `--hs-color-*` → `--mud-palette-*` y las constantes duplicadas. (A-M4; M-C2; WI-T1/WI-T2/WI-T7 del mapa de work items.)
3. **Lint de contraste en la spec**: la tabla de §9 se convierte en test automatizado (extender `SharedVisualFoundationTests.cs` a los nuevos hexos y a los hexos razor). (A-M1/M5; M-X2; decisión cruzada 1.)
4. **Roles obligatorios por uso**: accent-fill solo en fills; accent-ink para todo texto accent; ink oscuro sobre fills semánticos; washes solo vía token. Cada componente de la propuesta de componentes (t_3c855b9f) debe declarar qué tokens consume.

## 9. Tabla de contraste verificada (script WCAG, fórmula relative luminance)

Calculada para los valores propuestos (script `contrast_v2.py` / `contrast_v2_fix.py`, scratch; reproducible). Normativa: texto ≥4.5:1 (1.4.3), iconos/componentes ≥3:1 (1.4.11).

**Dark**
| Par | Ratio | Req | |
|---|---|---|---|
| #FFFFFF on accent #1B66E8 | 5.11 | 4.5 | ✓ |
| accent on card / canvas | 3.39 / 3.70 | 3.0 | ✓ |
| accent-ink #98BAF5 on card / input / wash | 8.80 / 7.75 / 7.96 | 4.5 | ✓ |
| t1 #F2F4F8 on card / canvas | 15.71 / 17.17 | 4.5 | ✓ |
| t2 #A9B1BE on card / canvas | 8.01 / 8.75 | 4.5 | ✓ |
| t3 #868FA0 on card / input | 5.31 / 4.68 | 4.5 | ✓ |
| disabled t3 on raised | 4.06 | 3.0 | ✓ |
| ink #0E1116 on done / on pr | 7.94 / 10.63 | 3.0 | ✓ |
| done #2FBF71 / pr #F5B83D on card (texto) | 7.26 / 9.73 | 4.5 | ✓ |
| err #F1545B on card (fill/borde) | 5.07 | 3.0 | ✓ |
| err-ink #F4636A on input / card (texto) | 4.96 / 5.63 | 4.5 | ✓ |

**Light**
| Par | Ratio | Req | |
|---|---|---|---|
| #FFFFFF on accent #1B66E8 | 5.11 | 4.5 | ✓ |
| accent on card / canvas | 5.11 / 4.64 | 3.0 | ✓ |
| accent-ink #1450B8 on card / wash | 7.32 / 6.38 | 4.5 | ✓ |
| t1 #14181F on card / canvas | 17.79 / 16.16 | 4.5 | ✓ |
| t2 #4B5563 on card / canvas | 7.56 / 6.86 | 4.5 | ✓ |
| t3 #657080 on card / input / canvas | 5.02 / 4.68 / 4.56 | 4.5 | ✓ |
| disabled t3 on raised | 4.23 | 3.0 | ✓ |
| ink #0E1116 on done | 7.94 | 3.0 | ✓ |
| done-ink #157A46 on card / wash | 5.38 / 4.71 | 4.5 | ✓ |
| pr-ink #8A5A12 on card / wash | 5.91 / 5.17 | 4.5 | ✓ |
| err-ink #C22B33 on card / input / wash | 5.70 / 5.31 / 4.88 | 4.5 | ✓ |

Dos pares fallaban en el primer cálculo y se corrigieron aquí: err #F1545B como texto sobre input dark (4.47 → err-ink #F4636A) y t3 light sobre input (4.30 → #657080). QA (t_68a74222) debe recalcular esta tabla de forma independiente.

## 10. Bloque de referencia machine-readable (propuesta, NO aplicada)

CSS de referencia para WI-T1/WI-T3/WI-T4 (los valores definitivos viven en DESIGN.md v2 + tokens.json DTCG, no en este documento):

```css
:root /* dark-first (default) */ {
  --hs-canvas:#0E1116; --hs-card:#161B22; --hs-card-border:#262C36;
  --hs-input:#1F2630; --hs-raised:#283040; --hs-scrim:rgba(0,0,0,.55);
  --hs-accent:#1B66E8;            /* fills ONLY */
  --hs-accent-ink:#98BAF5;        /* accent TEXT (color-mix accent 45% + #fff) */
  --hs-accent-wash:#14233F;
  --hs-done:#2FBF71; --hs-on-done:#0E1116; --hs-wash-done:rgba(47,191,113,.16);
  --hs-pr:#F5B83D;   --hs-on-pr:#0E1116;   --hs-wash-pr:rgba(245,184,61,.18);
  --hs-err:#F1545B;  --hs-err-ink:#F4636A; --hs-wash-err:rgba(241,84,91,.14);
  --hs-t1:#F2F4F8; --hs-t2:#A9B1BE; --hs-t3:#868FA0;
}
[data-theme="light"] {
  --hs-canvas:#F2F4F8; --hs-card:#FFFFFF; --hs-card-border:#D9DFE7;
  --hs-input:#F5F7FA; --hs-raised:#E7ECF3; --hs-scrim:rgba(20,24,31,.45);
  --hs-accent:#1B66E8; --hs-accent-ink:#1450B8; --hs-accent-wash:#E8F0FC;
  --hs-done:#2FBF71; --hs-on-done:#0E1116; --hs-done-ink:#157A46; --hs-done-wash:#E3F4EB;
  --hs-pr:#F5B83D;   --hs-on-pr:#0E1116;   --hs-pr-ink:#8A5A12;   --hs-pr-wash:#FBEEDB;
  --hs-err:#F1545B;  --hs-err-ink:#C22B33; --hs-err-wash:#FCE9EA;
  --hs-t1:#14181F; --hs-t2:#4B5563; --hs-t3:#657080;
}
```

## 11. Huecos y siguientes pasos

- **Contraste de washes dark compuestos** (rgba sobre superficies): A los verificó (done 5.53, PR 6.63) con v1; QA debe recalcularlos con los tokens finales.
- **Hexes light = decisión propia validada numéricamente**, sin benchmark de referencia con hexes públicos (Q-§5). Si Wall quiere verlos, el mock `redesign-mock.html` puede renderizar el toggle light antes de implementar (gate de mock antes de implementación, ux-ui-good-apps).
- **Escala hero 76/800** sigue siendo decisión propia (sin captura con escala de WHOOP; Q-#5).
- Esta propuesta **no toca código ni CSS** (git status: solo `docs/design/`). Siguiente: QA (t_68a74222) verifica §9 de forma independiente; t_ba764d7a integra tokens + componentes en la propuesta final; la implementación (WI-T1…WI-T8) es de Pau tras aprobación.
