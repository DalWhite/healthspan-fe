# Benchmark fitness — informe final con citas (referencia para el rediseño Healthspan)

Fecha: 2026-10-23 · Autor: UX (ux-designer) · Tarjeta: t_d48ac88d
Estado: **definitivo** — aprobado por QA (t_32f4fbb4) con las 6 correcciones de fidelidad de citas aplicadas (t_5758d57c).
Apps: Strava, WHOOP, Hevy, Fitbod, TrainingPeaks, Apple Fitness (+ Pump Pal como referencia secundaria).
Dimensiones: IA/asistentes, navegación, onboarding, logging, dark mode, tipografía, accesibilidad, monetización UX.

Ledgers de citas (evidencia verbatim adjunta en cada fuente):
- `[n]` → `docs/design/research-ux-apps-2026.md` (§Sources) — benchmark t_75c7b777
- `[Fn]` → `docs/design/benchmark-citations.md` (§6) — verificación QA t_b6020ed5

Documentos de trabajo consolidados aquí (quedan como borradores con puntero a este informe): `benchmark-fichas-tabla-t188bcde0.md` (fichas, tabla, matriz, transversales) y `decisiones-adopcion-evitacion-t_ac4efc0d.md` (decisiones). Este informe no introduce afirmaciones nuevas: reorganiza la evidencia ya citada y enlaza cada hallazgo y cada decisión a su cita.


## 0. Cómo leer este documento

- Cada hallazgo lleva la cita de la fuente que lo sostiene (`[n]` = ledger del informe de benchmark; `[Fn]` = ledger de verificación QA). El índice de citas (§13) mapea cada id usado a su fuente y URL; la evidencia verbatim está en los dos ledgers.
- **GAP** = dimensión sin evidencia verificable en los dos ledgers. No se afirma nada en esas celdas; queda como evidencia pendiente (§11).
- **⚠ refutado** = afirmación que circulaba en DESIGN.md v1 y QA la contradijo; se corrige aquí (§7 y §10).
- Cobertura de evidencia por app y dimensión: §3.
- Decisiones: adoptamos (§5), evitamos (§6), atribuciones eliminadas (§7), aplazadas por GAPs (§8). Ningún GAP fundamenta una decisión.

### 0.1 Resumen ejecutivo

- **Estructura:** 7 fichas (6 apps + Pump Pal secundaria), tabla comparativa de 8 dimensiones × 6 apps y matriz de cobertura §3. La evidencia es desigual: IA/asistentes 2/6, accesibilidad 2/6, tipografía 2/6, dark mode 3/6; Fitbod y TrainingPeaks cubren solo 3/8 cada uno.
- **Adoptar (15 decisiones, §5):** el log como producto con social y análisis como capas; record-first a 1 toque desde la barra inferior; bottom nav fija; una métrica hero con color semántico fijo; logging corto con corrección tardía; rest timer silencioso; social desactivable; streaks semanales tolerantes; timeline honesto para datos derivados y trials; sugerencias editables sin wizard; trainer↔trainee sobre los mismos componentes; paywall solo sobre análisis, nunca sobre registrar; accesibilidad como checklist auditable con targets ≥44–48 px; dark-first como decisión propia apoyada en WHOOP (medido) + Apple HIG; números hero con tabular-nums vía Inter/OpenType.
- **Evitar (10 decisiones, §6):** feed de contenido que el usuario debe alimentar; paywall sobre registrar o sobre análisis poco percibido con pop-ups agresivos; streaks diarios punitivos; datos derivados sin timeline único; wizards previos a una recomendación; segundo flujo para el trainer; targets pequeños en pantallas de registro en movimiento; color como única señal; plan-first sin log sólido; packs de tokens reverse-engineered como fuente.
- **Aplazar (5, §8):** IA conversacional (solo hay estudio con prototipo), accesibilidad de 4/6 apps, tema por defecto de 3 apps, escala de la cifra hero y monetización de WHOOP — todas bloqueadas por GAPs de §3.
- **Correcciones clave de QA (§7, §10):** Hevy no es dark-first; #1E6FFF no es "Hevy Blue" (los tokens v1 proceden de un pack reverse-engineered y son decisión propia); "hairline borders" → profundidad por luminosidad; tabular-nums vía Inter/OpenType; rampa readiness como adaptación propia con la rampa oficial WHOOP como referencia conceptual.


## 1. Fichas por app

### Ficha 1 — Strava

| Campo | Contenido |
|---|---|
| Posición | Log social de actividades; el feed es subproducto del registro, no un producto de contenido [1] |
| IA / asistentes | GAP — sin hallazgo verificado en los ledgers |
| Navegación | Record-first: "Record from the bottom navigation menu" + "Start button near the bottom of the screen" (help oficial 2026-09-10) [F9]. El feed social ocupa la parte inferior del dashboard como un único scroll vertical; usuarios recomendados cabeceran el feed de un usuario nuevo [1] |
| Onboarding | Un usuario que no sigue a nadie aterriza en un feed ya poblado (usuarios recomendados en cabecera) [1] |
| Logging | El contenido social existe sin coste de creación: "items appear without anyone composing them" [1]. Retención por 6 mecánicas: streak, challenges, feed, leaderboards, achievements, grupos; streak en semanas (ventana de 7 días), no días [2]. Guardar una actividad publica el item social en el mismo acto [2] |
| Dark mode | GAP — no medido en QA |
| Tipografía | GAP — sin fuente |
| Accesibilidad | Riesgo documentado en contexto de uso: para ver velocidad/distancia el ciclista "must tap the small icon in the bottom right" — objetivo pequeño en pantalla de riesgo [3] |
| Monetización UX | Paywall sobre análisis ("real-time insights", challenges personalizados) [34]; 2025: posición en leaderboard de segmentos sigue gratis, el análisis detallado pasa a pago; API cortada a apps de terceros para free users [35]. Sentimiento (r/Strava, ene 2026): pop-ups de prueba "getting aggressive" [36] |
| Lección para Healthspan | El log es el producto y el social es capa; streak con barra baja y ventana tolerante; el paywall agresivo sobre funciones poco percibidas erosiona confianza |

