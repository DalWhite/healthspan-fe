# Handoff técnico FE — Sesión guiada (t_972dab41)

**Para:** implementación FE (asignaturas: tarjetas FE1/FE2/FE3 + B1 API)
**Ground truth vinculante (en este orden si hay conflicto):**
1. Spec MVP `mvp-guided-session-t_b1e9048b.md` (adjunto t_b1e9048b) — contratos §2/§3, estados §4, métricas §5. Cerrada; no se re-decide aquí.
2. Spec funcional UX `docs/design/guided-session-ux-t_fc52ab22.md` — máquina de estados, flujo, logging, a11y. Pendiente de enmendar por t_700714bc (FIX-1..5); este doc ya incorpora las correcciones y la decisión FIX-4.
3. Mockups hi-fi `docs/design/prototypes/guided-session-hifi-t_eb3ab918.html` (11 frames, 390×844) — valores visuales literales. **Nota: el HTML aún pinta tokens v1; tras FIX-4 los hex de §5 de este doc mandan.**
4. Este doc — decisiones de consolidación, orden de implementación y acceptance final.

---

## 0. Decisiones tomadas por el lead (cerradas, no re-debatir en workers)

| # | Decisión | Justificación |
|---|---|---|
| D1 | **FIX-4 → tokens "v1.1" scope sesión guiada** (ver §5). `--hs-accent` #1E6FFF queda solo para elementos NO textuales. | Ratios verificados (cálculo WCAG propio, coincide con QA): blanco sobre #1E6FFF = 4.40 FAIL; #4D8CFF sobre card = 5.36 PASS; #F87077 sobre chip-wash error = 5.18 PASS. |
| D2 | Migración global de tokens del resto de la app = **backlog aparte**, no bloquea sesión guiada. | Los CTAs actuales de la app ya fallan AA; se decide prioridad por separado. |
| D3 | **No existe `GET /sessions/current`.** Superficie API cerrada = §3 del MVP: `POST /sessions/from-plan`, `GET /sessions/{id}`, ops `complete_set`/`undo_set` vía `/sync`, `POST /workouts/{id}/complete`. Retomar sesión = re-POST `from-plan` (idempotente → 200 `resumed`). El FE persiste `sessionId` en OPFS para deep-link. | La superficie cerrada es contrato; `current` abriría una segunda fuente de verdad. |
| D4 | **FIX-2: after completed**, `from-plan` con sesión de hoy completada → **409 `session_completed`** (no crea nueva, no reabre). FE renderiza frame I. Mañana → sesión nueva. | Coherente con §3.4 del MVP ("next day new session"). |
| D5 | **FIX-1: `tap_ts` va dentro del `payload`** de la op (el envelope real de `SyncOperationPayload` tiene exactamente `opId/entity/action/clientEntityId/payload` — sin campo `meta`). El server lo ignora para aplicar la op; se usa para time-to-log. | Verificado en `Controllers/ApiContracts/Sync/SyncContracts.cs`. |
| D6 | **Microcopy final en español** (tabla §7). El MVP §3.6 trae copies en inglés; UX ya fijó español y el producto es ES. | Estándar de la app. |
| D7 | Mapa muscular MVP = **silueta SVG inline genérica + leyenda de texto obligatoria**; relleno/contorno por grupo solo si existe asset por `MuscleGroup` (no existe hoy). | Riesgo ya flaggado en UX §7; la leyenda garantiza la información siempre. |

---

## 1. Inventario de componentes FE

### Reutilizados (existen en main, verificar al tocar)
| Componente | Uso en sesión guiada | Cambio |
|---|---|---|
| `Shared/SyncStatusIndicator.razor` | chip sync en header de sesión | **Localizar a español** (`SYNCED / PENDIENTE (n) / PROBLEMA (reintentar)`); mismo componente, mismos estados |
| `Offline/SyncStatusService.cs` + pila OPFS | cola de ops, replay, dedupe | sin cambios de mecanismo; añadir acciones `complete_set`/`undo_set` |
| `Services/ActiveSessionState.cs` | sesión manual en curso | no tocar (regla MVP: no romper logging manual); el FAB verde en sesión guiada sigue su conducta de "entreno en curso" |
| `Pages/Exercises/TodayWorkout.razor` (`/training/today`) | entrada 1 | + tarjeta-activación (frame A) con CTA 1-toque; si sesión activa hoy → `Seguir sesión en curso` |
| `Pages/Routines/MyRoutines.razor` (`/training/routines`) | entrada 2 | + chip `✍ FIRMADO POR {coach}` si `TrainerId != null` + botón `▶ Sesión de hoy` |
| `Components/FinishSessionDialog.razor` | confirmar fin (frame G) | adaptar recap (series, adherencia, chip sync) |
| `wwwroot/css/app.css` | foco teclado | **ya existe `:focus-visible` global (líneas 92-131)** — FIX-5 = no anularlo en componentes nuevos; nada que añadir |
| Tokens CSS `--hs-*` en app.css | todo el visual | + 3 tokens v1.1 (§5) |

