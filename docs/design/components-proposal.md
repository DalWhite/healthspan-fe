# Catálogo de componentes base con estados — propuesta (t_3c855b9f)

Fecha: 2026-10-23 · Autor: UX (ux-designer) · Estado: **propuesta — no toca código ni CSS** (límite del body).
Consumidores: t_68a74222 (QA accesibilidad, revisará este fichero junto a `tokens-proposal.md`), t_ba764d7a (propuesta final), t_38c41b61 (design system raíz).

**Fuentes:** matriz de evidencia `docs/design/evidence-matrix.md` (filas citadas como P1…M9), auditoría `docs/design/audit-design-system-v1-t511ecd88.md` (H1–H4, M1–M6, L1–L3), DESIGN.md v1 (`docs/design/DESIGN.md`), implementación actual `wwwroot/css/app.css` (evidencia `fichero:línea`, usada solo como referencia, sin modificarla).

**Dependencia de tokens:** los nombres de token son los actuales `--hs-*` (`app.css:2-81`). Los **valores hex son candidatos** pendientes de la propuesta de tokens (t_0098e568, en curso): accent `#1E6FFF→#1B66E8` (P1), tertiary `#78818F→#868FA0` (P4), ink sobre fills semánticos `#0E1116` (P3). Si tokens-proposal cambia un valor, este catálogo se mantiene válido porque todo componente referencia tokens, nunca hexos (C1/C2).

---

## 0 · Reglas transversales (aplican a los 7 componentes)

| Regla | Decisión | Por qué (outcome) |
|---|---|---|
| Token contract estricto | Todo color/spacing/radio de un componente sale de `var(--hs-*)`; prohibido hex literal en razor/SVG (C1). | Cambiar el accent propaga a toda la app sin tocar componentes → consistencia y coste de rediseño O(1). |
| accent-fill vs accent-ink | Accent crudo **solo como relleno o gráfico (borde/icono ≥3:1)**; todo accent como **texto** usa `--hs-accent-ink` (P2, C3). | Texto accent sobre card/nav fallaba AA (3.93–4.23:1); accent-ink (8.3–10.1:1) hace legibles nombres de ejercicio, tabs y chips seleccionados. |
| Ink sobre fills semánticos | Icono/texto sobre `--hs-done`/`--hs-pr` es `#0E1116`, nunca blanco (P3). | El check blanco sobre verde medía 2.38:1 (<3:1, 1.4.11); ink oscuro = 7.94:1 → el estado "hecho" se ve incluso con baja visión. |
| Vocabulario de estados | Los 8 estados del body son el vocabulario canónico; cada componente declara cuáles le aplican y cuáles son N/A explícitos (§8 matriz). | Un estado "no definido" es donde aparecen los bugs de producción (disabled sin token, C7). |
| Focus visible | Todos los componentes interactivos conservan `:focus-visible` global (`app.css:92-96`) y lo elevan con `--hs-shadow-focus` cuando están sobre superficie oscura (X5). | Navegación por teclado visible sin depender del hover. |
| Hover solo con puntero fino | Los estados hover se definen pero se publican bajo `@media (pointer: fine)`; en táctil no existe hover persistente. | Evita el "chip seleccionado fantasma" tras un tap en móvil. |
| Motion | Toda animación de entrada/pressed/timer respeta `prefers-reduced-motion` (`app.css:105`): fallback = cambio instantáneo o fade corto (X5). | Usuarios con vestibular sensitivity no pierden la función. |
| Nunca color solo | Todo estado semántico (done/PR/error/readiness) lleva icono y/o texto, no solo wash (X4, P7). | Daltonismo y baja visión siguen leyendo el estado; un concepto = un color en toda la app. |
| Escalera responsive | 3 escalones: **<480 · 480–999 · ≥1000** (E1; reemplaza los 29 `@media` ad-hoc). El shell cambia de nav a 1000px. | Menos estados de layout posibles → menos pantallas rotas en móvil, que es el objetivo primario. |
| Targets | ≥44px (`--hs-touch-target`) en todo lo accionable; **48px + gap ≥8px** en listas densas (sets, steppers) (X1, E2). | Gym con guantes/sudor: el error de tap es la fricción medida que mata el log (X3: −82.8 % errores con rediseño de un paso). |
| Font scaling | Cada plantilla se valida al 200 % de fuente sin overflow ni pérdida (X6, WCAG 1.4.4); anchos de celda en `rem`, no px. | Usuarios con baja visión conservan el flujo de registro completo. |

