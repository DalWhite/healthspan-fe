# Benchmark fitness — fichas por app y tabla comparativa por dimensión

> **Puntero (t_d48ac88d):** este documento queda como borrador de trabajo; la versión definitiva consolidada es `docs/design/benchmark-informe-final.md` (incluye además las correcciones de fidelidad de citas t_5758d57c).

Borrador estructurado (t_188bcde0). Fuente primaria: `docs/design/research-ux-apps-2026.md` (t_75c7b777, citas `[n]`) y `docs/design/benchmark-citations.md` (t_b6020ed5, citas `[Fn]`). Este documento no introduce afirmaciones nuevas: reorganiza la evidencia ya citada y marca explícitamente los huecos sin fuente. Listo para que QA (t_32f4fbb4) valide citas y para que la tarjeta de decisiones (t_ac4efc0d) enlace cada adopción/evitación a una fila de la tabla.

## 0. Cómo leer este documento

- Cada hallazgo lleva la cita de la fuente que lo sostiene (`[n]` = ledger del informe de benchmark; `[Fn]` = ledger de verificación QA).
- **GAP** = dimensión sin evidencia verificable en los dos ledgers. No se afirma nada en esas celdas; queda como evidencia pendiente.
- **⚠ refutado** = afirmación que circulaba en DESIGN.md v1 y QA la contradijo; se corrige aquí.
- Cobertura de evidencia por app y dimensión: §3 (segunda tabla).

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

## 5. Correcciones QA aplicadas en este documento

- No se atribuye dark-first a Hevy (refutado [F1]); la referencia dark del benchmark es WHOOP [F6].
- #1E6FFF no se atribuye a Hevy; el accent medido de Hevy es #267EE8 [F1][F3]. Los tokens v1 proceden de un pack reverse-engineered [F11] y son decisión propia de Healthspan.
- "Hairline borders" se reformula como profundidad por luminosidad + borde 1px [F5][F7].
- Tabular-nums se cita vía Inter/OpenType, no Hevy [F4].
- La rampa de readiness se cita como referencia conceptual WHOOP (#16EC06/#FFDE00/#FF0026), con hexes propios declarados adaptación [F5].

## 6. Evidencia pendiente (heredada de benchmark-citations §5)

- Capturas propias de instalación limpia de Hevy (tema por defecto) y de Fitbod/TrainingPeaks.
- Medición de escala de cifra hero en WHOOP.
- Contraste WCAG real de los tokens v1 sobre #0E1116.
- Auditoría de accesibilidad de WHOOP/Hevy/Fitbod/TrainingPeaks (hoy GAP en la matriz).

## Fuentes

Todos los ids `[n]` están en `docs/design/research-ux-apps-2026.md` (§Sources) y los `[Fn]` en `docs/design/benchmark-citations.md` (§6). Este documento no añade fuentes nuevas.
