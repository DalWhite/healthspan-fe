# Matriz de evidencia — rediseño Healthspan (t_45b423e0)

Fecha: 2026-10-23 · Autor: UX (ux-designer) · Estado: insumo para t_38c41b61 (nuevo design system) y sus hijos.

Consolida los tres entregables de investigación + la verificación QA de citas. Solo evidencia ya verificada en el repo; ninguna afirmación nueva sin fuente. **No toca código ni CSS** — cada "decisión propuesta" es una instrucción de diseño para la fase de implementación (Pau).

## Fuentes (ids usados en la columna Fuente)

| id | Entregable | Ubicación |
|---|---|---|
| A | Auditoría design system v1 (t_511ecd88) | `docs/design/audit-design-system-v1-t511ecd88.md` |
| B | Benchmark 6 apps fitness (t_75c7b777) | `docs/design/research-ux-apps-2026.md` (ledger [n] con citas verbatim en §Sources) |
| L | Literatura WCAG/M3/HIG + case studies (t_8a8f583d) | comentario de la tarjeta t_8a8f583d; artefacto `~/.hermes/profiles/ux-designer/cache/scratch/ev/investigacion3.md` (ledger [25]–[54]) |
| Q | Verificación QA de citas DESIGN.md v1 (t_b6020ed5) | `docs/design/benchmark-citations.md` ([F1]–[F11]) |

Convención de cita: cita verbatim de la fuente primaria (con su id de ledger) y/o evidencia `fichero:línea` de la auditoría. Los contrastes citados están calculados en A con la fórmula WCAG relative luminance.

---

## 1 · Paleta

