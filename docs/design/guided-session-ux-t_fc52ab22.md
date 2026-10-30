# Flujo UX guiado: sesión de fuerza en móvil — wireframes y estados

**Tarjeta:** t_fc52ab22 · **Autor:** UX · **Date:** 2026-10-28
**Ground truth (vinculante, no re-decidido):** spec MVP `mvp-guided-session-t_b1e9048b.md` (adjunto de t_b1e9048b, §2 snapshot, §3 API, §4 estados FE, §6 aceptaciones) + design system v1 `docs/design/DESIGN.md` + stack actual (`ActiveSessionState`, `/sync` offline-first OPFS, chips sync de `Pages/Exercises/TodayWorkout.razor`, FAB verde finalizar de entreno en curso, nav 5 slots).
**Wireframes:** `docs/design/prototypes/guided-session-t_fc52ab22.html` (10 frames, tokens verbatim, componentes nuevos marcados `nuevo` con outline punteado) + screenshots `guided_session_t_fc52ab22_{wide,mobile390}.png`.

## 0. Decisiones heredadas (no se re-deciden aquí)

- Zero nuevas entidades; sesión = `Workout` de hoy + snapshot efímero; serie = fila `Exercise` con `SourceTemplateExerciseId`; rep = contador FE local solo.
- Interacción de contador vinculante: toque = +1 rep (zona ≥44px), "Serie hecha" = un toque grande que commitea → op `complete_set`. Corrección = `undo_set` únicamente. Descanso = countdown local (`restSeconds`), skippable, `role="timer"`.
- Badges "Plan firmado por {coach}" solo con `coachSigned` (snapshot en creación).
- Copias de error de §3.6 del MVP; chip sync `synced / pending (n) / problem (retry)`; taps nunca perdidos.
- Cortes MVP respetados: sin animaciones (imagen estática `ImageRoute`), sin superset expansion, timer = countdown simple.

## 1. Arquitectura de información (dónde vive cada cosa)

| Pantalla | Ruta | Estado actual | Qué cambia |
|---|---|---|---|
| Hoy (`/training/today`) | entrada 1 | tarjeta de sesión manual + FAB | **nuevo:** tarjeta-activación "Plan de hoy · {coach}" con CTA `▶ Empezar sesión de hoy` (1 toque, sin wizard — misma regla que "Empezar entreno"). Si hay sesión activa hoy, la tarjeta cambia a `Seguir sesión en curso` (mismo destino). |
| Mis rutinas (`/training/routines`) | entrada 2 | cards de rutina | **nuevo:** chip "✍ Firmada por {coach}" en la card si `TrainerId != null`; botón `▶ Sesión de hoy` en la card. |
| **Sesión guiada** | **`/training/session`** (nueva) | — | pantalla única de principio a fin de la sesión. Hoy-only → sin id en la ruta; `GET /sessions/current` o reintentar `from-plan` (idempotente por `DailySessionKey`) resuelve/retoma. |
| Detalle de ejercicio | sheet sobre `/training/session` | — | no es ruta: bottom sheet con imagen grande, tip completo y mapa muscular. |
| Registro (Historial) | existente | — | cero cambios: cada serie commiteada ya es visible en el log al instante. |

Regla de navegación: durante la sesión guiada la bottom bar sigue activa (el usuario puede irse y volver; `/training/session` retoma), pero el FAB pasa a conducta de "entreno en curso": dentro de la sesión, FAB verde = `Finalizar` (consistente con el FAB verde finalizar actual).

**Por qué una sola pantalla con stepper de un ejercicio:** la tarea motriz es "ejecuta lo que el coach escribió"; cada pantalla extra durante una serie es fricción con mancuernas en la mano. Un slot visible + contador a pulgar = el mínimo que cubre el story 2–3 del MVP.

## 2. Flujo principal y transiciones

```
Hoy / Mis rutinas ──▶ [A] tarjeta-activación
   │ toque único POST /sessions/from-plan (idempotente)
   ├─ 201 activo ─────────────▶ [D] SESIÓN (slot 1)
   ├─ 200 resumed ────────────▶ [D] con progreso real
   ├─ 404 routine_not_found ──▶ toast + quedarse en origen
   ├─ 400 plan_empty ─────────▶ [B] vacío
   └─ red offline ────────────▶ [D] con [S] pending; POST se encola en OPFS

[D] SESIÓN — bucle por slot:
   reposo? → [F] countdown (auto al terminar; "Saltar" o "+30s")
   → [E] contador reps (+1 zone, big commit)
   → toque "Serie hecha" → op complete_set (instantáneo, feedback local)
        ├─ quedan series del slot → [F] descanso → [E]
        ├─ slot completo (dots ✓) → auto-avance al siguiente slot [D]
        └─ último slot completo → FAB "Finalizar" pulsante → [G] resumen
[D] ⇄ sheet [C] detalle ejercicio (toque en nombre/imagen del slot)
"Deshacer serie" ([E], enlace tertiary) → undo_set; nunca corrige reps sin borrar serie
```