### Nuevos (todos en healthspan-fe)
| Componente | Ruta/ubicación | Frame | Notas clave |
|---|---|---|---|
| `Pages/Training/Session.razor` | `@page "/training/session"` | D–J | pantalla única; sin id en ruta (today-only); resume vía OPFS o `from-plan` |
| `Components/Guided/ActivationCard.razor` | dentro de Today/Rutinas | A/B | mini-hilo 3 avatares, chip coach, CTA; estado vacío B |
| `Components/Guided/SegBar.razor` | header sesión | D | un tramo por slot, relleno por series hechas |
| `Components/Guided/SlotCard.razor` | card ejercicio actual | D | thumb 52px (`ImageRoute`), nombre en `--hs-accent-text`, dots de serie ○●✓, `Ver detalle ▾` (h≥44) |
| `Components/Guided/RepCounter.razor` | zona de ejecución | E | hero 76/800 `aria-hidden` + sr-only status; plus-zone 316×120 target ≥44; CTA `✓ SERIE HECHA` (`--hs-accent-cta`); link `Deshacer serie` (undo_set; confirmación solo si reps>0) |
| `Components/Guided/RestRing.razor` | descanso | F | `conic-gradient`, `role="timer"` `aria-live="off"`, `+30s` / `Saltar` (158×46), auto-avance al 0 + sr-only "Descanso terminado" |
| `Components/Guided/DetailSheet.razor` | bottom sheet | C | `role="dialog"`, foto hero 170px `object-position:center 30%` con `alt` real, tip=`Notes` fallback `Description`, silueta SVG + leyenda obligatoria, botón cerrar con `aria-label` |
| `Components/Guided/MuscleSilhouette.razor` | dentro del sheet | C | SVG inline `.sil/.prim/.sec` (clases del hi-fi), front/back por `MuscleSide` |
| `Components/Guided/SessionSummary.razor` | post-completar | J | hero duración, series, adherencia `ΣsetsDone/ΣsetsPlanned`, chip sync, `Volver a Hoy` |
| `Components/Guided/CompletedCard.razor` | estado 409 | I | «Fin del entreno — ya está en tu Historial» + CTA Historial |

Regla de estructura: nada de esto toca `TodayWorkout` más allá de montar `ActivationCard`; cero cambios en la ruta de logging manual.

## 2. Máquina de estados (binding)

`loading → active ⇄ (executing | resting) → finishing → completed` + laterales `empty-plan`, `error`, y cross-cutting `offline/pending` que **nunca bloquea interacción**.

| Estado | Disparador | Render | Interacción |
|---|---|---|---|
| loading | GET/POST en vuelo | skeleton + «Cargando entrenamiento…» | bloqueado; cap 8s, retry ≤3 backoff 1/2/4s |
| executing | slot con series pendientes | SlotCard + RepCounter | todo |
| resting | tras commit de serie | RestRing | commit deshabilitado; skippable |
| slot done | setsDone==sets | dots ✓ + wash done 16% + auto-avance | abre sheet detalle |
| session done | todas las series | FAB verde Finalizar pulsante | confirmar (sheet recap) |
| completed | `POST /workouts/{id}/complete` 2xx o 409 `session_completed` | J o I | solo volver |
| empty-plan | 400 `plan_empty` | card B | «Ver mis rutinas» |
| error | 4xx/5xx no retryable | card inline copy §7 | según tabla |
| offline/pending | sin red | banner «SIN CONEXIÓN — tus toques se guardan» + chip `PENDIENTE (n)` | **todo** (OPFS encola `from-plan` y ops) |

Transiciones clave: descanso→siguiente serie automático; commit = feedback local inmediato, sync en fondo; auto-avance de slot; al finalizar → resumen en contexto (nunca catálogo). Si `failed[].reason == session_completed` → chip PROBLEMA + recarga snapshot → frame J.

## 3. Datos que necesita el FE