| # | Fuente | Hallazgo | Decisión propuesta | Cita |
|---|---|---|---|---|
| P1 | A-H1 | El CTA primario (`#FFFFFF` sobre `#1E6FFF`) mide **4.40:1** < 4.5:1; es el botón más usado de la app. | Oscurecer el accent a **#1B66E8** (white 5.11:1 ✓; fill sigue ≥3:1 sobre card/canvas, cumple 1.4.11). | WCAG 1.4.3: "The visual presentation of text … has a contrast ratio of at least 4.5:1" [27]; evidencia `app.css:5642-5644` (A). |
| P2 | A-H2 | DESIGN.md v1 usa accent crudo como texto (tabs, títulos): 4.23:1 sobre nav-bg, 3.93:1 sobre card ✗. El código ya resolvió chips/CTA-sec con `--hs-accent-ink` (#9ABEFF, 8.3–10.1:1 ✓) pero la spec no lo documenta. | Regla de color nueva: **accent crudo solo para fills; todo accent-text usa accent-ink**. Documentar accent-ink y washes como tokens. | A-H2; WCAG 1.4.3 [27]; evidencia `app.css:444-446`, `app.css:15-17, 3981, 5655`. |
| P3 | A-H3 | Icono blanco del FAB "Finalizar" sobre `--hs-done` = **2.38:1** < 3:1 (fallo incluso no-textual). | **Ink oscuro sobre fills semánticos** (`#0E1116` sobre `#2FBF71` = 7.94:1 ✓). | WCAG 1.4.11 (componentes e iconos ≥3:1) [39]; evidencia `app.css:464-468` + `MainLayout.razor:88-98` (A). |
| P4 | A-M2 | Tertiary `#78818F` como texto real de 12.5px: 4.39:1 sobre card, 3.87:1 sobre input ✗. | Ajustar token tertiary a **#868FA0** (5.31 card / 4.68 input ✓). | A-M2; evidencia `app.css:518-522` (columna "Anterior" del set table). |
| P5 | A-M6 | `PaletteLight` hereda los valores dark (hack documentado); si llega el toggle claro, todo componente que lea la paleta light hereda superficies oscuras. | **Reconstruir la paleta light desde 0** como ladder espejo con el mismo accent; dark-first se mantiene como decisión. | Evidencia `HealthspanTheme.cs:30-33` (A); M3: un color scheme cubre roles "across light and dark themes" — el modo oscuro es parte del sistema de color, no un tema aparte [43]. |
| P6 | Q-§3 · B-§10 | Los tokens v1 coinciden 1:1 con un pack "reverse-engineered" de terceros; `#1E6FFF` no existe en ningún asset de Hevy (accent real medido: #267EE8). | **Declarar la paleta v1 como decisión propia de Healthspan** y eliminar toda atribución a Hevy en la nueva spec. | Q: "ese pack **no es fuente fiable** para tokens; usar fuentes primarias"; B: "#1E6FFF no es el azul de Hevy … mantener la paleta v1 como decisión propia". |
| P7 | B-§2 (WHOOP) | Color asignado a conceptos con consistencia absoluta en toda la app. | **Color semántico fijo por concepto** (done/PR/readiness/accent) en todos los componentes del design system; un concepto = un color, siempre. | "Yellow always means Recovery. Blue always means Strain. … Color is used with absolute consistency across the entire app, no legend ever needed." [6] |
| P8 | Q-#8 · B-§10 | La rampa de readiness de DESIGN.md es el mismo semáforo que WHOOP pero con hexes propios; los hexes oficiales (#16EC06/#FF0026) son agresivos sobre fondo oscuro. | Mantener los hexes propios **#2FBF71/#F5B83D/#F1545B**, declarados como adaptación propia con la rampa oficial como referencia conceptual. | WHOOP oficial: High #16EC06, Medium #FFDE00, Low #FF0026 [F5]; Q-§4.5. |
| P9 | A-veredicto · Q-#9 | El ladder de superficies sin sombras de v1 pasa AA (washes: done 5.53, PR 6.63, accent-ink 7.84) y es coherente con la plataforma. | **Mantener** ladder de superficies sin sombras; reformular "hairline borders" como **"profundidad por luminosidad + borde 1px"**. | Apple HIG Dark Mode: fondos *base* y *elevated* "to enhance the perception of depth when one dark interface is layered above another" [F7]; A "Lo que se sostiene". |
| P10 | Q-#13 | Ninguna app de referencia es dark-only (Hevy claro por defecto 71–85 % píxeles claros; Apple alterna); WHOOP sí es dark-native (72–94 % oscuro). | **Dark-first como decisión de producto declarada** (apoyada en WHOOP + HIG dark mode), no como hallazgo de benchmark. | Q: "La regla es defendible solo apoyada en WHOOP + HIG dark mode, y debe presentarse como decisión de producto." |

## 2 · Tipografía

| # | Fuente | Hallazgo | Decisión propuesta | Cita |
|---|---|---|---|---|
| T1 | A-veredicto · Q-#4 | Inter + tabular-nums global + hero/micro-labels fieles a spec, verificado en código; la atribución "tabular-nums = Hevy" no es verificable. | **Mantener** Inter y tabular-nums; re-atribuir la práctica a **Inter/OpenType**, no a Hevy. | Inter (rsms): "tabular numbers" como OpenType feature [F4]; evidencia `app.css:88-90`, `index.html:10-12` (A). |
| T2 | L-§2/§3/§4 | M3 define una escala única tokenizada (30 estilos); HIG define text styles de sistema con AX1–AX5; Android 14 escala fuentes hasta 200 %. | **Escala tipográfica tokenizada** (cada paso con tamaño/line-height/tracking/peso como token) y layout que **aguante 200 % de amplificación** (resize text). | M3: escala "15 baseline + 15 emphasized … cada uno con token propio" [41]; HIG: text styles "ensure support for Dynamic Type and larger accessibility type sizes" [47]; WCAG 1.4.4: texto ampliable al 200 % sin pérdida [40]; Android 14 font scaling [46]. |
| T3 | Q-#5 | WHOOP da tratamiento tipográfico propio a los números (DINPro) frente a las palabras (Proxima Nova); cifras hero en superficies oscuras. | **Números hero con tratamiento propio** dentro de la escala tokenizada (el paso hero 4.75rem/800 de v1 se sostiene como patrón, con la atribución corregida). | WHOOP guidelines: "Prefer to use **DINPro for numbers**" [F5]; evidencia hero `app.css:476-482` (A). |
| T4 | A-M2 | El paso tertiary de la escala actual falla AA como texto (ver P4). | El ajuste de P4 se aplica **como token de la escala**, no como hexo local. | A-M2. |

## 3 · Espaciado / densidad

| # | Fuente | Hallazgo | Decisión propuesta | Cita |
|---|---|---|---|---|
| E1 | A-M3 | 29 `@media` con ≥10 breakpoints ad-hoc (380…1000) frente al único breakpoint de shell de la spec (1000px); riesgo de estados rotos en móvil. | **Reconstruir la escalera como ladder de 3 escalones** (p. ej. 480 / 768 / 1000) con tokens de layout. | A-M3; ux-ui-good-apps §8 (consistencia DLS); popular-web-designs: "todos los sistemas de referencia usan 2-3 escalones" (A). |
| E2 | L-§2/§3 | Material: targets 48dp "con ≥8 dp de separación"; HIG: "el espaciado importa tanto como el tamaño" (44×44 default, mínimo 28×28). | **Separación explícita entre targets** en listas densas (sets/reps, steppers): el gap es parte del token de componente, no un resto de layout. | Android: "a width and height of at least 48dp" [28]; HIG sizes of controls [48]. |
| E3 | A-L2 | `.hs-chip` min-height 44px aplica también a badges estáticos (PR badge) → infla densidad. | **Separar chip-interactivo (≥44px target) de badge-estático** (tamaño por contenido). | Evidencia `app.css:3959` (A). |
| E4 | B-§1 (Strava) | En contexto de riesgo (ciclismo), objetivos pequeños obligan a gestos de precisión: "must tap the small icon in the bottom right". | Las pantallas de registro **en movimiento** (gym, outdoor) usan los targets más grandes de la escalera, nunca el suelo normativo. | "Buttons (placement, padding, and size), flow, and color scheme are the top areas of concern for cyclists." [3] |

## 4 · Componentes

| # | Fuente | Hallazgo | Decisión propuesta | Cita |
|---|---|---|---|---|
| C1 | A-M1 | 12 hexos hardcodeados en 6 componentes razor/chart saltan el token contract; cambiar el accent no los propaga. | **Token contract estricto**: todo color en componentes (incluidos SVG/charts) sale de `var(--hs-*)`/constantes del theme; extender el test de regresión a los hexos razor. | ux-ui-good-apps §8 (Airbnb DLS "Unified": design tokens, no one-off components); evidencia `Biomarkers.razor:535-536` etc., `SharedVisualFoundationTests.cs:107+` (A). |
| C2 | A-M4 | Triple fuente de verdad (`:root --hs-*` + alias `--mud-palette-*` + constantes C#); un cambio de accent exige tocar 3 sitios. | **Spec de tokens como fuente única** que genere/alimente CSS y C# (una sola edición → toda la app). | design-md (tokens single-source, `{colors.primary}`); evidencia `app.css:2-35`, `HealthspanTheme.cs:13-24` (A). |
| C3 | A-H4 | La spec prescribe un CTA secundario (accent-soft bg + accent text) que mide 3.56:1 ✗; el código ya lo corrigió con accent-ink (8.33:1 ✓). | **La spec es el documento defectuoso**: reconstruirla documentando accent-ink y washes como tokens obligatorios para CTA-sec/chips. | A-H4; evidencia `app.css:5653-5656`. |
| C4 | B-§3 (Hevy Trainer) | Update oficial de producto con doble persona (trainer) bajo "mismos momentos que importan", validación de mercado del patrón host/guest. | **trainer↔trainee con los mismos componentes y vistas distintas** (permisos distintos, sin segundo flujo de componentes). | "UX polish … logging mid-workout, navigating exercises, celebrating your monthly achievements … add value without making your workout more complicated." [15] |
| C5 | B-§3 (Hevy) | El rest timer automático es parte del flujo de log (en vivo y al construir rutina), no un extra. | **Rest timer como default silencioso** en la sesión de gym (configurable por ejercicio, heredado a la rutina). | "Hevy offers a customizable and automatic workout rest timer to monitor your rest between sets and exercises." [12] |
| C6 | A-veredicto · B-§2 (WHOOP) | El FAB de Healthspan es acción directa (no ruta duplicada) y WHOOP movió sus funciones más populares a la bottom nav por feedback de miembros. | **Mantener bottom nav de destinos únicos + FAB de acción + avatar=cuenta** en el nuevo sistema. | "…put them all in one easy-to-place place that's just a single click away: The all-new navigation bar…" [8]; evidencia `MainLayout.razor:79-106, 189` (A). |
| C7 | A-L1 · A-L3 | Spec internamente contradictoria (sombras vs glow del FAB) y estado disabled sin token definido. | Definir en la nueva spec: **glow ≠ elevación** (o eliminarlo) y **token disabled** explícito (t3 sobre raised). | Evidencia `app.css:462`, `app.css:5637` (A). |

## 5 · Accesibilidad

| # | Fuente | Hallazgo | Decisión propuesta | Cita |
|---|---|---|---|---|
| X1 | L-§1/§4 | Suelo normativo 24px (WCAG 2.5.8) pero convergencia M3↔HIG en 44–48 dp/pt; contexto gym (guantes, sudor) penaliza objetivos pequeños. | **Target ≥44 dp/pt como objetivo de diseño** (48dp en listas densas); 24px solo como suelo normativo nunca como diseño. | HIG: "a button needs a hit region of at least 44x44 pt" [30]; Android 48dp [28]; WCAG 2.5.8 ≥24×24 CSS px [25]. |
| X2 | L-§1/§2 | Contraste AA exigido en ambos temas: ≥4.5:1 texto, ≥3:1 iconos/componentes; Material lo repite idéntico. | Los fixes P1–P4 se adoptan como **requisito de aceptación del nuevo design system** (lint de contraste en la spec de tokens). | WCAG 1.4.3 [27] + 1.4.11 [39]; Material: "texto grande/graphics ≥3:1, texto pequeño ≥4.5:1" [45]. |
| X3 | L-§5 (iMHere) | Rediseño centrado en accesibilidad midió −35 % tiempo de tarea (48s→31s), −82.8 % errores, usabilidad 83 %→92 %, 79 % de preferencia. | El **flujo de registro de un paso, mínimo esfuerzo** es requisito de diseño (no feature): la fricción de entrada es causa medida de error y abandono. | "no errors occurred with use of the redesigned app" [50]; time −35 %, errors −82.8 % [49]. |
| X4 | B-§6 (Apple) | Apple lista cinco funciones visuales de accesibilidad y Fitness+ sigue imperfecta para ciegos/baja visión. | **Nunca color como única señal** (color+icon+text en errores y semánticos); soportar texto grande y reduce motion. | "Sufficient Contrast, Dark Interface, Larger Text, Differentiate Without Color Alone, and Reduce Motion." [23] |
| X5 | A-veredicto | `prefers-reduced-motion` y `focus-visible` global ya implementados y funcionando. | **Mantener** y llevarlos a la nueva spec como requisitos, no como suerte de implementación. | Evidencia `app.css:105`, `app.css:92-96` (A). |
| X6 | L-§1 | Resize text 200 % sin pérdida de contenido/funcionalidad (WCAG 1.4.4). | Cada plantilla de pantalla del nuevo sistema debe **validarse a 200 % de fuente** (incluida la tabla de sets y la bottom nav). | "texto ampliable al 200 % sin pérdida de contenido/funcionalidad" [40]. |

## 6 · Patrones de producto

| # | Fuente | Hallazgo | Decisión propuesta | Cita |
|---|---|---|---|---|
| M1 | B-§8.1 | Strava, Hevy y TrainingPeaks monetizan sobre un log impecable; social y análisis son capas. | **El log es el producto**: proteger la activación "registrar el entreno de hoy en un gesto" como núcleo intocable del rediseño. | "A workout tracking app that just works." [13]; TrainingPeaks free tier: poblar el calendario antes de pagar [19]. |
| M2 | B-§8.2 | El feed social de Strava existe como subproducto del log; en Hevy lo social es desactivable y su ausencia es motivo explícito de fuga. | **Social opcional y desactivable desde el día 1**; guardar un entreno publica el item en el mismo acto (log y social son el mismo evento). | "items appear without anyone composing them" [1]; "I don't really use the social features…" [37]; "Saving it publishes an item to the home feed" [2]. |
| M3 | B-§8.3 | El streak de Strava cuenta semanas (ventana de 7 días), no días. | Streaks solo cuando el log diario sea hábito; entonces **ventana semanal tolerante** (gracia), no racha diaria punitiva. | "Strava counts its streak in weeks rather than days… logging an activity inside a 7-day window." [2] |
| M4 | B-§2 (WHOOP) | La calibración mostrada "in 7 different ways without clear expectations or a timeline" hizo que usuarios creyeran que la app estaba "broken". | Todo dato derivado (RM, volumen, carga recomendada) se presenta con **línea de progreso única y timeline honesto** desde el onboarding. | "Calibration is shown in 7 different ways without clear expectations or a timeline, leaving new users confused about their progress." [9]; trial Fitbod con timeline explícito y sin tarjeta [17]. |
| M5 | B-§8.4 | WHOOP (triple ring) y Apple (three rings) resuelven el dashboard como una métrica hero leíble de un vistazo. | **Un hero por pantalla** (anillo de sesión/serie de hoy) con color semántico constante (P7); el análisis detallado debajo del hero. | "Users read their entire body status in one look without parsing three separate charts." [6]; Apple ADA 2026: "a beautiful data visualization that shows training load alongside metrics…" [22]. |
| M6 | B-§8.6 | El paywall de Strava sobre análisis no percibido genera rechazo visible incluso en usuarios fieles; TrainingPeaks cobra sobre análisis manteniendo el log gratis. | **Paywall futuro solo sobre análisis avanzado/coaching**, nunca sobre el acto de registrar ni el historial propio. | "Placement on a segment's leaderboard will remain free… analysing them in detail will now be available to subscribers only" [35]; "the 'trial' pop-ups are getting aggressive" [36]. |
| M7 | B-§4 (Fitbod) | La recomendación llega como workout propuesto por algoritmo que el usuario usa, no como wizard de formularios. | Cualquier sugerencia (peso/series) es **propuesta editable que el usuario confirma**, nunca un formulario previo obligatorio. | "fully customized workouts based on your goals, fitness level, and available equipment" [16]. |
| M8 | L-§5/§6 | Retención mHealth: benchmark ~7 % a 30 días (80 % participación mínima, 3.9 % >15 días); co-diseño con usuarios/clínicos alcanzó 45.8 %; push diarios mejoran adherencia. | **Notificaciones/recordatorios de bajo coste como palanca de adherencia** en el diseño (no añadidos después); el proceso de co-diseño se trata como parte del requisito, no como fase opcional. | "only 3.9% usa las apps >15 días; push notifications diarias afectan positivamente la adherencia" [54]; "retención a 30 días 45.8 % vs benchmark típico 7 %" [51]. |
| M9 | B-§3 (Hevy Journal→WHOOP) | WHOOP Journal limita deliberadamente a "10 or fewer" preguntas diarias porque "consistent answers are what unlock insights". | Registro de entrenamiento como **lista corta predecible con prefill de la última sesión** y corrección tardía fácil (≤14 días); el análisis llega después de datos consistentes. | "Track 10 or fewer at a time. A short list is easier to answer every day, and consistent answers are what unlock insights." [7] |

---

## Decisiones cruzadas (los tres entregables convergen)

1. **La spec, no el código, es el documento defectuoso** (A-H2/H4): el código ya corrigió los contrastes que la spec prescribe mal → el nuevo design system se escribe desde la spec machine-readable (design-md compliant, tokens + lint de contraste, A-M5).
2. **Los 7 requisitos no negociables de L-§6 son el contrato de aceptación** del nuevo design system; las filas X1–X6 y P1–P4 son su traducción a decisiones concretas de Healthspan.
3. **Procedencia de tokens corregida** (Q-§3): la paleta se mantiene pero se re-declara como propia; toda atribución a Hevy en DESIGN.md v1 se elimina en la nueva spec.
4. **Dark-first se sostiene como decisión de producto** (Q-#10, P10) apoyada en WHOOP + HIG dark mode, con la paleta light real como deuda a pagar (P5).

## Huecos abiertos (arrastrados de las fuentes)

- Contraste real de `#A9B1BE`/`#78818F` sobre `#0E1116` verificado solo por cálculo propio (A); Q-§5 lo marcaba pendiente — la auditoría lo cerró con cálculo WCAG (P4).
- Capturas propias de Hevy/Fitbod/TrainingPeaks con tema por defecto en instalación limpia: sin ellas, "tema por defecto" queda inferido de marketing (Q-§5).
- Escala de cifra hero de WHOOP sin captura con escala: la escala 76/800 de v1 queda como decisión propia, no benchmark (Q-§5, T3).
- El estudio Chalmers es prototipo de investigación, no WHOOP Coach de producción (B-§Cobertura).
