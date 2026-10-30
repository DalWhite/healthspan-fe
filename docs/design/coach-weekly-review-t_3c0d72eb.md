# Spec: UI de revisión para el coach (Weekly Review — coach-facing)

**Tarjeta:** t_3c0d72eb · parent t_2d6ced5c (D1–D11 BINDING) · wiring: t_cc8ae405 · QA: t_0635e698
**Prototype:** `docs/design/prototypes/coach-review-t_3c0d72eb.html` (5 frames @390px, screenshot `coach-review-390.png`)

## 0. Goals

Task of the coach, every Monday: review the N generated reports with the **minimum of taps and maximum trust**. The coach never rewrites the report: they only decide (Approve), patch (3 knobs), release (Send). Accountability loop: un reviewed never reaches the trainee (D8).

## 1. Two screens (dual persona, rule 8: same system, different views)

1. **Queue — «Revisión»** (inbox of the coach): all reports of the week, pending sorted first.
2. **Detail — «Revisión de la semana»**: report with sections in the FIXED order of §3/D10 (header → compliance → volume_by_group → wellness_extra → intensity_balance → trend → coach_summary), UI never invents sections.

## 2. Decisions closed (owner: this card)

- **CU1 — Entry point.** Route `/coach/reviews`. Desktop (≥1000px): slot «Revisión» in the sidebar after Insights. Mobile: entry from the avatar sheet (never steals the 5 slots of the trainee — those are their activation). Wiring/razor is Pau's (t_cc8ae405); this card fixes the destination.
- **CU2 — Queue card = glanceable decision unit.** Each card: trainee name · week label · hero compliance % (tabular, 30/800, colored by adherence vocabulary: ≥80 done-green, <80 gold) · status chip **with icon** (● «Revisar» grey / ✓ «Aprobado» accent / ✎ «Ajustado» gold / ✈ «Enviado» green — lifecycle colors gray→blue→gold→green) · one line of `coach_summary`. Sort: `coach_pending` **con datos ascending** (peor primero → la atención va donde hay riesgo) → `coach_pending` **sin datos** (E1, sin ranking falso) → aprobados/ajustados → enviados. Card sin plan (E1): data chip «Sin plan» (borde discontinuo), hero «—», never a fake 0%. Queue header carries count chip «n por revisar · semana N» + batch `Send (n)` (CU4). Tap → detail. Rationale: coach clears the Monday chore scanning numbers, not reading prose.
- **CU3 — One-click approve.** In the detail, the primary button **Approve** (accent, 44px, ✓ icon) sits in a **sticky bottom action bar** (Adjust ✎ secondary left, Approve ✓ primary right — thumb reach on mobile, always visible on scroll). After a glance of the hero, validation = 1 tap. Never a confirm modal (approve is cheap; undo is not needed because delivery is a separate action — CU4).
- **CU4 — Approve ≠ Send.** `Approve`/`Adjust` decide the content (D8: approved|adjusted). `Send` = delivery, batchable from the queue header: `Send (n)` where n = aprobados + ajustados pendientes de entrega (ajustado es estado de contenido terminal en D8: no requiere Approve adicional). Monday chore = N approves + 1 send. Tradeoff (1 line): separating Send keeps the final gate before data reaches the trainee and batches delivery; collapsing them would force per-trainee delivery decisions the coach does not want each Monday.
- **CU5 — Adjust = patch, exactly 3 knobs.** Secondary button Adjust opens a bottom sheet (progressive disclosure — not in the queue, not above the fold on first visit) with ONLY:
  1. **Summary text**: textarea prefilled with the template text (prefill = keep). Editing it → `summary_override`, `source=template→override` (D7/D8).
  2. **Hide a group**: per-row visibility toggle on volume_by_group (the only "section tweak" of D8, one affordance per row, 44px tap).
  3. **Recompute numbers**: explicit button, label «Recalculate». Numbers are never edited directly; the coach flags recompute, never invents (D8).
  Save (sheet primary «Guardar») → PATCH If-Match, `Version+1`, estado resultante **Ajustado** (chip ✎; entra en el lote Send). On 409 (E11/concurrency): inline error, non-blaming, input preserved: «El informe cambió en otro dispositivo. Recarga para revisar la última versión.» + sub «Tu nota se conserva · versión N → N+1» — NN/g error rule, never a snackbar.
