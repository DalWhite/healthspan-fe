# Investigación UX 2026 — Strava, WHOOP, Hevy, Fitbod, TrainingPeaks, Apple Fitness

Benchmark de UX para Healthspan (board healthtracker). Cada afirmación externa va citada con su id de ledger; la evidencia verbatim de cada fuente citada aparece bajo su URL en Sources. Las afirmaciones de conocimiento del modelo que sostienen la síntesis van marcadas `[unverified]`.

## 1. Strava

**IA de navegación.** El feed social de Strava no es una pestaña aparte: ocupa la parte inferior del dashboard de inicio como un único scroll vertical, mezclando usuarios recomendados con actividades de quien sigues.[1] El feed se construye a partir de actividades registradas, no de posts escritos: "items appear without anyone composing them" — el contenido social existe como subproducto del log, sin coste de creación para el usuario.[1] Un usuario nuevo que no sigue a nadie aterriza en un feed ya poblado porque los usuarios recomendados ocupan la cabecera.[1]

**Bucle de retención.** Seis mecánicas sostienen el loop: streak, challenges, feed social, leaderboards, achievements/milestones y grupos.[2] El streak de Strava cuenta semanas, no días: basta registrar una actividad dentro de una ventana de 7 días.[2] Lección directa para Healthspan: la barra para mantener la racha es baja y tolerante (regla de "grace" de ux-ui-good-apps), y el guardado de un entreno publica el item social en el mismo acto — log y social son el mismo evento, no dos flujos.[2]

**Paywall.** En la página oficial, el tier de pago se vende con insights ("See your progress, spot trends and get faster with real-time insights") y con mecánicas sociales avanzadas ("Set goals, climb leaderboards and rally your friends with custom challenges").[34]
El shake-up de 2025 mantuvo gratis la posición en leaderboards de segmentos pero movió el análisis detallado al tier de pago,[35] y Strava cortó el acceso a su API para decenas de miles de apps de terceros para usuarios sin suscripción.[35]
Sentimiento de comunidad (r/Strava, enero 2026): los pop-ups de prueba "are getting aggressive", y un usuario de años "starting to wonder if I'm actually missing out on something life-changing".[36] (Corregido 2026-10-23 por t_5758d57c: la frase "fancy graphs I'll never look at" no está respaldada en el ledger [36] y se eliminó.)
Riesgo de marca: empujar el paywall sobre funciones que el usuario no percibe accionables erosiona la confianza incluso entre usuarios fieles.[36]

**Movilidad y tamaño de objetivo.** Un caso de rediseño documentado para ciclismo señala que "Buttons (placement, padding, and size), flow, and color scheme are the top areas of concern" y que para ver velocidad o distancia el ciclista "must tap the small icon in the bottom right" — un objetivo pequeño en un contexto de riesgo.[3] Refuerza el estándar de target size de Healthspan (44px) especialmente en pantallas de registro en movimiento.

## 2. WHOOP

**Home como hero de una métrica.** La Home es "the first thing you see when you open the WHOOP app", y WHOOP la rediseñó en 2025 "moving to a single scrolling view and adding a separate Health tab for longer-term health metrics".[5] El anillo triple unifica Recovery, Strain y Sleep como arcos concéntricos en el centro de la pantalla: "Users read their entire body status in one look without parsing three separate charts".[6] Color asignado a conceptos, no a estética: amarillo = Recovery, azul = Strain, morado = coach, gris = Sleep, "no legend ever needed".[6] Para Healthspan: un número hero por pantalla (el anillo de sesión/serie de hoy) y color semántico constante en todos los componentes del design system.

**Onboarding con calibración.** El onboarding de WHOOP es un flujo de registro seguido de un periodo de calibración de 7 días en el que las funciones se desbloquean gradualmente.[9] El problema documentado por su propia diseñadora: "Calibration is shown in 7 different ways without clear expectations or a timeline, leaving new users confused about their progress" y muchos usuarios "thought the app is 'broken'".[9] El objetivo de crecimiento es el "7 day perfect start".[9] Lección: si Healthspan muestra datos derivados (RM, volumen semanal, "carga recomendada"), debe explicitar cuándo empiezan a ser fiables con una línea de progreso clara, no con mensajes sueltos tipo "trends coming soon".