### Ficha 2 — WHOOP

| Campo | Contenido |
|---|---|
| Posición | Tracker dark-native de métricas corporales; dark 72–94 % píxeles medido en capturas oficiales [F6] |
| IA / asistentes | Coach conversacional: estudio empírico (n=36, ACM MHCI 2025) — un agente conversacional "can aid in the interpretation of tracker metrics" [11] (prototipo de investigación, no la app de producción). El coach vive en la barra inferior, a un clic del home [8] |
| Navegación | Barra de navegación inferior fija con las funciones más populares "just a single click away" (decisión tomada por feedback de miembros); perfil de rendimiento desde la foto de perfil [8]. Home rediseñada en 2025 a un único scroll + pestaña Health separada [5] |
| Onboarding | Registro + calibración de 7 días con desbloqueo gradual; objetivo "7 day perfect start". Fracaso documentado por su propia diseñadora: "Calibration is shown in 7 different ways without clear expectations or a timeline" → usuarios creían que la app estaba "broken" [9] |
| Logging | Journal: 300+ comportamientos con impacto fisiológico; lista corta de preguntas sí/no cada mañana, "Track 10 or fewer at a time"; 3 entradas al flujo; edición retroactiva hasta 14 días [7] |
| Dark mode | Dark-native: superficies escalonadas medidas #101518, #14191D, #111619, #1F282D, #292D30; gradiente oficial #283339→#101518 [F5][F6]. Única app del benchmark defendible como referencia dark |
| Tipografía | Guidelines oficiales: DINPro para números, Proxima Nova para palabras → números con tratamiento tipográfico propio (hero numbers) [F5]. "All-caps micro-labels": ⚠ solo en descripciones de terceros, no en el PDF oficial [F5] |
| Accesibilidad | GAP — no auditada en QA. Semáforo oficial High/Med/Low #16EC06/#FFDE00/#FF0026 [F5] |
| Monetización UX | GAP en los ledgers (modelo hardware+membership no cubierto por las fuentes citadas) |
| Lección para Healthspan | Una métrica hero leíble de un vistazo; color semántico fijo sin leyenda; si hay datos derivados, timeline de fiabilidad explícito y único |

### Ficha 3 — Hevy

| Campo | Contenido |
|---|---|
| Posición | "A workout tracking app that just works" — logging de fuerza con rutinas, timers y PRs [13]; "Loved by 17+ million athletes", rating 4.9 [13] |
| IA / asistentes | GAP — sin hallazgo verificado |
| Navegación | Renovación nativa anunciada con foco en "logging mid-workout, navigating exercises, celebrating your monthly achievements" [15] |
| Onboarding | Splash oscuro, signup Apple/Google/email con email pre-validado, paywall mensual/anual/lifetime con anual preseleccionado [32]. El splash oscuro es solo la pantalla de arranque, no el tema por defecto [F1] |
| Logging | Rest timer automático, configurable global y por ejercicio, disponible en workout en vivo y al construir rutina [12]. Feed social muestra workouts ajenos (volumen, PRs, FC media) con likes/comentarios; atletas sugeridos desactivables en ajustes de privacidad [31]. Fuga documentada: "I don't really use the social features and I'm not a fan of subscription fitness apps" [37] |
| Dark mode | ⚠ **Refutado "dark-first"**: 6/6 capturas oficiales son UI clara (71–85 % píxeles claros); el tema oscuro es elección del usuario [F1][F2] |
| Tipografía | ⚠ "tabular-nums atribuido a Hevy" no verificable (no publica spec); la práctica es citable vía Inter/OpenType [F4]. Accent real medido #267EE8 (6/6 capturas); single-accent sí sostenido [F1] |
| Accesibilidad | GAP — sin fuente |
| Monetización UX | Paywall en onboarding con 3 planes y anual preseleccionado [32]; usuarios huyen del modelo de suscripción [37] |
| Doble persona | "Trainer" oficial: mismas vistas de log, permisos distintos (inferencia UX sobre [15], no texto de la fuente), filosofía "add value without making your workout more complicated" [15] — valida el patrón trainer↔trainee |
| Lección para Healthspan | Rest timer como default silencioso; social desactivable desde el día 1; las lecciones de Hevy son de flujo, no de tema visual |

### Ficha 4 — Fitbod

| Campo | Contenido |
|---|---|
| Posición | "Fitbod is a personalized strength training app that delivers fully customized workouts based on your goals, fitness level, and available equipment" (algoritmo propietario) [16] |
| IA / asistentes | Recomendación como propuesta, no wizard: la app genera la workout y el usuario la confirma/edita [16] |
| Navegación | GAP — sin fuente |
| Onboarding | Trial de 7 días automático con acceso completo y sin datos de pago; timeline explícito (recordatorio día 5, fin día 7) [17] |
| Logging | GAP — no cubierto en los ledgers |
| Dark mode | GAP — sin medición QA |
| Tipografía | GAP — sin fuente |
| Accesibilidad | GAP — sin fuente |
| Monetización UX | Trial honesto: sin tarjeta y con recordatorios de cierre [17] |
| Lección para Healthspan | Cualquier sugerencia de peso/series debe presentarse como propuesta editable; si hay trial, decir cuándo termina |