Transiciones con justificación de resultado de usuario:
- **Descanso → siguiente serie automático:** elimina un toque con manos ocupadas; el countdown ya corriendo es la señal.
- **Commit de serie = feedback inmediato local, sync en fondo:** p50 time-to-log no depende de la red; la veritat local manda (§4 MVP).
- **Auto-avance de slot:** el trainee nunca busca "¿qué sigue?"; el progreso segmentado arriba responde siempre.
- **Al finalizar → resumen y vuelta a Hoy en contexto:** mismo patrón que el flujo manual actual (nunca aterrizar en catálogo).

## 3. Estado de UI (máquina de la pantalla `/training/session`)

`loading → active ⇄ (executing | resting) → finishing → completed → resumen`; laterales: `empty-plan`, `error`, y el cross-cutting `offline/pending` que nunca bloquea la interacción.

| Estado | Disparador | Render | Interacción |
|---|---|---|---|
| loading | GET/POST en vuelo | skeleton de card + contador; copy "Cargando entrenamiento…" (8s cap, retry ≤3 backoff 1/2/4s) | bloqueado |
| active/executing | slot con series pendientes | slot card + contador + commit | todo |
| resting | tras `complete_set` | countdown ring, "Saltar" + "+30s" | commit deshabilitado |
| slot done | setsDone == sets | fila de dots ✓ + nombre, wash done 16% | abre sheet detalle |
| session done ready | todas las series | FAB verde "Finalizar" pulsante | confirmar |
| completed | `POST /workouts/{id}/complete` 2xx | resumen [G] | solo volver |
| empty-plan | 400 `plan_empty` | card [B] CTA "Ver mis rutinas" | salir |
| error | 4xx/5xx no retryable | inline card con copy §3.6 | según tabla |
| offline/pending | sin red | chip `PENDING (n)`; contador sigue funcionando | todo (OPFS) |

Chip sync (compartido con el log actual — mismos estados, mismo componente): `SYNCED` verde cuando cola vacía · `PENDING (n)` tertiary · `PROBLEMA (REINTENTAR)` err con acción; solo entra desde `failed[]` de `/sync`. Si `failed.reason == session_completed` → la sesión ya acabó en otro dispositivo: chip err + recarga snapshot → resumen.

## 4. Wireframes (frames A–J en el HTML)