**Journal = registro de bajo fricción con recompana analítica.** WHOOP Journal permite registrar más de 300 comportamientos y ver su impacto fisiológico; se contesta como lista corta de preguntas sí/no cada mañana.[7] Límite deliberado: "Track 10 or fewer at a time. A short list is easier to answer every day, and consistent answers are what unlock insights".[7] Tres entradas al flujo (prompt matinal, card en Home, botón +) y edición retroactiva hasta 14 días.[7] Lección para Healthspan: el registro de entrenamiento debe ser una lista corta predecible (prefill de la última sesión), con corrección tardía fácil; el análisis (comparativas) llega después de datos consistentes, no antes.

**Coach conversacional.** Un estudio empírico (n=36, ACM MHCI 2025) sobre un agente conversacional en fitness tracker halló que puede "aid in the interpretation of tracker metrics" e influye en la percepción de comportamientos y rutinas.[11] El coach de WHOOP vive en la barra de navegación inferior, accesible en un clic desde el home.[8]

**Navegación.** WHOOP movió sus funciones más populares a una barra de navegación inferior fija ("just a single click away") tras feedback de miembros; el perfil de rendimiento se abre desde la foto de perfil en la esquina superior.[8] Coincide con la decisión de Healthspan: bottom nav móvil con destinos únicos, avatar = cuenta.

## 3. Hevy

**El log como núcleo.** Hevy se vende como "A workout tracking app that just works" con logging intuitivo, planificador de rutinas, temporizadores de descanso automáticos y PRs.[13] El rest timer es automático y configurable por defecto y por ejercicio, disponible tanto en workout en vivo como al construir una rutina.[12] Lección: el timer de descanso no es un extra, es parte del flujo de log en el gimnasio; Healthspan debería heredarlo como comportamiento por defecto silencioso (no como opción que hay que descubrir).

**Social como capa opcional.** El feed muestra workouts ajenos con nombre, duración, volumen, PRs y media de frecuencia cardíaca, con likes y comentarios.[31] Los atletas sugeridos aparecen en un carrusel y pueden desactivarse en ajustes de privacidad.[31] Sentimiento de comunidad: usuarios que no usan lo social lo mencionan explícitamente como motivo de fuga ("I don't really use the social features and I'm not a fan of subscription fitness apps").[37] Lección: el social debe ser desactivable desde el primer día; personal-first (regla 6 de ux-ui-good-apps).

**Trainer (doble persona).** El update oficial de producto de Hevy anuncia "Trainer" y una renovación nativa de navegación con foco en "logging mid-workout, navigating exercises, celebrating your monthly achievements", bajo la filosofía "keep the app simple… add value without making your workout more complicated".[15] Es la validación de mercado del patrón Airbnb host/guest que Healthspan aplica con trainer↔trainee: mismos componentes, vistas distintas.[15]

**Monetización.** El teardown de onboarding de ScreensDesign documenta splash oscuro, signup con Apple/Google/email, y un paywall comparando planes mensual, anual y lifetime con el anual preseleccionado.[32] Hevy presume de 17M+ de atletas y 4.9 de rating mostrando el log como héroe en el onboarding.[13]

## 4. Fitbod

**Recomendación como propuesta, no como wizard.** Fitbod entrega "fully customized workouts based on your goals, fitness level, and available equipment" mediante un algoritmo propietario.[16] Su onboarding abre un trial de 7 días con acceso completo y sin datos de pago, con timeline explícito del trial (recordatorio día 5, fin día 7).[17] Lección: si Healthspan añade sugerencia de peso/series, debe presentarse como propuesta editable (el usuario confirma, no rellena formularios), y cualquier trial debe ser honesto sobre cuándo termina.

## 5. TrainingPeaks