### Ficha 5 — TrainingPeaks

| Campo | Contenido |
|---|---|
| Posición | "Comprehensive platform for endurance athletes and coaches": "detailed training log, a powerful analytics engine, and a planning tool all in one" [19] |
| IA / asistentes | GAP — sin hallazgo verificado |
| Navegación | Calendario como superficie principal (modelo plan-first) [19] |
| Onboarding | GAP — sin fuente |
| Logging | El log y el calendario son el valor base; el análisis es la capa superior [19] |
| Dark mode | GAP — sin medición QA |
| Tipografía | GAP — sin fuente |
| Accesibilidad | GAP — sin fuente |
| Monetización UX | Free tier permite poblar el calendario y ver datos antes de pagar funciones avanzadas [19] — contraejemplo moderado de Strava |
| Lección para Healthspan | El pago sobre análisis, no sobre el acto de registrar; no construir rutinas sobre un modelo pobre |

### Ficha 6 — Apple Fitness

| Campo | Contenido |
|---|---|
| Posición | Anillos de actividad como núcleo del movimiento diario [21]; accent saturado #A7FE01 medido en capturas [F8] |
| IA / asistentes | GAP en los ledgers (Fitness+ tiene entrenadores guiados en vídeo [21], no IA conversacional) |
| Navegación | Dashboard resumen con widgets que el usuario añade, quita y reordena [21] |
| Onboarding | GAP — sin fuente específica |
| Logging | Tres anillos (move/exercise/stand) [21]; goal setting de doble horizonte: ajuste temporal del día "without disrupting their main goal" [21] |
| Dark mode | Alterna claro/oscuro (23–58 % oscuro medido) [F8]; plataforma: fondos base/elevated para profundidad entre interfaces oscuras (HIG Dark Mode) [F7]. ⚠ "calm type" atribuido a Apple Fitness: refutado (accent saturado, sin documento) [F8] |
| Tipografía | GAP — sin fuente propia; la plataforma soporta Dynamic Type [22][23] |
| Accesibilidad | La más fuerte del benchmark: 5 funciones visuales soportables — Sufficient Contrast, Dark Interface, Larger Text, Differentiate Without Color Alone, Reduce Motion [23]; Dynamic Type / Increased Contrast / Differentiate Without Color citados en ADA 2026 [22]. Caveat: Fitness+ sigue imperfecto para ciegos/baja visión pese a Audio Hints [24] |
| Monetización UX | Suscripción Fitness+ (vídeo on-demand + meditaciones) [21] |
| Lección para Healthspan | Widgets priorizables por el usuario; horizonte de meta doble; accesibilidad como lista de funciones verificables, no como aspiración |

### Ficha 7 (referencia secundaria) — Pump Pal

Aparece en DESIGN.md v1 pero no es app del benchmark de masa. "1-tap quick start + FAB central" solo sostenida por el case study del propio diseñador (fuente única) [F10]. Usar como apoyo del patrón quick-start, nunca como evidencia de mercado.

## 2. Tabla comparativa por dimensión

Leyenda: ✅ hallazgo con fuente verificada · GAP sin evidencia · ⚠ corrección QA aplicada.

| Dimensión | Strava | WHOOP | Hevy | Fitbod | TrainingPeaks | Apple Fitness |
|---|---|---|---|---|---|---|
| IA / asistentes | GAP | Coach conversacional con apoyo empírico n=36 [11]; en bottom nav [8] | GAP | Recomendación como propuesta editable [16] | GAP | GAP (trainers guiados en Fitness+ [21]) |
| Navegación | Record-first en bottom nav [F9]; feed como scroll del dashboard [1] | Bottom nav fija por feedback de miembros [8]; home de un scroll [5] | Rediseño nativo centrado en el momento del log [15] | GAP | Calendario como superficie principal [19] | Widgets reordenables por el usuario [21] |
| Onboarding | Feed poblado desde el día 1 [1] | Calibración 7 días con desbloqueo gradual; fracaso de comunicación documentado [9] | Splash + signup social + paywall con plan preseleccionado [32] | Trial 7 días sin tarjeta con timeline explícito [17] | GAP | GAP |
| Logging | Log = social sin coste de creación [1]; streak semanal [2] | Journal ≤10 preguntas diarias, edición retroactiva 14 días [7] | Rest timer automático por defecto y por ejercicio [12] | GAP | Log + calendario como valor base [19] | Tres anillos + meta de doble horizonte [21] |
| Dark mode | GAP | Dark-native medido 72–94 % [F6]; superficies escalonadas [F5] | ⚠ No dark-first (71–85 % claro; tema opcional) [F1][F2] | GAP | GAP | Alterna 23–58 % [F8]; patrón base/elevated HIG [F7] |
| Tipografía | GAP | DINPro números / Proxima Nova palabras [F5]; all-caps ⚠ sin fuente oficial [F5] | ⚠ tabular-nums no verificable → citar Inter/OpenType [F4]; single-accent #267EE8 [F1] | GAP | GAP | GAP propia; Dynamic Type a nivel plataforma [22][23] |
| Accesibilidad | Objetivo pequeño en pantalla de riesgo (ciclismo) [3] | GAP | GAP | GAP | GAP | 5 funciones visuales listables [23]; ADA 2026 [22]; caveat Fitness+ [24] |
| Monetización UX | Insights y análisis detallado tras paywall; leaderboard gratis [34][35]; pop-ups agresivos → rechazo [36] | GAP (membership no cubierto) | Paywall en onboarding, anual preseleccionado [32]; rechazo a suscripción [37] | Trial honesto sin tarjeta [17] | Free tier = log+calendario; pago = análisis [19] | Suscripción de contenido Fitness+ [21] |