- **A Tarjeta-activación (Hoy / rutinas):** mini-hilo de 3 avatares de ejercicio, "Plan de hoy · {n} ejercicios · {series} series", chip `✍ FIRMADO POR {COACH}` (solo `coachSigned`), CTA grande. Justifica: un toque hasta la tarea (story 2).
- **B Vacío (`plan_empty`):** ilustración hairline + copy en español «Aún no hay entreno de hoy» + meta «Tu coach todavía no ha programado esta rutina. Cuando lo haga, aparecerá aquí.» + CTA "Ver mis rutinas". (Adaptación al español del copy §3.6 `"Aún no hay workouts plan in this routine."` — el wireframe fija el microcopy para los mockups.) Nunca un contador 0 engañoso.
- **C Sheet detalle ejercicio:** imagen (`ImageRoute`, null-safe → placeholder hairline), prescripción `4×8 · 60 kg`, tip = `Notes` del coach (fallback `ExerciseType.Description`), mapa muscular: silhouette front/back según `MuscleSide`, **primario = relleno, secundario = contorno**, leyenda "Primario: cuádriceps · Secundario: glúteos". Vista de reposo (sin prisa por ejecutar) → no interfiere con el contador.
- **D Pantalla de sesión (slot actual):** arriba back + chip sync + título "Sesión de hoy" + subtítulo `✍ Plan firmado por {coach}`; progreso segmentado por slot (fill done por serie completada) + línea "Series {x}/{planned} · {tiempo}"; card del slot: thumb 52px + nombre accent + `4×8 · 60 kg` + dots de serie (○●✓) + "Ver detalle ▾".
- **E Contador en vivo:** número hero 76/800 tabular (reps del intento actual), zona grande "+1 REP" (≥120px alto, target ≥44px garantizado), contador secundario "reps 5/8 · serie 2/4", CTA grande `✓ SERIE HECHA`, enlace tertiary `Deshacer serie` (undo_set, con confirmación solo si reps>0: "¿Borrar la serie {n}? Se elimina del registro."). Toque fuera de zona = nada (QA: 1 toque = 1 rep).
- **F Descanso:** ring countdown MM:SS `role="timer"`, accent; label "DESCANSO · {siguiente ejercicio}"; botones `+30s` y `Saltar`. Al 0 → vib/flash sutil + auto-volver a E. Nunca bloquea volver a D.
- **G Confirmación/fin:** CTA `Finalizar sesión` (pulsante cuando todo hecho) → sheet de confirmación con recap `[S]` ("Series registradas {x}/{y} · sincronización: SYNCED") → `complete` → resumen (duración hero, series, adherencia vs plan `Σ setsDone/Σ setsPlanned`) → "Volver a Hoy". Si quedan pending, el recap dice `PENDING (n)` y **aun así** permite finalizar (OPFS no pierde taps).
- **H/H2 Estado offline y error:** banner hairline "SIN CONEXIÓN — tus toques se guardan" + chip `PENDING (n)`; card `PROBLEMA (REINTENTAR)` con copy §3.6 y sesión **usable** (los contadores son la verdad local).
- **I Sesión terminada (recarga/otro dispositivo):** «Fin del entreno — ya está en tu Historial» (adaptación del copy §3.6 `"End of training; see log."`) → CTA ver Historial. 409 `session_completed` en cualquier op.

## 5. Logging — eventos necesarios para coherencia (sin nueva tabla de eventos)

Principio MVP §5: la instrumentación se deriva de **registros existentes**. Todo lo server-side persistido ya existe; estos son los anclajes canónicos que UX necesita nombrar para métricas §5 y para QA:

| Evento UI | Artefacto persistido (existente) | Campos clave para métricas |
|---|---|---|
| `guided.start` | `POST /sessions/from-plan` → `Workout.startedAt` | `clientSessionId`, 201 vs 200-resumed |
| `set.commit` | op `/sync` `complete_set` | `tap_ts` (local en payload meta), `applied_at` server → time-to-log p50/p95; `slotId` → adherencia |
| `set.undo` | op `/sync` `undo_set` | calidad del contador (ruido de +1) |
| `session.finish` | `POST /workouts/{id}/complete` → `CompletedAt` | completion rate; early-drop = primer `set.commit` ≤15min de `startedAt` |
| `sync.result` | respuesta `/sync` `applied/deduped/failed` | chip states; `pending (n)` en offline |

Locales-efímeros (no se envían; contracto de nombres para el FE y para futuros funnels — no persistidos en MVP): `rest.start / rest.skip / rest.complete`, `session.abandon` (salir con sesión activa hoy), `sheet.open`. Regla: **ningún contador de reps toca el server hasta el commit** (MVP §1.5); el logging de sesión guiada es el mismo pipeline `/sync` + una acción nueva, no un segundo sistema.

## 6. Accesibilidad y touch targets (acceptación QA)

- Zona +1 y CTA serie: ≥44px (la zona +1 ocupa el ancho completo del frame; target real medido en el prototipo).
- Countdown con `role="timer"`, `aria-live="off"`; anuncio sr-only en 0 ("Descanso terminado").
- Número hero `aria-hidden="true"` + status sr-only «Serie {n} de {total} · {reps} reps de {objetivo}» (evita spam tabular al +1).
- Chips de sync con color + texto (no solo color).
- Back de la sesión: nombre accesible «Volver a Hoy» (el glifo ‹ es decorativo).
- Tap outside no registra reps (no hay zonas fantasma).

## 7. Riesgos / siguiente paso

- El SVG de bodymap del intento previo (asset `guided_session_bodymap.json` en el workspace de la tarjeta) **no separa los 7 `MuscleGroup` por path**: o se corta por grupo (asset nuevo, tarea FE/diseño aparte) o el mapa MVP usa silueta genérica + leyenda de texto. Recomendado MVP: silueta + relleno solo si el asset por grupo existe; la leyenda "Primario: … · Secundario: …" es obligatoria en ambos casos.
- Siguiente: mockups hi-fi sobre estos frames (mismo HTML, quitar outline `nuevo`), y revisión de QA t_e34941fe contra §3/§4 del MVP.