Snapshot `GuidedSession` (§2 del MVP) — campos exactos: `sessionId, routineId, templateId, coachSigned, coachName, state, version, startedAt, completedAt, slots[{slotId, order, exerciseTypeId, name, imageRoute, sets, reps, loadKg, restSeconds, tip, muscles{primary[], secondary[]}}], progress[{slotId, setsDone, repsLast, done}], totals{setsPlanned, setsDone}`.

Fuentes adicionales ya existentes: `GET /routines/plan?date=` para resolver `routineId` en deep-link sin OPFS; `ExerciseType.ImageRoute` → `/api/exercises/{id}/image` (null-safe → placeholder hairline); fotos demo del hi-fi = free-exercise-db, misma fuente del catálogo.

**Prerequisito backend (tarjeta B1):** `POST /sessions/from-plan` (201/200 resumed/400/404/409 ver D4), `GET /sessions/{id}` con `ETag: "v{version}"`, handlers de ops `complete_set`/`undo_set` en `/sync` (materializan `Exercise` con `SourceTemplateExerciseId`, soft-delete en undo, reasons `slot_not_in_plan|session_completed|slot_all_sets_done|not_your_session`), `tap_ts` en payload ignorado-permitido (D5). Verificado: **nada de esto existe hoy en healthspan-api** — solo `/sync` genérico y `today/sessions` GET.

## 4. Interacciones, anim y accesibilidad

- Toque en plus-zone = +1 rep (1 toque 1 rep, QA lo prueba); fuera de zona = nada. Contador de reps **nunca toca el server hasta el commit**.
- `✓ SERIE HECHA` → op `complete_set` (payload: `sessionWorkoutId|clientWorkoutId, slotId, reps, weightKg, bodyweightKg, notes, tap_ts`). Corrección = solo `undo_set` (`payload:{slotId}` borra última fila del slot), con confirmación si reps>0.
- Motion (hi-fi §Spec de movimiento): pop hero 120ms, dots stagger 60ms + wash, ring cónico lineal, slot slide 220ms `cubic-bezier(.2,.7,.3,1)`, FAB glow 1.8s. Tokens `--hs-t-fast/--hs-t-med`. **Todo OFF con `prefers-reduced-motion`; el feedback de commit (dot+wash+sr-only) funciona sin animación.**
- A11y (FIX-3, fixing en t_700714bc, el FE lo implementa ya): back `‹` = `<button>` con «Volver a Hoy»; plus-zone `<button>`; sheets `role="dialog"` + cerrar `aria-label`; hero `aria-hidden` + status sr-only «Serie {n} de {total} · {reps} reps de {objetivo}»; ring `role="timer"`; chip ✍ con texto nunca solo icono/color; foco: no anular el `:focus-visible` global de app.css.
- Targets ≥44px (medidos en hi-fi: back 44×44, CTA 326×50, plus-zone 316×120, descanso 158×46, detail-link h44, FAB 46×46).

## 5. Tokens color v1.1 (decisión D1 — hex literales)