- **CU6 — Hierarchy.** One hero number only: compliance %. Everything else is secondary (bars, arrows, bands). Summary bubble last. The Approve affordance never competes with content: it lives in the sticky bottom bar (CU3). Section order is D10-FIXED: UI renders the schema, never re-sorts.
- **CU7 — Accessible states (never color alone).** Status chips (ciclo D8): `coach_pending` = ● gris «Revisar»; `approved` = ✓ accent «Aprobado»; `adjusted` = ✎ gold «Ajustado»; `delivered` = ✈ verde «Enviado». Data chips (orthogonales, borde discontinuo): ○ «Sin plan» / ? «Sin datos». Chip = micro-label mayúscula + icono + texto. Contraste medido (script WCAG): texto accent pequeño usa token **`--accentText #4D8CFF`** (old #1E6FFF sobre #14233F = 3.56 FAIL → 4.85 PASS); blanco sobre #1E6FFF solo ≥16px bold (4.40, large-text); botones blancos pequeños usan **`--accentDeep #1B63EA`** (5.22); microcopy dentro de card usa `t2` (t3 = 4.39 FAIL a 11px). Focus ring: accent 2px `:focus-visible`. All tap targets ≥44px mobile; disabled controls keep readable text + «Bloqueado» label (disabled ≠ invisible).
- **CU8 — Edge rendering (maps §4 to pixels).** E1/E6/E8 → section shows data chip «Sin datos» / panel «Historial insuficiente» (no arrows, no 0) — **Approve remains available** (a no-data week is still a valid report; ver frame 5 del prototipo). E2 → 0% is real, variant «semana sin sesiones». E4/E5 → presence without volume renders dot without bar, never invented volume. E7 → wellness row is a separate line with «no computa en el volumen». E11 → opened delivered report: chip «Enviado» + actions disabled + «Bloqueado · enviado» label. E12 → header names the majority trainer (never a list).
- **CU9 — WASM budget + skeleton.** Detail renders only the metrics of D10 (≤20 KB already fixed by parent). New skeleton variant `revisions` per t_fea01cd1 protocol (fixed-height blocks, CLS ≤0.1, `role=status` + aria-busy).
- **CU10 — Copy (D11).** «Revisión», buttons `Approve` · `Adjust` · `Send` — los tres verbos D11 se quedan exactos (isla English mínima y deliberada); **todo lo demás copy es español**: etiquetas de sección, chips («Revisar / Aprobado / Ajustado / Enviado / Sin plan / Sin datos»), microcopy y el `coach_summary` en español voz-coach según D7. Words `adherence`, `template`, `prescription` never in UI. Trainee sees only «Revisión de tu semana» (t_cc8ae405 surface).

## 3. Tap budget (review checklist)

Queue → detail: 1 · Approve: 1 → **2 taps inbox→validated**. Adjust: open(1)+knob(1)+save(1)=3. Send batch: 1 from queue. All ≤4. After any action, coach lands back on the queue (rule 2: not a catalog).

## 4. Acceptance mapping (this card)

| Acceptance | How |
|---|---|
| Clear visual hierarchy, fixed §3 order | CU6 + hero=1 number, sections render schema order |
| Coach actions approve/adjust/send, one-click flow | CU3 (1 tap approve from detail, 2 from inbox) + CU4 batch send |
| Accessible states pending/delivered | CU7 icon+text+shape, no color-alone |
| Fast validation without rewriting | CU2 glanceable cards + CU5 3-knob patch (never retype) |

## 5. Guarantees t_cc8ae405 must keep (wiring)

Approve/Adjust persist Version+1 with If-Match (409 → the error copy of CU5); adjustment merges preserve untouched metrics (additive `summary_override`); trainee sees exactly the approved/adjusted artifact (QA t_0635e698 compares delivered == coach decision). The UI of this card assumes and requires that end-to-end equality.

## 6. What is NOT in v1

No email/PDF (parent out_of_scope). No per-metric inline editing (numbers only via recompute). No reopen of delivered (E11, not v1). Queue is single-week (this Monday's batch); archive = next week history in the trainee side, not a coach backlog.