**Calendario como superficie principal.** TrainingPeaks es "a comprehensive platform for endurance athletes and coaches" que funciona como "detailed training log, a powerful analytics engine, and a planning tool all in one".[19] Su free tier permite poblar el calendario y ver datos antes de pagar funciones avanzadas.[19] Es el modelo "plan-first" (lo que Healthspan NO es hoy): el valor gratuito es el log y el calendario; el pago está en el análisis y el coaching. Encaja con la regla de Healthspan de no construir rutinas sobre un modelo pobre (nombre+freq) hasta tener prescripciones ordenadas.

## 6. Apple Fitness

**Lenguaje de diseño.** Apple Fitness organiza el movimiento diario en el sistema de tres anillos.[21] Su dashboard resumen permite añadir, quitar y reordenar widgets para priorizar las métricas que importan.[21]
El goal setting de doble horizonte permite un ajuste temporal del día "without disrupting their main goal" — comprensión de la no-linealidad del entrenamiento.[21]
Los Apple Design Awards 2026 premian en fitness la visualización de datos legible de un vistazo (Training Readiness de The Outsiders como "a beautiful data visualization that shows training load alongside metrics such as sleep quality and resting heart rate").[22]

**Accesibilidad como requisito.** Apple lista cinco funciones visuales que una app puede soportar: Sufficient Contrast, Dark Interface, Larger Text, Differentiate Without Color Alone, Reduce Motion.[23] La accesibilidad de Fitness+ sigue siendo imperfecta para ciegos/baja visión incluso tras añadir Audio Hints.[24] Lección para Healthspan (dark-only ya decidido): asegurar contraste, texto grande (Dynamic Type-equivalent) y nunca color como única señal — coherente con la regla de error "color+icon+text".

## 7. Estándares de accesibilidad aplicables

- WCAG 2.2 SC 2.5.8 Target Size (Minimum), nivel AA.[26]
- Contraste mínimo de texto 4.5:1; texto grande 3:1.[27]
- Apple HIG: controles de al menos 44×44 pt.[30]
- Android/Material: targets de 48×48 dp con ≥8 dp de separación.[29]

Healthspan v1 usa 44px como suelo; en listas densas de sets/reps conviene 48dp y separación explícita, porque el contexto de uso (guantes, sudor, gimnasio) penaliza objetivos pequeños.[unverified]

## 8. Patrones comunes (síntesis)

1. **El log es el producto.** Strava, Hevy y TrainingPeaks monetizan sobre un log impecable; el social y el análisis son capas.[1][13][19]
2. **Social como subproducto del log, opcional y desactivable.** Nadie construye un feed que el usuario tenga que "alimentar".[1][31][37]
3. **Streaks con barra baja y ventana tolerante** (Strava cuenta semanas).[2]
4. **Una métrica hero + color semántico fijo** en el dashboard (WHOOP triple ring; Apple three rings).[6][21]
5. **Onboarding que promete un resultado con timeline honesto** (calibración WHOOP, trial Fitbod sin tarjeta).[9][17]
6. **Paywall sobre análisis avanzado, no sobre el acto de registrar** (Strava segments, TrainingPeaks calendar); el paywall agresivo sobre funciones poco percibidas genera rechazo visible.[35][36] TrainingPeaks es el contraejemplo moderado.[19]
7. **Doble persona en un mismo sistema** (Hevy Trainer) valida el patrón trainer↔trainee de Healthspan.[15]

## 9. Implicaciones concretas para Healthspan