## 3. Matriz de cobertura de evidencia (para QA)

✅ = hay al menos una cita en los ledgers · GAP = sin fuente · ⚠ = afirmación corregida por QA.

| App | IA | Naveg. | Onboarding | Logging | Dark | Tipografía | Accesib. | Monetiz. |
|---|---|---|---|---|---|---|---|---|
| Strava | GAP | ✅ [1][F9] | ✅ [1] | ✅ [1][2] | GAP | GAP | ✅ [3] | ✅ [34][35][36] |
| WHOOP | ✅ [8][11] | ✅ [5][8] | ✅ [9] | ✅ [7] | ✅ [F5][F6] | ✅ [F5] (⚠ all-caps) | GAP | GAP |
| Hevy | GAP | ✅ [15] | ✅ [32] | ✅ [12][31][37] | ⚠ [F1][F2] | ⚠ [F4] | GAP | ✅ [32][37] |
| Fitbod | ✅ [16] | GAP | ✅ [17] | GAP | GAP | GAP | GAP | ✅ [17] |
| TrainingPeaks | GAP | ✅ [19] | GAP | ✅ [19] | GAP | GAP | GAP | ✅ [19] |
| Apple Fitness | GAP | ✅ [21] | GAP | ✅ [21] | ✅ [F7][F8] | GAP | ✅ [22][23][24] | ✅ [21] |

Huecos concentrados: **IA/asistentes** (2/6 apps), **accesibilidad** (2/6), **tipografía** (2/6 con correcciones), **dark mode** (3/6), y Fitbod/TrainingPeaks en general (3/8 y 3/8). Coincide con la corrección QA nº 7: "Fitbod y TrainingPeaks aún no tienen hallazgos verificados" [benchmark-citations §4].

## 4. Hallazgos transversales (cada uno enlaza a las fichas)

1. **El log es el producto; social y análisis son capas.** Strava [1], Hevy [13], TrainingPeaks [19].
2. **Social como subproducto del log, opcional y desactivable.** Strava [1], Hevy [31][37].
3. **Streaks con barra baja y ventana tolerante** (Strava cuenta semanas) [2].
4. **Una métrica hero + color semántico fijo** (WHOOP triple ring [6]; Apple three rings [21]).
5. **Onboarding con timeline honesto** (calibración WHOOP [9], trial Fitbod sin tarjeta [17]).
6. **Paywall fuera del acto de registrar** (Strava segments [35], TrainingPeaks calendar [19]); el paywall agresivo genera rechazo visible [36][37].
7. **Doble persona en un mismo sistema** (Hevy Trainer [15]) valida trainer↔trainee.
8. **Accesibilidad verificable = lista de funciones** (Apple [23]), no adjetivos; y targets ≥44–48 px/pt como estándar convergente HIG/Material/WCAG 2.2 [26][29][30].

## 5. Adoptamos (A1–A15)