| Token | Hex | Uso | Ratio verificado |
|---|---|---|---|
| `--hs-accent` (v1, sin cambio) | `#1E6FFF` | **solo no-texto**: rellenos, segbar, ring, FAB, foco, tabs | (UI comp ≥3:1 ✔) |
| `--hs-accent-cta` **nuevo** | `#1A5ACF` | fondo CTA con texto blanco | 6.16 vs #FFF |
| `--hs-accent-text` **nuevo** | `#4D8CFF` | texto/icono accent sobre card/canvas/input/accent-soft | 5.36 / 5.86 / 4.72 / 4.85 |
| texto error sobre chip-wash | `#F87077` | chip PROBLEMA (wash 16% de #F1545B) | 5.18 vs fondo #39242B (v1 #F1545B = 4.22 FAIL) |
| links interactivos | `#A9B1BE` (t2) o `--hs-accent-text` | «Deshacer serie», detail-links | 8.01 / 5.36 |
| `--hs-text-tertiary` `#78818F` | — | **solo meta no interactiva** (4.39 FAIL para texto/link) | — |

Chips semánticos v1 pasan: gold 6.63, done 5.53, PENDIENTE t2 sobre accent-soft 7.24. No cambiar.

## 6. Logging (sin nueva tabla de eventos)

| Evento UI | Artefacto persistido (existente) | Campos para métricas §5 MVP |
|---|---|---|
| `guided.start` | `POST /sessions/from-plan` → `Workout.startedAt` | `clientSessionId`; 201 vs 200-resumed |
| `set.commit` | op `/sync` `complete_set` | **`tap_ts` en payload** (D5) → time-to-log `applied_at − tap_ts` (p50≤300ms, p95≤1.2s online); `slotId` → adherencia |
| `set.undo` | op `/sync` `undo_set` | ruido del contador |
| `session.finish` | `POST /workouts/{id}/complete` → `CompletedAt` | completion rate; early-drop ≤15min |
| `sync.result` | respuesta `/sync` `applied/deduped/failed` | estados del chip; `pendiente (n)` offline |

Locales-efímeros (no se envían; contrato de nombres): `rest.start/rest.skip/rest.complete`, `session.abandon`, `sheet.open`.

## 7. Microcopy final (español, literal)

| Código / estado | Copy |
|---|---|
| `plan_empty` (400) | «Aún no hay entreno de hoy» + «Tu coach todavía no ha programado esta rutina. Cuando lo haga, aparecerá aquí.» + CTA `Ver mis rutinas` |
| `routine_not_found` (404) | «Rutina no encontrada» + toast, quedarse en origen |
| `session_completed` (409) | «Fin del entreno — ya está en tu Historial» + CTA Historial |
| `not_your_session` (403) | «Esta sesión pertenece a otro usuario» |
| `slot_not_in_plan` | chip PROBLEMA (REINTENTAR), UI intacta |
| offline | banner «SIN CONEXIÓN — tus toques se guardan» + chip `PENDIENTE (n)` |
| loading | «Cargando entrenamiento…» |

## 8. Edge cases offline / error / sync

1. **Offline desde el minuto 0:** `from-plan` se encola en OPFS con `clientSessionId`+`clientWorkoutId`; al reconectar, `/sync` devuelve `idMap` y las ops `complete_set` resuelven el workout real. Taps nunca perdidos.
2. **Doble toque en activación / app reabierta el mismo día:** `from-plan` idempotente → 200 `resumed` con progreso real (no duplica Workout).
3. **Replay offline:** mismo `opId` → `deduped`, no doble fila.
4. **Sesión completada en otro dispositivo:** op → `failed.reason=session_completed` → chip PROBLEMA + recargar snapshot → frame J; intentos nuevos → 409 frame I (D4).
5. **`slot_all_sets_done`:** chip PROBLEMA, el slot ya muestra ✓; no bloquear el resto.
6. **Foto null:** placeholder hairline (nunca img rota).
7. **Undo con reps locales sin commit:** solo resetea el contador local, no genera op.

## 9. Criterios de aceptación (QA audita contra esto)

1. desde Hoy: 1 toque → sesión activa (201) o reanudada (200); badge coach solo si `coachSigned`.
2. `from-plan` doble el mismo día → 200 `resumed`, un solo Workout (verificado en API + `DailySessionKey`).
3. cada `✓ SERIE HECHA` = una fila `Exercise` con `SourceTemplateExerciseId` visible al instante en Historial.
4. `undo_set` borra exactamente la última serie del slot (soft-delete); nada corrige reps sin borrar serie.
5. offline round-trip: taps → `PENDIENTE (n)` → reconectar → `SYNCED`; `applied/deduped` correctos; cero toques perdidos.
6. coach que intenta manejar sesión ajena → 403 `wrong_role`.
7. sesión completada → cualquier op nueva → 409; frame I/J según caso (D4).
8. máquina de estados §2 renderizada con mocks empty/offline/error (copys §7 literales).
9. métricas §5 MVP computables server-side (`tap_ts`→time-to-log, adherencia vía `SourceTemplateExerciseId`).
10. **Sin regresión** en `apply-template` ni en el logging manual (checks de QA existentes de TodayWorkout en verde).
11. A11y: FIX-3 completo (botones reales con nombre, sheets `role=dialog`, sr-only, `role=timer`) + `:focus-visible` visible en todos los controles nuevos + contraste de §5 medido ≥4.5 en todo texto.
12. Motion según §4 con `prefers-reduced-motion` funcional.

## 10. Orden de implementación y tarjetas

`B1 API (pau)` → `FE1 activación/entry (pau)` y `FE2 núcleo /training/session (pau)` en paralelo tras B1 → `FE3 sheet detalle + motion + localización (pau)` → `QA auditoría (qa)`. FE2 es el camino crítico; FE1/FE3 son montables en paralelo sobre contrato. Los deliverables de `docs/design/` (espec + hi-fi + handoff) se comitean con el PR de FE1.