- Proteger la activación "registrar el entreno de hoy en un gesto": es exactamente el núcleo que comparten Strava, Hevy y TrainingPeaks.[1][13][19]
- Bottom nav de Healthspan (5 slots + FAB) es la misma decisión que WHOOP tomó por feedback de miembros; mantener destinos únicos.[8]
- Para el editor de rutinas trainer↔trainee: seguir el modelo Hevy Trainer — mismas vistas de log, permisos distintos, sin segundo flujo.[15]
- Rest timer automático como default silencioso en la sesión de gym, heredado de Hevy.[12]
- No añadir streaks/insignias hasta que el log diario sea el hábito; cuando lleguen, ventana semanal tipo Strava.[2]
- Visualización: un hero por pantalla con color semántico constante (paleta v1: accent #1E6FFF, done #2FBF71, PR #F5B83D), coherente con WHOOP/Apple.[6][21] El accent #1E6FFF debe declararse color propio de Healthspan, no "de Hevy" (ver §10).
- Riesgo a vigilar: cualquier paywall futuro debe quedar fuera del acto de registrar y del historial propio.[35][36]

## 10. Relación con la verificación QA (docs/design/benchmark-citations.md, t_b6020ed5)

QA verificó por medición de píxeles sobre capturas oficiales de App Store las atribuciones de DESIGN.md v1. Tres hallazgos condicionan este benchmark:

1. **Hevy NO es dark-first**: sus 6 capturas oficiales son UI clara (71–85 % píxeles claros); el tema oscuro es elección del usuario. El splash oscuro que documenta ScreensDesign[32] es solo la pantalla de arranque, no el tema por defecto. El benchmark de "dark-first" debe apoyarse en WHOOP (dark-native, 72–94 % oscuro medido), no en Hevy.
2. **#1E6FFF no es el azul de Hevy** (el real medido es #267EE8); los tokens v1 proceden de un pack "reverse-engineered" de terceros, no de Hevy. Consecuencia: mantener la paleta v1 como decisión propia de Healthspan y eliminar la atribución a Hevy.
3. **Rampa de readiness**: WHOOP oficial usa #16EC06/#FFDE00/#FF0026; los hexes de DESIGN.md son adaptación propia y deben declararse como tal.

Este informe adopta esas correcciones: las lecciones de Hevy aquí son de flujo (log-first, rest timer automático, social desactivable, Trainer dual-persona), no de tema visual.

## Cobertura y huecos

- Medium (case study Strava runner), listings de App Store (Hevy, TrainingPeaks) y uxsnaps.com/strava-feed-and-social no fueron extraíbles (403/502); quedan registrados en el ledger pero sin citas en este informe — sus afirmaciones no se usan.[unverified]
- El estudio Chalmers es un prototipo de investigación, no la app WHOOP Coach de producción.[11]
- El sentimiento de Reddit es evidencia de percepción de usuarios, no de métricas de Strava.[36][37]

## Sources

[1] https://appservatory.com/case-studies/strava — Appservatory — Strava mechanics case study
    > "Strava builds its feed out of recorded activities rather than written posts, so items appear without anyone composing them."
    > "Recommended users sit at the top of the feed, so an account that follows nobody still opens to a populated stream."
    > "Six mechanics carry the loop"
[2] https://gamification.gamebizconsulting.com/case-studies/strava — Mechanics Playbook — Strava gamification case study
    > "Six mechanics carry the loop"
    > "Strava counts its streak in weeks rather than days, so the state depends on logging an activity inside a 7-day window rather than on returning every day."
    > "Saving it publishes an item to the home feed, where other users can give kudos or leave a comment."
[3] https://stevenmerickson.com/strava.html — Steven Merrickson — Strava cycling redesign case study
    > "Buttons (placement, padding, and size), flow, and color scheme are the top areas of concern for cyclists."
    > "To see speed, distance traveled, or time elapsed, a cyclist must tap the small icon in the bottom right."
[5] https://www.whoop.com/us/en/thelocker/the-all-new-whoop-home-screen — WHOOP Locker — Home screen official
    > "Home screen is the first thing you see when you open the WHOOP app."
    > "Home screen in 2025 based on member feedback, moving to a single scrolling view and adding a separate Health tab for longer-term health metrics."
[6] https://www.uxsnaps.com/daily-health-dashboard — UX Snaps — 5 UX lessons from WHOOP's dashboard
    > "Recovery, Strain, and Sleep coexist as concentric arcs in a single ring at the center of the screen."
    > "Users read their entire body status in one look without parsing three separate charts."
    > "Yellow always means Recovery. Blue always means Strain. Purple always means brand or coach. Grey always means Sleep."
    > "Color is used with absolute consistency across the entire app, no legend ever needed."
[7] https://www.whoop.com/us/en/thelocker/the-whoop-journal — WHOOP Locker — Journal official
    > "The WHOOP Journal is a feature that lets you log more than 300 daily choices and behaviors and see how they impact your physiological data."
    > "Track 10 or fewer at a time. A short list is easier to answer every day, and consistent answers are what unlock insights."
[8] https://www.whoop.com/us/en/thelocker/app-update-navigation-bar — WHOOP Locker — All-new navigation bar
    > "moving to a single scrolling view and adding a separate Health tab for longer-term health metrics"
    > "We’ve taken several of our most popular features and put them all in one easy-to-locate place that’s just a single click away: The all-new navigation bar that now lives at the bottom of your mobile app screen"
    > "Your performance profile, which contains data streaks, 30-day averages, all-time highs and lows, and more, can be viewed by clicking on your profile photo in the upper-left corner of the home screen."
[9] https://www.ramona-mengxi.com/whoop-growth-onboarding — Ramona Mengxi — WHOOP onboarding/calibration redesign (designer)
    > "Calibration is shown in 7 different ways without clear expectations or a timeline, leaving new users confused about their progress."
    > "Many users didn’t understand this and thought the app is “broken”."
    > "We aim to increase the number of members who achieve a 7 day perfect start." (quote añadida 2026-10-23 por t_5758d57c; verbatim en ramona-mengxi.com, verificado en vivo por QA t_32f4fbb4)
[11] https://research.chalmers.se/publication/548305/file/548305_Fulltext.pdf — Chalmers MHCI — WHOOP Coach conversational agent study
    > "findings on how a CA can aid in the interpretation of tracker metrics and how it influences users’ perceptions of their behaviours and routines"
[12] https://www.hevyapp.com/features/workout-rest-timer — Hevy — rest timer feature page
    > "rest timer is available for any exercise you add to a live workout or while building a routine (reusable workout template)."
    > "Hevy offers a customizable and automatic workout rest timer to monitor your rest between sets and exercises."
[13] https://www.hevyapp.com — Hevy — official site
    > "A workout tracking app that just works. Everything you need to log your workouts and smash your goals."
    > "Loved by 17+ million athletes." (quote añadida 2026-10-23 por t_5758d57c; verbatim en hevyapp.com, verificado en vivo por QA t_32f4fbb4)
    > "4.9 App Store & 4.9 Google Play (630,000+ ratings)." (quote añadida 2026-10-23 por t_5758d57c; verbatim en hevyapp.com, verificado en vivo por QA t_32f4fbb4)
[15] https://www.reddit.com/r/Hevy/comments/1umaz02/hevy_community_update_1_trainer_native_redesign — Hevy community update #1 (Trainer, native redesign)
    > "UX polish & smoother navigation.** Lots of small improvements to the moments that matter most: logging mid-workout, navigating exercises, celebrating your monthly achievements."
    > "Our philosophy is to keep the app simple, so every feature gets a lot of thought before it ships: the goal is always to add value without making your workout more complicated."
[16] https://www.fitbod.me — Fitbod — official site
    > "Fitbod is a personalized strength training app that delivers fully customized workouts based on your goals, fitness level, and available equipment."
[17] https://www.botsi.com/resources/flows/apps/fitbod — Botsi — Fitbod onboarding flow teardown
    > "a 7-day free trial starts automatically with full access and no payment info."
[19] https://screensdesign.com/showcase/trainingpeaks — ScreensDesign — TrainingPeaks UX breakdown
    > "TrainingPeaks is a comprehensive platform for endurance athletes and coaches."
    > "It serves as a detailed training log, a powerful analytics engine, and a planning tool all in one."
    > "This approach allows users to populate their calendar and see their data before deciding to upgrade for more advanced features."
[21] https://screensdesign.com/showcase/apple-fitness — ScreensDesign — Apple Fitness UI breakdown
    > "At its core, the app tracks daily movement, exercise, and standing time through its signature three-ring system."
    > "users can make a temporary adjustment for the day without disrupting their main goal."
    > "Summary dashboard (00:48), which allows users to add, remove, and reorder widgets to prioritize the metrics that matter most to them."
    > "Fitness+ subscription service, offering a vast library of on-demand workout videos and guided meditations led by professional trainers."
[22] https://developer.apple.com/design/awards — Apple Design Awards 2026
    > "The Outsiders* distinguishes itself with its Training Readiness Score, a beautiful data visualization that shows training load alongside metrics such as sleep quality and resting heart rate."
    > "The app also lets athletes set training goals across dozens of workout types, making it an especially versatile way to train with more intention."
    > "also supports Dynamic Type, Increased Contrast, and Differentiate Without Color to enable people of all abilities"
[23] https://developer.apple.com/videos/play/tech-talks/111433 — Apple Tech Talk — Accessibility Nutrition Labels (Dynamic Type, Dark Mode, contrast)
    > "Sufficient Contrast, Dark Interface, Larger Text, Differentiate Without Color Alone, and Reduce Motion."
[24] https://afb.org/aw/jan2023/apple-fitness-plus-revisited — AFB AccessWorld — Apple Fitness+ accessibility revisited
    > "In March 2022, Apple added “Audio Hints” to their Fitness+ Workouts."
    > "This new feature is designed specifically to make workouts accessible to people who are blind or low vision."
[26] https://www.w3.org/TR/WCAG22 — W3C WCAG 2.2 specification
    > "WCAG 2.2 success criteria are written as testable statements that are not technology-specific."
    > "Target Size (Minimum)"
[27] https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html — W3C Understanding SC 1.4.3 Contrast (Minimum)
    > "The visual presentation of text and images of text has a contrast ratio of at least 4.5:1"
    > "Large-scale text and images of large-scale text have a contrast ratio of at least 3:1"
[29] https://developer.apple.com/design/tips — Apple Design tips: Hit targets
    > "Consider making sure these elements have a width and height of at least 48dp, as described in the Material Design Accessibility guidelines"
    > "Create controls that measure at least 44 points x 44 points so they can be accurately tapped with a finger."
[30] https://developer.apple.com/design/human-interface-guidelines/buttons — Apple HIG: Buttons
    > "a button needs a hit region of at least 44x44 pt — in visionOS, 60x60 pt — to ensure that people can select it easily"
    > "Create controls that measure at least 44 points x 44 points so they can be accurately tapped with a finger."
[31] https://www.hevyapp.com/features/content-feed — Hevy — social content feed feature page
    > "content feed (or social feed) shows updates from users you follow."
    > "You can turn off suggested athlete recommendations by navigating to the settings"
[32] https://screensdesign.com/apps/hevy-workout-tracker-gym-log — ScreensDesign — Hevy onboarding/paywall teardown
    > "Hevy sign-up form with pre-filled, validated email, password, and username fields, a legal disclaimer, and a Continue button."
    > "paywall comparing monthly, yearly, and lifetime plans, with the yearly plan selected and a subscribe button"
[34] https://www.strava.com/subscribe — Strava — official subscription page (free vs subscription feature table)
    > "See your progress, spot trends and get faster with real-time insights."
    > "Set goals, climb leaderboards and rally your friends with custom challenges."
[35] https://velo.outsideonline.com/road/road-racing/strava-shake-up-major-changes-for-paid-and-free-users — Velo — Strava paywall shake-up
    > "Placement on a segment’s leaderboard will remain free for everyone."
    > "But analysing them in detail will now be available to subscribers only."
[36] https://www.reddit.com/r/Strava/comments/1qd2d69/is_it_just_me_or_is_the_strava_subscription — r/Strava — subscription value sentiment thread (Jan 2026)
    > "But lately, the "trial" pop-ups are getting aggressive, and I’m starting to wonder if I’m actually missing out on something life-changing"
    > Nota (t_5758d57c): la frase "fancy graphs I'll never look at" NO está respaldada en este ledger y el hilo de Reddit no es accesible sin login (old.reddit, verificado 2026-10-23); no citarla.
[37] https://www.reddit.com/r/Hevy/comments/1qqva2d/i_dont_really_use_the_social_side_of_hevy_and — r/Hevy — user sentiment on social features/subscriptions
    > "lifting with Hevy for a while, but realized I don’t really use the social features and I’m not a fan of subscription fitness apps."