| # | Decisión | Outcome (por qué) | Evidencia del benchmark |
|---|---|---|---|
| A1 | **El log es el producto**: proteger "registrar el entreno de hoy en un gesto" como núcleo intocable; social y análisis son capas encima. | El usuario obtiene valor el día 1 sin configurar nada; el hábito se forma sobre el acto de registrar, no sobre contenido. | Ficha 1 Strava (posición, logging), Ficha 3 Hevy (posición), Ficha 5 TrainingPeaks (logging); tabla §2 fila *Logging*; hallazgo §4.1. [1][13][19] |
| A2 | **Record-first**: quick-start a 1 toque sin wizard, accesible desde la barra inferior/FAB en cualquier pantalla. | La activación ocurre donde el usuario ya está; cero fricción previa al registro. | Ficha 1 Strava (navegación — help oficial 2026-09-10: "Record from the bottom navigation menu", "Start button near the bottom of the screen") [F9]; tabla §2 fila *Navegación*. Pump Pal solo como apoyo secundario [F10] (fuente única, no evidencia de mercado). |
| A3 | **Bottom nav fija de destinos únicos; cuenta desde el avatar.** | Las funciones más usadas quedan a un clic; no hay dos sistemas de navegación compitiendo. | Ficha 2 WHOOP (navegación — decisión tomada por feedback de miembros, "just a single click away"; perfil desde la foto de perfil) [8]; tabla §2 fila *Navegación*. |
| A4 | **Una métrica hero por pantalla + color semántico fijo por concepto, sin leyenda.** | El usuario lee su estado de un vistazo sin parsear tres gráficas separadas. | Ficha 2 WHOOP (posición — triple ring; tipografía — DINPro para números [F5]) [6]; Ficha 6 Apple Fitness (logging — three rings) [21]; hallazgo §4.4. |
| A5 | **Logging como lista corta y predecible con prefill de la última sesión y corrección tardía fácil** (referencia WHOOP: ≤10 ítems, edición retroactiva ≤14 días). | Fricción diaria baja; las respuestas consistentes son lo que desbloquea el análisis, no al revés. | Ficha 2 WHOOP (logging — "Track 10 or fewer at a time", 3 entradas al flujo, edición 14 días) [7]; tabla §2 fila *Logging*. |
| A6 | **Rest timer como default silencioso** en la sesión de gym (configurable global y por ejercicio, heredado a la rutina). | El timer es parte del flujo de log en el gimnasio, no una opción que hay que descubrir. | Ficha 3 Hevy (logging — timer automático, en workout en vivo y al construir rutina) [12]; tabla §2 fila *Logging*. |
| A7 | **Social como subproducto del log**: guardar una sesión publica el item en el mismo acto; social opcional y desactivable desde el día 1. | Nadie construye un feed que el usuario deba "alimentar"; quien no quiere social no migra por él. | Ficha 1 Strava (logging/onboarding — "items appear without anyone composing them") [1][2]; Ficha 3 Hevy (logging — atletas sugeridos desactivables [31]; fuga documentada por features sociales no usadas [37]); hallazgo §4.2. |
| A8 | **Streaks solo cuando el log diario sea hábito; entonces ventana semanal tolerante** (gracia de 7 días), nunca racha diaria punitiva. | La barra para mantener la racha es baja y tolerante: un día fallado no rompe el hábito. | Ficha 1 Strava (logging — "counts its streak in weeks rather than days") [2]; hallazgo §4.3. |
| A9 | **Todo dato derivado (RM, volumen, carga recomendada) con una única línea de progreso y timeline honesto desde el onboarding; cualquier trial con fecha de fin explícita.** | El usuario no cree que la app está "rota"; la confianza en los datos se construye desde la semana 1. | Ficha 2 WHOOP (onboarding — fracaso documentado por su diseñadora: "shown in 7 different ways without clear expectations or a timeline") [9]; Ficha 4 Fitbod (onboarding/monetización — trial 7 días sin tarjeta, recordatorio día 5) [17]; hallazgo §4.5. |
| A10 | **Cualquier sugerencia (peso/series) como propuesta editable que el usuario confirma**, nunca un wizard de formularios previos. | El usuario edita una propuesta en lugar de rellenar un cuestionario antes de entrenar. | Ficha 4 Fitbod (IA — "fully customized workouts based on your goals, fitness level, and available equipment"; la app genera y el usuario confirma/edita) [16]; tabla §2 fila *IA / asistentes*. |
| A11 | **Trainer↔trainee con los mismos componentes de log y permisos distintos; sin segundo flujo.** | El trainer añade valor sin complicar el entreno del trainee (validado por mercado). | Ficha 3 Hevy (doble persona "Trainer" — "add value without making your workout more complicated") [15]; hallazgo §4.7. |
| A12 | **Paywall futuro solo sobre análisis avanzado/coaching; nunca sobre el acto de registrar ni sobre el historial propio.** | Monetizar sin erosionar la confianza de usuarios fieles (el contraejemplo Strava y el moderado TrainingPeaks). | Ficha 1 Strava (monetización — leaderboard gratis, análisis detallado a pago [35]; pop-ups "getting aggressive" [36]; insights en tier de pago [34]); Ficha 5 TrainingPeaks (monetización — free tier = poblar calendario y ver datos) [19]; hallazgo §4.6. |
| A13 | **Accesibilidad como checklist de funciones verificables** (contraste suficiente, texto grande, diferenciar sin color, reduce motion) **y targets ≥44–48 px/pt**, mayores en pantallas de registro en movimiento. | Accesibilidad auditable, no aspiracional; el contexto gym (guantes, sudor) no penaliza objetivos pequeños. | Ficha 6 Apple Fitness (accesibilidad — 5 funciones [23]; ADA 2026 [22]; caveat Fitness+ imperfecta [24]); Ficha 1 Strava (accesibilidad — "must tap the small icon in the bottom right" en contexto de riesgo) [3]; hallazgo §4.8 [26][29][30]. |
| A14 | **Dark-first como decisión de producto declarada** (no como hallazgo de benchmark), apoyada en WHOOP dark-native medido + Apple HIG dark mode; **profundidad por luminosidad** (ladder de superficies, sin sombras) + borde 1px. | Un sistema visual oscuro coherente con fuente real, no una imitación reverse-engineered. | Ficha 2 WHOOP (dark — 72–94 % píxeles medido [F6]; superficies escalonadas y gradiente oficial #283339→#101518 [F5]); Ficha 6 Apple Fitness (dark — fondos base/elevated HIG [F7]); QA veredicto 13 [F1][F6][F7][F8]. |
| A15 | **Números con tratamiento tipográfico propio (hero) y tabular-nums atribuido a Inter/OpenType**, no a Hevy. | Las cifras no reflowan al cambiar y la métrica hero se lee de un vistazo. | Ficha 2 WHOOP (tipografía — guidelines oficiales: DINPro para números, Proxima Nova para palabras) [F5]; Ficha 3 Hevy (tipografía — corrección QA: práctica citable vía Inter/OpenType) [F4]. |

## 6. Evitamos (E1–E10)

| # | Evitamos | Por qué (riesgo medido o documentado) | Evidencia del benchmark |
|---|---|---|---|
| E1 | Un feed social de contenido que el usuario debe alimentar (producto de contenido antes que log). | El modelo opuesto funciona sin coste de creación [1]; en Hevy la no-adopción social es motivo explícito de fuga [37]. | Ficha 1 Strava (logging) [1]; Ficha 3 Hevy (logging/monetización) [31][37]. |
| E2 | Paywall sobre el acto de registrar o sobre análisis poco percibido; pop-ups de trial agresivos. | Strava 2025 movió el análisis a pago y cortó API a terceros; la comunidad reacciona con desconfianza incluso entre usuarios fieles. | Ficha 1 Strava (monetización) [34][35][36]; Ficha 5 TrainingPeaks (monetización, contraejemplo moderado) [19]. |
| E3 | Streaks diarios punitivos (racha que se rompe por un día). | Strava cuenta semanas con ventana de 7 días; la barra baja sostiene el hábito. | Ficha 1 Strava (logging) [2]. |
| E4 | Mostrar datos derivados sin timeline único y claro (mensajes sueltos tipo "trends coming soon"). | WHOOP: calibración "shown in 7 different ways without clear expectations or a timeline" → usuarios creían que la app estaba "broken". | Ficha 2 WHOOP (onboarding) [9]. |
| E5 | Wizards o formularios previos a una recomendación. | Fitbod entrega la workout como propuesta generada que el usuario usa/edita, sin cuestionario previo. | Ficha 4 Fitbod (IA) [16]. |
| E6 | Un segundo flujo de componentes para la persona trainer. | Hevy Trainer: mismas vistas de log, permisos distintos (inferencia UX sobre [15], no texto de la fuente); filosofía "add value without making your workout more complicated". | Ficha 3 Hevy (doble persona) [15]. |
| E7 | Objetivos pequeños en pantallas de registro en movimiento (gym/outdoor). | Caso documentado de Strava en ciclismo: para ver velocidad/distancia "must tap the small icon in the bottom right" — target pequeño en contexto de riesgo. | Ficha 1 Strava (accesibilidad) [3]. |
| E8 | Color como única señal (en errores, semánticos y readiness). | Apple lista "Differentiate Without Color Alone" como función soportable; Fitness+ sigue imperfecta para ciegos/baja visión pese a Audio Hints. | Ficha 6 Apple Fitness (accesibilidad) [23][24]. |
| E9 | Construir rutinas/planificación sobre un modelo pobre antes de tener el log sólido (plan-first sin base). | TrainingPeaks: el valor base es log + calendario; el análisis es la capa superior. | Ficha 5 TrainingPeaks (logging/navegación) [19]. |
| E10 | Usar packs de tokens reverse-engineered (p. ej. `Meliwat/awesome-ios-design-md` / spectr.to) como fuente de diseño. | QA: el pack contradice las guidelines oficiales de WHOOP y "no es fuente fiable para tokens; usar fuentes primarias". | benchmark-citations §3 [F11]. |

## 7. Atribuciones de DESIGN.md v1 que se eliminan (evitación de falsa atribución)

Correcciones QA aplicadas (benchmark-citations §1 y §4; incorporadas en §10 de este informe):

| Afirmación v1 | Veredicto QA | Corrección adoptada |
|---|---|---|
| Hevy = dark-first | ❌ contradicho (6/6 capturas oficiales UI clara 71–85 %; tema oscuro es elección del usuario) | No atribuir dark-first a Hevy; la referencia dark del benchmark es WHOOP [F1][F2]. |
| Accent #1E6FFF = "Hevy Blue" | ❌ hex no trazable (accent real medido #267EE8; #1E6FFF ausente de capturas y assets de hevy.com) | Declarar la paleta v1 decisión propia de Healthspan; quitar toda atribución a Hevy [F1][F3][F11]. |
| Hevy = tabular-nums | ⚠ no verificable (Hevy no publica spec) | Citar la práctica vía Inter/OpenType [F4]. |
| WHOOP = hairline borders | ❌ sin fuente oficial | Reformular como "profundidad por luminosidad + borde 1px" (HIG base/elevated + superficies WHOOP medidas) [F5][F6][F7]. |
| WHOOP = all-caps micro-labels | ⚠ parcial (solo en descripciones de terceros) | No citarla como regla oficial WHOOP; mantenerla, si se usa, como decisión propia [F5]. |
| Apple Fitness = calm type | ❌ subjetivo/sin fuente (accent saturado #A7FE01 medido) | Eliminar la atribución [F8]. |
| Rampa readiness = "rampa WHOOP" | ⚠ parcial (oficial #16EC06/#FFDE00/#FF0026; v1 usa hexes propios) | Mantener hexes propios declarados adaptación, con la rampa oficial como referencia conceptual [F5]. |
| Pump Pal = evidencia de quick-start | ⚠ fuente única (case study del propio diseñador) | Usar solo como apoyo del patrón, nunca como evidencia de mercado [F10]. |

## 8. Decisiones aplazadas (GAPs — no se decide con evidencia ausente)

| # | Tema | Estado | Condición para decidir |
|---|---|---|---|
| G1 | Coach/IA conversacional | Solo evidencia: estudio de investigación (Chalmers, n=36, ACM MHCI 2025) sobre un prototipo, no la app de producción [11]. Lo único citable como patrón es su placement (bottom nav, un clic del home) [8]. | No adoptar ni descartar todavía; si se adopta, justificarlo como hipótesis con prototipo propio. |
| G2 | Accesibilidad de WHOOP/Hevy/Fitbod/TrainingPeaks | GAP en la matriz de cobertura §3 (2/6 apps con evidencia). | A13 queda sustentada solo por Apple + estándares [22][23][26][29][30]; auditoría propia pendiente. |
| G3 | Tema por defecto de Hevy/Fitbod/TrainingPeaks en instalación limpia | Inferido hoy de material de marketing (benchmark-citations §5). | Capturas propias de instalación limpia antes de fijar reglas de tema por app. |
| G4 | Escala de la cifra hero (76/800 de v1) | Sin captura de WHOOP con escala medida (benchmark-citations §5). | La escala queda como decisión propia de Healthspan, no como benchmark [F5][F6]. |
| G5 | Monetización de WHOOP y dark/tipografía de Strava, TrainingPeaks, Fitbod | GAP en matriz §3. | Ninguna decisión de diseño se apoya en estas celdas hasta tener fuente. |

## 9. Trazabilidad con la matriz de evidencia (evidence-matrix.md)

Sin contradicciones con las 40 filas de la matriz consolidada; mapeo decisión→fila:

A1→M1 · A2→C6 · A3→C6 · A4→M5+P7 · A5→M9 · A6→C5 · A7→M2 · A8→M3 · A9→M4 · A10→M7 · A11→C4 · A12→M6 · A13→X1+X4 · A14→P9+P10 · A15→T1+T3 · E1→M2 · E2→M6 · E3→M3 · E4→M4 · E5→M7 · E6→C4 · E7→E4+X1 · E8→X4 · E9→M1 · E10→P6 · §3→P6+P8+T1.

## 10. Correcciones QA aplicadas

- No se atribuye dark-first a Hevy (refutado [F1]); la referencia dark del benchmark es WHOOP [F6].
- #1E6FFF no se atribuye a Hevy; el accent medido de Hevy es #267EE8 [F1][F3]. Los tokens v1 proceden de un pack reverse-engineered [F11] y son decisión propia de Healthspan.
- "Hairline borders" se reformula como profundidad por luminosidad + borde 1px [F5][F7].
- Tabular-nums se cita vía Inter/OpenType, no Hevy [F4].
- La rampa de readiness se cita como referencia conceptual WHOOP (#16EC06/#FFDE00/#FF0026), con hexes propios declarados adaptación [F5].

### Correcciones de fidelidad de citas aplicadas (t_5758d57c, post-aprobación QA t_32f4fbb4)

1. **[36]** eliminada la frase "fancy graphs I'll never look at" de Ficha 1, de A12 y del cuerpo de `research-ux-apps-2026.md` (§2 Paywall): no está en el ledger [36] y el hilo de Reddit no es accesible sin login (verificado 2026-10-23). Se mantiene solo "the 'trial' pop-ups are getting aggressive", que sí está respaldado; el ledger [36] queda anotado para no reintroducirla.
2. **[13]** quote del ledger extendida con "Loved by 17+ million athletes" y "4.9 App Store & 4.9 Google Play (630,000+ ratings)" (verbatim en hevyapp.com, verificado en vivo por QA); la ficha cita ahora esas frases.
3. **[9]** quote del ledger extendida con "We aim to increase the number of members who achieve a 7 day perfect start" (verbatim en ramona-mengxi.com, verificado en vivo por QA).
4. **§3 conteo corregido:** TrainingPeaks 4/8 → 3/8 (✅ en Navegación, Logging y Monetización sobre su propia fila de la matriz).
5. **[16]** Ficha 4 cita ahora la frase completa del ledger, sin elipsis que unía fragmentos.
6. **Inferencias marcadas:** "mismas vistas de log, permisos distintos" (Ficha 3 y E6) se declara inferencia UX sobre [15], no texto de la fuente. Las referencias "transversal 4.x" del doc de decisiones se reescriben como "hallazgo §4.x" para ancla inequívoca.


## 11. Evidencia pendiente (heredada de benchmark-citations §5)

- Capturas propias de instalación limpia de Hevy (tema por defecto) y de Fitbod/TrainingPeaks.
- Medición de escala de cifra hero en WHOOP.
- Contraste WCAG real de los tokens v1 sobre #0E1116.
- Auditoría de accesibilidad de WHOOP/Hevy/Fitbod/TrainingPeaks (hoy GAP en la matriz).

## 12. Registro de verificación QA

- **QA (t_32f4fbb4, 2026-10-23):** informe **aprobado** — 7 fichas, tabla 8×6 sin celdas sin cita/GAP/⚠, 0 citas huérfanas verificadas por script propio (fichas 28 ids [n] + 11 [Fn]; decisiones 26 [n] + 11 [Fn]), 25/25 decisiones A/E enlazadas a ficha/tabla + ids y trazables a evidence-matrix.md, 5/5 G con estado de evidencia.
- **Recomprobación en vivo por QA:** [F9] Strava help (verbatim, 2026-09-10); procedencia [F11] (11/11 hexes de DESIGN.md v1 presentes en el pack Meliwat/awesome-ios-design-md); Hevy 17M+/4.9 (hevyapp.com).
- **Correcciones no bloqueantes:** las 6 de §10 (t_5758d57c), aplicadas en este informe y en los documentos fuente antes de esta consolidación.
- **Guard anti-regresión:** "dark-first", "calm type", "hairline", "Hevy Blue" y "tabular-nums de Hevy" aparecen en este informe solo en contexto correctivo o como decisión propia declarada.


## 13. Índice de citas (ids usados en este informe → fuente)

Cada id usado en el informe resuelve contra un ledger; la evidencia verbatim está en el ledger correspondiente.

### Ledger `[n]` — docs/design/research-ux-apps-2026.md (§Sources)
[1] https://appservatory.com/case-studies/strava — Appservatory — Strava mechanics case study
[2] https://gamification.gamebizconsulting.com/case-studies/strava — Mechanics Playbook — Strava gamification case study
[3] https://stevenmerickson.com/strava.html — Steven Merrickson — Strava cycling redesign case study
[5] https://www.whoop.com/us/en/thelocker/the-all-new-whoop-home-screen — WHOOP Locker — Home screen official
[6] https://www.uxsnaps.com/daily-health-dashboard — UX Snaps — 5 UX lessons from WHOOP's dashboard
[7] https://www.whoop.com/us/en/thelocker/the-whoop-journal — WHOOP Locker — Journal official
[8] https://www.whoop.com/us/en/thelocker/app-update-navigation-bar — WHOOP Locker — All-new navigation bar
[9] https://www.ramona-mengxi.com/whoop-growth-onboarding — Ramona Mengxi — WHOOP onboarding/calibration redesign (designer)
[11] https://research.chalmers.se/publication/548305/file/548305_Fulltext.pdf — Chalmers MHCI — WHOOP Coach conversational agent study
[12] https://www.hevyapp.com/features/workout-rest-timer — Hevy — rest timer feature page
[13] https://www.hevyapp.com — Hevy — official site
[15] https://www.reddit.com/r/Hevy/comments/1umaz02/hevy_community_update_1_trainer_native_redesign — Hevy community update #1 (Trainer, native redesign)
[16] https://www.fitbod.me — Fitbod — official site
[17] https://www.botsi.com/resources/flows/apps/fitbod — Botsi — Fitbod onboarding flow teardown
[19] https://screensdesign.com/showcase/trainingpeaks — ScreensDesign — TrainingPeaks UX breakdown
[21] https://screensdesign.com/showcase/apple-fitness — ScreensDesign — Apple Fitness UI breakdown
[22] https://developer.apple.com/design/awards — Apple Design Awards 2026
[23] https://developer.apple.com/videos/play/tech-talks/111433 — Apple Tech Talk — Accessibility Nutrition Labels (Dynamic Type, Dark Mode, contrast)
[24] https://afb.org/aw/jan2023/apple-fitness-plus-revisited — AFB AccessWorld — Apple Fitness+ accessibility revisited
[26] https://www.w3.org/TR/WCAG22 — W3C WCAG 2.2 specification
[29] https://developer.apple.com/design/tips — Apple Design tips: Hit targets
[30] https://developer.apple.com/design/human-interface-guidelines/buttons — Apple HIG: Buttons
[31] https://www.hevyapp.com/features/content-feed — Hevy — social content feed feature page
[32] https://screensdesign.com/apps/hevy-workout-tracker-gym-log — ScreensDesign — Hevy onboarding/paywall teardown
[34] https://www.strava.com/subscribe — Strava — official subscription page (free vs subscription feature table)
[35] https://velo.outsideonline.com/road/road-racing/strava-shake-up-major-changes-for-paid-and-free-users — Velo — Strava paywall shake-up
[36] https://www.reddit.com/r/Strava/comments/1qd2d69/is_it_just_me_or_is_the_strava_subscription — r/Strava — subscription value sentiment thread (Jan 2026)
[37] https://www.reddit.com/r/Hevy/comments/1qqva2d/i_dont_really_use_the_social_side_of_hevy_and — r/Hevy — user sentiment on social features/subscriptions

### Ledger `[Fn]` — docs/design/benchmark-citations.md (§6)
- [F1] App Store — Hevy Workout Tracker (id1458862350), 6 capturas iOS analizadas por píxeles. https://apps.apple.com/us/app/hevy-workout-tracker-gym-log/id1458862350
- [F2] Hevy Help — "How to change the theme (light or dark)? (Android/iOS)". https://www.hevyapp.com/help/change-the-theme-android-ios
- [F3] Hevy marketing site (hexes #1D83EA/#1780EA) y CSS/JS de hevy.com (`#008CFF`; `#1E6FFF` ausente). https://www.hevyapp.com/ · https://hevy.com/login
- [F4] Inter (rsms) — OpenType features: "tabular numbers". https://rsms.me/inter/
- [F5] WHOOP Brand & Design Guidelines (PDF oficial): Proxima Nova para palabras, DINPro para números; Teal #00F19F (CTA), Strain #0093E7, Recovery Blue #67AEE6, High/Medium/Low #16EC06/#FFDE00/#FF0026, Sleep #7BA1BB, Background Gradient #283339→#101518. https://developer.whoop.com/assets/files/WHOOP%20-%20Brand%20&%20Design%20Guidelines-bdea3554e94b4ea09e68695b1e8dc8e7.pdf
- [F6] App Store — WHOOP 2.0 (id933944389), 4 capturas iOS (Sleep/Recovery/Strain/Healthspan) analizadas por píxeles. https://apps.apple.com/us/app/whoop-2-0/id933944389
- [F7] Apple Human Interface Guidelines — Dark Mode (base / elevated backgrounds para profundidad al superponer interfaces oscuras). https://developer.apple.com/design/human-interface-guidelines/foundations/dark-mode
- [F8] App Store — Apple Fitness (id1208224953), 4 capturas iOS analizadas por píxeles (#A7FE01). https://apps.apple.com/us/app/apple-fitness/id1208224953
- [F9] Strava Help Center — "How do I Record an Activity on Strava?" (Marie, 2026-09-10). https://support.strava.com/en-us/articles/15402137-how-do-i-record-an-activity-on-strava
- [F10] Ralph Dela Cruz — Pump Pal case study (diseñador del producto, ene-2024→ene-2025): Quick Start 1 toque + botón central de nav con popup. https://ralphdelacruz.com/pump-pal
- [F11] Meliwat/awesome-ios-design-md (procedencia de los tokens v1; creada 2026-04-16, spectr.to). https://github.com/Meliwat/awesome-ios-design-md · https://raw.githubusercontent.com/Meliwat/awesome-ios-design-md/main/design-md/fitness/hevy/DESIGN.md