---

## 1 · Card

**Uso:** contenedor universal (Hoy, Historial, Biblioteca, Insights, rutinas). Outcome: la card es la unidad de escaneo — el usuario lee una sesión/ejercicio de un vistazo sin entrar.

### Anatomía
1. Superficie `--hs-card` + borde 1px `--hs-border` + radio `--hs-radius-card` (14) + padding `--hs-space-4` (16). Sin sombra (P9: profundidad por luminosidad).
2. `media` (opcional): imagen full-bleed arriba (Biblioteca: 110px, image-first).
3. `title` (16–17/700; accent-ink si es affordance de edición).
4. `meta` (12.5/500 `--hs-t2`; secundarios `--hs-t3`→#868FA0).
5. `trailing` (opcional): valor (volumen/duración) o chevron.
6. `content` (opcional): set table, dots de intentos, chips de bloque.

### Variantes
- **container** (estática, agrupa contenido) · **interactive** (toda la card es un botón → detalle; Biblioteca) · **selectable** (selección de rutina/día) · **row** (compacta, Historial).

### Estados
| Estado | Visual | Comportamiento / a11y |
|---|---|---|
| default | card + borde | — |
| hover (fine) | borde → accent al 45 % (graphic ≥3:1 ✓ 3.93 sobre card) | pista de interactividad; solo en variantes interactive/selectable |
| pressed | bg → `--hs-raised` | feedback inmediato del tap (gym) |
| selected | wash accent 16 % + borde accent + texto accent-ink | `aria-pressed`/`aria-checked`; mismo patrón que chip seleccionado → consistencia |
| disabled | bg `--hs-raised`, texto `--hs-t3`, sin pointer, borde normal | **token explícito** (reemplaza `opacity .55`, C7): legible y explicable ("bloque bloqueado") |
| loading | skeleton con forma de card (bloques `--hs-raised`, shimmer que se desactiva con reduced-motion) | mantiene la estructura de la pantalla mientras llega el dato → no hay "pantallazo en blanco" |
| error | card con mensaje inline (icono + texto err) y acción de reintento | patrón EmptyState/ErrorState ya existente; nunca snackbar genérico (NN/g) |
| empty | como contenedor: micro-label + una frase de guía + **un** CTA secundario | el vacío enseña el siguiente paso, no un hueco muerto |

### Responsive
1 columna <480 · grid 2 col (Biblioteca) 480–999 · grid ≥3 col o layout horizontal ≥1000. Padding 16 en todos los escalones (densidad constante). A 200 % de fuente la card crece verticalmente; `trailing` pasa debajo del título.

### Accesibilidad
- interactive card = **un solo tab stop** (semántica de botón, `aria-label` con nombre completo + meta); los elementos internos no son focusables individualmente → flujo de teclado rápido (Biblioteca).
- Contrastes verificados sobre card: t1 15.71:1, t2 8.01:1, t3 ajustado 5.31:1 (auditoría).
- Cero chips/botones en card de Biblioteca (estándar "pro" del usuario): 1 tap = detalle → menos decisiones por pantalla.

**Decisiones clave:** sin sombra (P9: la escalera de luminosidad ya separa superficies; las sombras en dark son ruido) · pressed = raised (el feedback de luminosidad es visible en pantalla OLED bajo sol, un scale sutil no).

---

## 2 · CTA

**Uso:** acción principal de cada pantalla (Empezar entreno, Añadir serie, Guardar). Outcome: el botón más usado de la app debe ser el más legible y el más difícil de confundir.

### Anatomía
1. Relleno (fill) + texto 16/700 + radio 12 + padding vertical 15 + min-height 44px.
2. `icon slot` (izquierda, opcional) — se sustituye por spinner en loading.
3. `label` (nunca solo icono).
4. `message slot` (debajo, inline) para error.

### Variantes (única taxonomía; no existe "tercer botón")
- **primary:** fill accent (`#1B66E8` candidato) + texto `#F2F4F8` → 5.11:1 ✓ (P1). Una primary por pantalla.
- **secondary:** fill `--hs-accent-soft` + texto `--hs-accent-ink` → 8.33:1 ✓ (C3: la spec v1 prescribía accent crudo sobre soft = 3.56:1 ✗; la spec nueva adopta lo que el código ya corrigió).
- **danger:** wash err 16 % + texto err + icono (no fill rojo; reserva el rojo sólido para confirmaciones destructivas futuras). Contraste wash→QA (§10).
- **done** (acción Finalizar): fill `--hs-done` + ink `#0E1116` (P3).

### Estados
| Estado | Visual | Comportamiento / a11y |
|---|---|---|
| default | ver variantes | — |
| hover (fine) | fill aclara un escalón (mix 88 % con blanco) | pista; no cambia de forma |
| pressed | fill oscurece un escalón (mix raised) | feedback táctil |
| disabled | bg `--hs-raised` + texto `--hs-t3` + `aria-disabled` | **token explícito** (C7): un botón apagado con opacity sigue siendo ilegible y no explica por qué |
| loading | spinner en icon slot, label intacto, `aria-busy`, input bloqueado | previene doble submit sin borrar el texto del botón |
| error | mensaje inline bajo el botón (icono+texto, específico, input preservado) | el error junto a la acción es la diferencia entre reintentar y abandonar (NN/g) |
| selected / empty | N/A (definido, no aplicable) | — |

### Responsive
<480: full-width (zona del pulgar, un pulgar lo alcanza) · 480–999: full-width en formularios, auto ≥160px en barras · ≥1000: auto-width, min 120px. Nunca dos CTAs primary lado a lado en móvil (una decisión por pantalla).

### Accesibilidad
- Texto ≥4.5:1 en todas las variantes (verificado en auditoría; danger pendiente QA).
- `focus-visible` con outline propio (ya en código, `app.css:5646-5648`) → llevarlo a la spec.
- Label siempre textual; el spinner nunca reemplaza el label.

**Decisiones clave:** disabled por token y no por opacity (C7: el estado existe en la app pero no tiene definición — es el hallazgo L3) · loading conserva el label (el usuario sabe qué acción está esperando, no ve un botón anónimo).

---

## 3 · Chip (y badge estático)

**Uso:** filtros (Biblioteca), selectores (tipo de entreno), etiquetas de bloque (Superserie n/m), contadores. Outcome: el filtrado y la selección se entienden sin explicar la app.

**Split fundacional (E3):** dos componentes distintos con el mismo lenguaje visual:
- **chip-interactivo** (filtro/selector): target ≥44px, estados hover/selected/disabled.
- **badge-estático** (PR badge, chip de bloque, racha): tamaño por contenido (min-height ~24px), sin estados interactivos. Hoy `.hs-chip` infla los badges a 44px (`app.css:3959`) y destruye la densidad de las listas → separarlos recupera densidad sin tocar targets reales.

### Anatomía
1. Fondo wash 16–18 % del color semántico sobre card · 2. borde 1px (color al 45 %) · 3. radio `--hs-radius-chip` (8) · 4. texto 11.5/700 UPPERCASE tracking +0.6 · 5. icono opcional (★ PR, ✓).

### Variantes semánticas (wash ya verificado AA en auditoría)
`accent` (wash 16 %, texto accent-ink 7.84:1) · `done` (wash 16 %, texto done 5.53:1) · `pr` (wash 18 %, texto pr 6.63:1) · `neutral` (card + borde, texto t2).

### Estados (chip-interactivo)
| Estado | Visual | Comportamiento / a11y |
|---|---|---|
| default | neutral | — |
| hover (fine) | borde accent + texto t1 | pista |
| selected | wash accent 16 % + borde accent + texto accent-ink | `aria-pressed=true`; el estado se lee por borde+peso+color (X4) |
| pressed | wash un escalón más subido | feedback tap |
| disabled | texto `--hs-t3`, borde `--hs-border`, sin pointer | filtro no disponible (p. ej. sin datos) explicado por texto de ayuda |
| loading / error / empty | N/A para el chip; el contenedor del grupo de chips sí tiene loading (skeleton de chips) y empty ("Sin resultados — limpiar filtros") | el empty del grupo ofrece la salida (limpiar), no un vacío |

Badge-estático: solo default + variante semántica; PR badge = "★ PR" con texto (nunca solo el color gold).

### Responsive
Wrap libre con gap `--hs-space-2` (8px) — los chips nunca se aprietan para caber en una línea (<480: scroll horizontal solo en filtros de calendario, con snap). A 200 % de fuente el chip crece y hace wrap; el target mínimo se mantiene.

### Accesibilidad
- Chips de filtro como toggle buttons con `aria-pressed`; grupo con `role="group"` + micro-label accesible.
- UPPERCASE solo en chips/badges/labels (regla v1): nunca en texto largo (legibilidad).

**Decisiones clave:** split chip/badge (E3: un badge no es un target; inflarlo a 44px cuesta una fila de altura por tarjeta en Historial) · selected = wash + borde + ink (mismo patrón que card selectable → el usuario aprende "seleccionado" una sola vez).

---

## 4 · Bottom sheet

**Uso:** acciones contextuales sin salir de contexto (crear entreno, sheet de cuenta móvil, editar sesión). Outcome: la tarea secundaria no expulsa al usuario de la pantalla donde estaba (regla "aterrizar en contexto").

### Anatomía
1. `scrim` `rgba(0,0,0,.55)` (token de spec v1).
2. `sheet`: superficie, radio superior 20 (`--hs-radius-hero`), `--hs-shadow-sheet` (única sombra del sistema: un overlay necesita separarse del contenido; ver drift abajo).
3. `handle` 40×4 (decorativo, `aria-hidden`).
4. `title` (micro-label o h2) · 5. `content` (fields con validación inline on-blur) · 6. `actions` row (primary + secondary, 48px) · 7. `safe-area-inset-bottom`.

### Estados
| Estado | Visual | Comportamiento / a11y |
|---|---|---|
| closed | — | foco devuelto al trigger al cerrar |
| open (entrada) | slide-up 240ms spring | reduced-motion → fade 120ms o instantáneo |
| open | foco atrapado dentro; primer field enfocado | `role="dialog" aria-modal="true" aria-labelledby=title` |
| loading (contenido async) | skeleton dentro del sheet (no spinner central) | el sheet no se "congela" vacío |
| error | inline por field (icono+texto, input preservado) + acciones disponibles | un error no bloquea la salida del sheet |
| pressed/disabled/selected/empty | N/A para el sheet (aplican a sus CTAs internos) | — |

### Responsive
<768: full-width inferior, `max-height min(90vh, 620px)`, scroll interno · ≥768: dialog centrado max-width 440px, radio completo, handle oculto (el patrón ya existe en `app.css:1240-1253`; se re-ancla a la ladder de 3 escalones). Cierre: Escape, tap en scrim, botón explícito (el drag-to-dismiss es opcional y nunca la única salida).

### Accesibilidad
- Focus trap + Escape + restauración de foco; título anunciado; scrim no focusable.
- Campos: label arriba del input (nunca placeholder-as-label), validación on-blur, mensaje junto al field (NN/g; ya es estándar del proyecto).
- Sheet de cuenta móvil: contiene TODAS las entradas de cuenta + logout (regla del usuario: el móvil nunca pierde acceso a la cuenta cuando la sidebar desaparece).

**Drift a resolver en la propuesta final:** la spec v1 §3 prescribe sombra para el sheet pero la implementación usa `--hs-shadow-panel` = none (`app.css:1092-1101`) mientras el token `--hs-shadow-sheet` existe sin usarse. Decisión propuesta: **el sheet es la única excepción declarada a "sin sombras"** (overlay sobre contenido, análoga al scrim); el resto del sistema mantiene P9.

---

## 5 · FAB

**Uso:** acción directa global (registrar/quick-start; Finalizar durante sesión). Outcome: el gesto de activación "registrar el entreno de hoy en un toque" está disponible desde cualquier pantalla (M1, C6).

### Anatomía
1. 46×46px, radio 14, fill. 2. Icono 22–24px. 3. Label accesible textual (aria-label "Registrar entreno") — nunca solo "+".
4. Posición: slot central de la bottom nav <1000px, elevado sobre la barra, fuera del flujo de los 4 tabs.

### Variantes
- **action:** fill accent + icono `#F2F4F8` (5.11:1 con accent candidato ✓).
- **done** (Finalizar): fill `--hs-done` + icono ink `#0E1116` (7.94:1 ✓; reemplaza el blanco actual 2.38:1 ✗, P3).

### Estados
| Estado | Visual | Comportamiento / a11y |
|---|---|---|
| default | ver variantes | — |
| hover (fine) | fill aclara un escalón | pista |
| pressed | fill oscurece + scale 0.96 (reduced-motion: sin scale) | feedback |
| disabled | token raised + t3 (p. ej. Finalizar sin sesión activa) | el FAB nunca es un misterio: si no hay acción, se explica |
| loading / error / selected / empty | N/A (la acción es local e instantánea; los errores de guardado viven en la pantalla, no en el FAB) | — |

**Glow (L1/C7): se elimina.** `box-shadow: 0 0 18px rgba(30,111,255,.45)` (`app.css:462`) contradice la regla "sin sombras" de la misma spec. Decisión: profundidad del FAB = fill accent sobre nav oscura + borde 1px, igual que todo lo demás. Tradeoff: se pierde el brillo decorativo de v1; se gana un sistema sin efectos one-off (DLS "Unified") y una spec sin contradicción interna. Si Wall quiere conservarlo, se declara como token único `--hs-fab-glow` de uso exclusivo del FAB — pero la recomendación es quitarlo.

### Responsive
<1000: centrado en bottom nav (slot 3; nunca duplica un destino de tab — regla del usuario) · ≥1000: el FAB **se convierte en un CTA primario persistente al pie del sidebar** ("Registrar entreno") — misma acción, misma ruta, sin botón flotante sobre contenido desktop. Justificación: en desktop el flotante solapa contenido y la sidebar ya tiene el destino; la acción directa debe seguir a un toque de distancia.

### Accesibilidad
- 46px ≥44 ✓; `aria-label` con verbo de acción; fuera del orden de tab de los links de nav pero focusable; no solapa safe-area (por encima de `env(safe-area-inset-bottom)`).

---

## 6 · Timer / log de serie (el corazón del producto)

**Uso:** registro de sets durante la sesión + cuenta de descansos. Outcome: X3 (iMHere: −35 % tiempo, −82.8 % errores con flujo de un paso) y M9 (lista corta predecible con prefill) se juegan aquí; este componente es el más protegido del rediseño.

### 6.1 Set row — anatomía (tabla real, `table-layout: fixed`, ya corregido el overflow a 390px en `app.css:493-499`)
1. `Serie` (índice, micro-label column) · 2. `Anterior` (valor de la última sesión, tertiary #868FA0, 12.5 — fuente del prefill, M9) · 3. `Reps/kg` (celdas input: superficie `--hs-input`, radio 8) · 4. `✓` toggle (48px) · 5. `PR badge` inline (badge-estático gold "★ PR") · 6. `error message` inline bajo la fila · 7. gap vertical ≥8px entre targets (E2).

Variantes: **load** (reps+kg) · **bodyweight** (solo reps) · **timed** (segundos) · **intentos** (dots 3/8: ✗ fallido · gold PR · relleno completado — el dot lleva icono, no solo color).

### 6.2 Estados de la fila
| Estado | Visual | Comportamiento / a11y |
|---|---|---|
| default | fila editable; Anterior tertiary | prefill ya cargado: el usuario corrige, no escribe desde cero (M9) |
| editing | input con `--hs-shadow-focus` + borde accent | foco claro en la fila que se está editando |
| done | wash done 10 % + valor texto done + check relleno ink | `aria-pressed=true` + texto "completada" (X4); la fila completada se distingue sin color |
| pressed (toggle) | check → raised | feedback |
| selected | N/A (las filas no se seleccionan) | — |
| disabled | **read-only**: texto t1 plano, sin inputs, sin wash | doble persona C4: trainer↔trainee usan la MISMA fila con permisos distintos (trainee ve la prescripción bloqueada); no existe un segundo componente |
| loading | 3 filas skeleton | la estructura de la tabla aparece antes que los datos |
| error | mensaje inline bajo la fila ("El peso debe ser un número entre 0 y 300 kg"), input preservado, borde err en la celda | específico, on-blur, sin culpar (NN/g); reemplaza el snackbar genérico |
| empty | ejercicio sin series → un ghost CTA "Añadir serie" (48px), nunca una tabla vacía con 4 headers | el vacío enseña la acción, no la burocracia |

### 6.3 Rest timer (misma familia, patrón silencioso — C5)
- **Anatomía:** pill de countdown (número tabular-nums, texto accent-ink) + controles pause/skip (48px) + micro-label "DESCANSO".
- **Estados:** `idle` (oculto) → `running` (arranca solo al marcar ✓ serie; default silencioso, configurable por ejercicio y heredado a la rutina — Hevy [12]) → `paused` (bg raised + icono pause) → `finished` (wash done 16 % + texto "Descanso terminado" + auto-dismiss 3s + anuncio `aria-live="polite"`).
- Los ticks **nunca** se anuncian a lectores de pantalla (aria-live off en el número); solo el fin.
- reduced-motion: sin pulso; el número simplemente cambia.

### Responsive
- <480: tabla fixed con anchos en `rem` (sobrevive 200 % de fuente, X6); "Anterior" baja a 11.5px pero mantiene ≥4.5:1 con #868FA0 (4.68 sobre input ✓).
- 480–999: igual, con más aire horizontal. ≥1000: la tabla gana columnas de gráfico opcional (no en este sprint).
- Targets de ✓ y steppers: 48px con gap 8px en todos los escalones (E2/X1; contexto gym E4: los targets más grandes de la escalera, nunca el suelo normativo).

### Accesibilidad
- Semántica de tabla real (`th scope="col"`), nombre accesible de cada input = "Repeticiones para serie 3 de Sentadilla" (contexto, no "input 3").
- Toggle con `aria-pressed`; fila done anunciada (`aria-live` en el cambio, no en la carga).
- PR: texto "★ PR personal" (no solo gold). Fallback de fila a 200 % de fuente: el input reps/kg se apila en dos líneas, la tabla no desborda.

**Decisiones clave:** "Anterior" como columna de primera clase (el prefill es el gesto de velocidad: teclear 0 dígitos vs 4) · timer default silencioso (un timer que hay que configurar es un timer que nadie usa; C5 valida que el descanso automático es parte del log, no un extra).

---

## 7 · Hero metrics

**Uso:** un número por pantalla que se lee de un vistazo (readiness 76, duración, volumen semanal). Outcome: M5 (WHOOP/Apple) — el usuario interpreta su estado sin parsear 3 gráficos.

**Regla dura: un solo hero por pantalla** (M5); el análisis vive debajo.

### Anatomía
1. `value`: paso hero 4.75rem/800 tracking −0.045em, tabular-nums (T1/T3: los números tienen tratamiento propio).
2. `micro-label`: 11/700 UPPERCASE tracking +1 (label del hero, único junto a columnas y chips).
3. `delta` (opcional): 12.5/500 t2 ("+2 vs semana pasada").
4. `status word` (cuando el valor usa rampa semántica): chip-estático "LISTO / MODERADO / BAJO".

Variantes: **hero** (1 por pantalla) · **tile** (24/800 dentro de card, para 2–4 métricas secundarias) · **inline stat** (Historial, valor trailing).

### Estados
| Estado | Visual | Comportamiento / a11y |
|---|---|---|
| default | valor t1 (o rampa: done/pr/err según concepto, P7) | el color del valor es fijo por concepto, nunca decorativo |
| loading | skeleton con forma del número (bloque raised del ancho del hero), label visible | la pantalla no "salta" cuando llega el dato |
| empty | label + frase de guía ("Aún no hay sesiones — Registra tu primer entreno" + CTA secondary), **nunca un 0 desnudo** | un 0 sin contexto se lee como dato malo, no como falta de datos |
| error | valor sustituido por mensaje + botón "Reintentar" (secondary) | el fallo de red no se disfraza de métrica |
| hover / pressed / selected / disabled | N/A (el hero no es interactivo; si se toca, es una card, no un hero) | — |

### Responsive
- `clamp(3rem, 12vw, 4.75rem)`: encaja en 360px y escala en desktop sin breakpoint extra.
- Tiles: 2 col <480 · 3–4 col ≥480. A 200 % de fuente el hero crece y empuja el contenido (scroll), nunca se recorta (X6).

### Accesibilidad
- Valor + label agrupados (`role="group"` o aria-label "Readiness 76, listo") para que un lector de pantalla reciba la métrica con su significado.
- Rampa readiness **siempre acompañada de status word** (X4: "Differentiate Without Color Alone" — Apple [23]).
- Animación count-up solo si `prefers-reduced-motion: no-preference`; tabular-nums evita el reflow del número al actualizar (T1).

**Decisiones clave:** empty ≠ 0 (un 0 engañoso es el "app broken" de WHOOP en miniatura, M4) · un hero por pantalla (dos números hero compiten y ninguno se lee primero, M5).

---

## 8 · Matriz componente × estado

| Componente | default | hover | pressed | disabled | loading | error | selected | empty |
|---|---|---|---|---|---|---|---|---|
| Card | ✓ | ✓* | ✓ | ✓ token | ✓ skeleton | ✓ inline | ✓ (selectable) | ✓ (contenedor) |
| CTA | ✓ | ✓* | ✓ | ✓ token | ✓ busy | ✓ inline | — | — |
| Chip interactivo | ✓ | ✓* | ✓ | ✓ token | — (grupo) | — (grupo) | ✓ | — (grupo) |
| Badge estático | ✓ | — | — | — | — | — | — | — |
| Bottom sheet | ✓ open | — | — | — | ✓ interno | ✓ inline | — | — |
| FAB | ✓ | ✓* | ✓ | ✓ token | — | — | — | — |
| Set row | ✓ | — | ✓ (toggle) | ✓ read-only | ✓ skeleton | ✓ inline | — | ✓ |
| Rest timer | ✓ running | ✓* | ✓ | — | — | — | — | — |
| Hero metric | ✓ | — | — | — | ✓ skeleton | ✓ + retry | — | ✓ guía |

\* hover solo bajo `@media (pointer: fine)`.

---

## 9 · Diff vs DESIGN.md v1 (qué cambia y por qué)

| # | v1 | Propuesta | Evidencia |
|---|---|---|---|
| 1 | CTA primary white sobre `#1E6FFF` (4.40:1 ✗) | accent `#1B66E8` (5.11:1 ✓) | P1 / A-H1 |
| 2 | CTA secondary "accent-soft + accent text" (3.56:1 ✗) | accent-soft + **accent-ink** (8.33:1 ✓) — documentar el token que el código ya usa | C3 / A-H4 |
| 3 | disabled = `opacity .55` sin token | token explícito raised + t3 + `aria-disabled` | C7 / A-L3 |
| 4 | `.hs-chip` único (badges inflados a 44px) | split chip-interactivo (≥44) vs badge-estático (por contenido) | E3 / A-L2 |
| 5 | FAB done con icono blanco (2.38:1 ✗) | ink `#0E1116` sobre fills semánticos (7.94:1) | P3 / A-H3 |
| 6 | FAB glow contradice "sin sombras" | glow eliminado (o token único declarado); profundidad = fill + borde | C7 / A-L1 |
| 7 | Sheet sombra prescrita pero shadow-panel=none en código | sheet = única sombra declarada del sistema (`--hs-shadow-sheet`) | drift verificado `app.css:1092-1101` vs spec §3 |
| 8 | Tertiary `#78818F` en "Anterior" (4.39/3.87:1 ✗) | `#868FA0` (5.31/4.68 ✓) como token de escala | P4 / A-M2 |
| 9 | Estados loading/empty/error solo como regla general (§6) | definidos por componente (skeletons con forma, empty con guía, error inline) | DESIGN.md §6 + matriz X6 |
| 10 | Breakpoints ad-hoc (29 `@media`) | ladder 480/768/1000 aplicada a cada componente | E1 / A-M3 |
| 11 | Timer no especificado como componente | rest timer silencioso con estados idle/running/paused/finished | C5 (Hevy [12]) |
| 12 | Fila read-only (doble persona) no definida | variante disabled=read-only de la misma fila | C4 (Hevy Trainer [15]) |

Se **mantiene** de v1 (verificado en auditoría): ladder de superficies sin sombras, accent único + washes AA, Inter + tabular-nums global, radio/padding de card, bottom nav + FAB de acción sin ruta duplicada, focus-visible + reduced-motion.

---

## 10 · Huecos abiertos para QA (t_68a74222) y propuesta final (t_ba764d7a)

1. **CTA danger wash:** contraste texto err sobre wash err 16 % sin calcular — QA debe medirlo (done/pr ya verificados).
2. **accent-ink con accent nuevo:** `--hs-accent-ink` es `color-mix(accent 45 %, white)` (`app.css:13`); al migrar accent a #1B66E8 el mix cambia — recalcular todos los usos texto (chips selected, CTA-sec, nombres de ejercicio).
3. **Set table a 200 % de fuente:** validar el fallback de apilamiento reps/kg sin overflow en 360px (X6).
4. **Focus trap del sheet en MudBlazor:** el sheet de cuenta usa popover de Mud (`app.css:209-231`); verificar Escape/restauración de foco reales.
5. **Hover bajo `pointer: fine`:** confirmar que ningún estado hover actual queda visible sticky tras tap en móvil.
6. **FAB en desktop:** la conversión a CTA de sidebar es decisión nueva de esta propuesta — necesita visto bueno de Wall antes de la propuesta final.
7. **Glow del FAB:** eliminado por recomendación; si Wall lo quiere conservar, se declara token único (decisión de producto, no técnica).

Ningún componente ni CSS existente fue modificado en esta tarjeta (verificable: `git status` solo añade `docs/design/components-proposal.md`).
