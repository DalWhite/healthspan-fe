# Decisiones de adopción y evitación — rediseño Healthspan (benchmark fitness)

> **Puntero (t_d48ac88d):** este documento queda como borrador de trabajo; la versión definitiva consolidada es `docs/design/benchmark-informe-final.md` (incluye además las correcciones de fidelidad de citas t_5758d57c).

Sección de decisiones del informe final (t_d48ac88d). Fecha: 2026-10-23 · Autor: UX (ux-designer) · Tarjeta: t_ac4efc0d.

Insumos (ninguna afirmación nueva; todo se enlaza a evidencia ya citada):
- Fichas por app y tabla comparativa: `docs/design/benchmark-fichas-tabla-t188bcde0.md` (fichas §1, tabla §2, matriz de cobertura §3, hallazgos transversales §4).
- Ledger de investigación `[n]`: `docs/design/research-ux-apps-2026.md` (§Sources, citas verbatim).
- Ledger QA `[Fn]` y veredictos: `docs/design/benchmark-citations.md` (§1 veredictos, §3 procedencia, §4 correcciones).
- Coherencia cruzada con la matriz de evidencia: `docs/design/evidence-matrix.md` (filas P/T/E/C/X/M).

Claves de lectura:
- **A#** = adoptamos · **E#** = evitamos · **G#** = decisión aplazada (hueco de evidencia, no se decide con GAPs).
- Cada decisión lleva: *Outcome* (el resultado de usuario que mejora — la justificación) y *Evidencia* (ancla en fichas/tabla + ids de cita).
- Un GAP de la matriz de cobertura (§3) nunca fundamenta una decisión; queda en §4.

---

## 1. Adoptamos (A1–A15)

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

## 2. Evitamos (E1–E10)

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

## 3. Atribuciones de DESIGN.md v1 que se eliminan (evitación de falsa atribución)

Correcciones QA aplicadas (benchmark-citations §1 y §4; incorporadas en las fichas §5):

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

## 4. Decisiones aplazadas (GAPs — no se decide con evidencia ausente)

| # | Tema | Estado | Condición para decidir |
|---|---|---|---|
| G1 | Coach/IA conversacional | Solo evidencia: estudio de investigación (Chalmers, n=36, ACM MHCI 2025) sobre un prototipo, no la app de producción [11]. Lo único citable como patrón es su placement (bottom nav, un clic del home) [8]. | No adoptar ni descartar todavía; si se adopta, justificarlo como hipótesis con prototipo propio. |
| G2 | Accesibilidad de WHOOP/Hevy/Fitbod/TrainingPeaks | GAP en la matriz de cobertura §3 (2/6 apps con evidencia). | A13 queda sustentada solo por Apple + estándares [22][23][26][29][30]; auditoría propia pendiente. |
| G3 | Tema por defecto de Hevy/Fitbod/TrainingPeaks en instalación limpia | Inferido hoy de material de marketing (benchmark-citations §5). | Capturas propias de instalación limpia antes de fijar reglas de tema por app. |
| G4 | Escala de la cifra hero (76/800 de v1) | Sin captura de WHOOP con escala medida (benchmark-citations §5). | La escala queda como decisión propia de Healthspan, no como benchmark [F5][F6]. |
| G5 | Monetización de WHOOP y dark/tipografía de Strava, TrainingPeaks, Fitbod | GAP en matriz §3. | Ninguna decisión de diseño se apoya en estas celdas hasta tener fuente. |

## 5. Trazabilidad con la matriz de evidencia (evidence-matrix.md)

Sin contradicciones con las 40 filas de la matriz consolidada; mapeo decisión→fila:

A1→M1 · A2→C6 · A3→C6 · A4→M5+P7 · A5→M9 · A6→C5 · A7→M2 · A8→M3 · A9→M4 · A10→M7 · A11→C4 · A12→M6 · A13→X1+X4 · A14→P9+P10 · A15→T1+T3 · E1→M2 · E2→M6 · E3→M3 · E4→M4 · E5→M7 · E6→C4 · E7→E4+X1 · E8→X4 · E9→M1 · E10→P6 · §3→P6+P8+T1.

## 6. Verificación de citas

Todos los ids `[n]` usados existen en el ledger de `research-ux-apps-2026.md` (§Sources) y todos los `[Fn]` en `benchmark-citations.md` (§6). Verificación automática (grep) en el momento de la entrega: 0 citas huérfanas. Este documento no añade fuentes nuevas.
