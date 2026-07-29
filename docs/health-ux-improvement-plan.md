# Health UX and Blood-Test Workflow Improvement Plan

Reviewed 29 July 2026.

## Implementation status

Implemented 29 July 2026.

- Health Overview is action-first and no longer mixes in training metrics.
- Blood-test upload, per-record processing status, review, history, and detail are mobile-first.
- The review flow preserves report ranges and raw OCR provenance, validates marker issues inline,
  and provides a full-screen report viewer on phones.
- Biomarkers use canonical aliases and units, block incompatible-unit charts, provide period
  presets plus an optional custom range, and link measurements to their source test.
- Assessments use compact score accordions and a single selected trend.
- Profile changes refresh applicable marker classifications.
- Upload validation now checks size, extension, content type, and file signature. Health mutations
  return Problem Details with stable error codes.
- Blood-test history uses a summary contract; marker rows are fetched only for detail/review.
- Exact duplicate uploads are warned using a per-user SHA-256 content hash.

The schema change is in the API migration
`20260729155654_ImproveBloodTestDataIntegrity`.

Source reports and raw extraction payloads are retained for the life of the blood-test record.
Deleting the record deletes the database payload and performs best-effort source-document cleanup.
There is no automatic time-based retention while this remains a small personal deployment.

The accepted in-memory processing limitation below remains intentional: an upload still in flight
when the App Service restarts may need to be deleted and uploaded again.

## Outcome

The current approach is directionally correct:

- The original report is archived and remains owner-scoped.
- OCR runs asynchronously instead of blocking the upload screen.
- Extracted markers require human review before entering biomarker analytics.
- Failed extraction can fall back to manual entry.
- Finalized tests can be corrected or deleted.
- Biomarkers, assessments, and health profile data already use a typed frontend client.

The main risks are data quality and a health UI that exposes too many panels, fields, and status
details at once. Losing an in-flight OCR job during an App Service restart is a known and accepted
free-tier limitation.

Authenticated `/` and the default post-login destination now open `/training/insights`. Health
Overview remains the entry point for the Health section rather than the application home.

## Engineering constraints

The application is expected to serve at most approximately 100 users on a free Azure App Service
and Aiven MySQL database.

- Prefer small changes to the existing ASP.NET hosted worker, database, and document storage.
- Do not introduce Azure Service Bus, Redis, a distributed cache, a workflow engine, or a separate
  worker service.
- Assume one API instance unless the hosting plan changes.
- Accept that an App Service sleep or restart can interrupt an in-flight report. The user can
  delete and upload it again.
- Optimize for low memory usage and simple maintenance, not guaranteed background delivery.
- Do not add pagination, background infrastructure, or aggregation tables until real data volume
  or measured response time requires them.
- Preserve current routes and deployment topology.

## Target health navigation

| Destination | Primary purpose |
|---|---|
| Health Overview | Show what needs attention and the latest useful health signals |
| Blood tests | Upload, monitor, review, and manage reports |
| Biomarkers | Find a marker and understand its history |
| Assessments | Record and compare Galpin assessments |
| Personal profile | Maintain data used for ranges and bodyweight calculations |

Keep existing routes. Add focused detail/status routes only where mobile navigation needs them.

## Target blood-test journey

1. Select a PDF or image.
2. Confirm the test date and optional lab.
3. Upload once and receive the created blood-test ID immediately.
4. Show a dedicated status view: queued, processing, ready for review, or failed.
5. Allow the user to leave while processing continues in the background.
6. Review suggested markers beside the original report.
7. Resolve uncertain rows, then finalize the test.
8. Only finalized, unit-safe values appear in Biomarkers.

If processing is interrupted or extraction fails, offer clear actions to delete and upload again
or enter results manually.

## 1. Keep background processing intentionally simple — accepted limitation

The current unbounded in-memory channel contains the complete uploaded file bytes. A deployment,
restart, or worker crash can lose accepted jobs and leave records permanently in `Processing`.

- Keep the existing hosted worker and in-memory channel.
- Do not add persistent jobs, startup recovery, retry orchestration, or another infrastructure
  dependency.
- Keep this limitation documented while the application remains on the free hosting plan.
- In the UI, treat an unusually long `Processing` record as interrupted and tell the user to delete
  and upload it again or use manual entry.
- Continue separating safe user-facing errors from internal exception details.
- Return the created ID and current status from upload so the frontend follows the correct record.

## 2. Protect biomarker correctness — P0

- Preserve OCR/lab reference minimum and maximum values when a review is saved. They are currently
  displayed in the review UI but omitted from the review input and recalculated or lost on save.
- Make raw OCR text immutable provenance. Edit interpreted name, value, unit, and reference range
  separately.
- Add canonical unit conversion per metric before assigning `NormalizedValue` and
  `NormalizedUnit`.
- Never draw one trend series from incompatible units. Convert known units or split/block the
  series with a clear message.
- Add canonical marker aliases so spelling, language, accents, and abbreviations do not create
  separate metrics accidentally.
- Reclassify stored marker statuses when biological sex or date of birth changes, or calculate
  status against the applicable range when reading.
- Use a simple document hash to warn about an exact duplicate upload. More elaborate duplicate
  matching is unnecessary at the expected scale.

Acceptance:

- Finalizing a review does not change a confirmed lab reference range.
- Every trend uses one declared unit.
- Profile changes cannot leave visibly stale range classifications.

## 3. Redesign upload and status for mobile — P1

- Replace the collapsible desktop panel with a focused upload sheet/page:
  file, metadata, confirmation.
- Use one 20 MB limit in UI, client, and API.
- Validate extension, content type, and file signature; show the exact rejection reason.
- Show file name, size, type, and a replace action before upload.
- Distinguish network upload progress from background OCR processing.
- Navigate to the created record instead of reloading every list and guessing which item is new.
- Poll only that record while visible, with increasing intervals; stop on terminal state.
- Keep manual entry secondary, but available from failed extraction and the empty state.
- Preserve unfinished metadata if the user closes and reopens the upload surface.

Avoid explanatory paragraphs. Use short status labels and one primary action per state.

## 4. Make review fast and safe on a phone — P1

The current eight-column editable table is suitable for desktop inspection but heavy on mobile.

- Use marker cards on mobile and retain a compact table only at larger breakpoints.
- Show name, value, unit, range, and status first.
- Move raw OCR text and confidence into a collapsed “OCR details” area.
- Prioritize low-confidence, missing-value, duplicate, and out-of-range rows.
- Add previous/next issue navigation and a visible unresolved count.
- Show the report in a desktop side pane and a full-screen mobile viewer.
- Keep the save/finalize action sticky and label it with the result, for example
  `Finalize 24 markers`.
- Warn before leaving with unsaved changes.
- Validate missing names, invalid numbers, range inversions, and duplicate canonical markers
  inline.
- After finalization, return to that test’s detail rather than the top of the list.

## 5. Simplify Blood Tests history — P1

- On mobile, navigate from the list to a dedicated test detail instead of placing detail below the
  complete list.
- Keep the desktop split view if it remains useful.
- Replace overlapping filters with four states: All, Needs review, Processing, Issues.
- Treat finalized as the normal state rather than a permanent filter chip.
- Put re-upload guidance, manual results, and delete actions directly on failed/interrupted records.
- Use one list-summary response without all marker rows; fetch markers only for the selected test.
- Derive pending counts from the list/status response instead of requesting the same records twice.
- Keep the current unpaged history at this scale. Add paging only if measured payload size becomes
  a problem.

## 6. Rebuild Health Overview around actions — P1

The current overview is mostly six equally weighted KPI boxes and includes a training metric.

- Remove recent workouts from Health Overview.
- Lead with a compact `Needs attention` area: pending reviews, failed uploads, and important
  out-of-range results.
- Follow with latest blood test, a small set of changed/out-of-range biomarkers, and latest
  assessment.
- Add a profile-completion prompt only when date of birth or biological sex is missing.
- Use direct actions: Upload report, Review, View biomarkers, Record assessment.
- Hide empty modules instead of filling the page with “not available” cards.
- Keep clinical language neutral: report values and ranges without diagnostic conclusions.

## 7. Simplify Biomarkers — P1

- Add search plus compact category and status filters.
- Put out-of-range and recently changed markers first.
- On mobile, open a selected marker in a focused detail view/sheet; do not place its chart after a
  long card list.
- Replace two date pickers in the page header with presets: 3M, 6M, 1Y, All. Keep custom range as a
  secondary option.
- Show latest value, reference range, change from previous result, unit, and source date.
- Indicate when only one measurement exists instead of presenting an empty “trend.”
- Link each trend point back to its source blood test.
- Keep the current latest-marker query unless profiling shows a problem; expected user-level data
  volume is small.

## 8. Simplify Assessments and Personal Profile — P2

Assessments:

- Replace the always-expanded adaptation grid with compact adaptation rows or accordions.
- Show previous score and change while entering the current score.
- Keep evidence notes optional and collapsed until requested.
- Define the 0–10 scale briefly at the point of entry.
- Move trends to assessment detail/history instead of repeating a Trend action on every form card.
- Keep edit/delete in the history item overflow menu.

Personal Profile:

- Keep the current compact form.
- Explain biological sex and date of birth in one short note because they affect reference ranges.
- Show when range classification has been refreshed after a profile change.
- Preserve existing height/weight values when an omitted optional field is submitted.

## 9. Normalize health API behavior — P1

- Return RFC Problem Details with stable error codes; do not convert unexpected server errors into
  `400 Bad Request`.
- Propagate cancellation tokens through health queries and writes.
- Split list and detail contracts for blood tests.
- Reuse `GET /BloodTests/{id}` for processing status unless a measured payload issue justifies a
  smaller status endpoint.
- Keep queries straightforward and unpaged while per-user history remains small.
- Use normal structured application logs for processing duration and failure. Do not add a
  separate telemetry pipeline.
- Define retention for source documents and raw extraction payloads.

## Delivery order

1. Reference-range preservation and unit normalization.
2. Mobile upload/status flow.
3. Mobile review flow.
4. Blood-test list/detail cleanup.
5. Health Overview and Biomarkers.
6. Assessments and profile refinements.
7. API efficiency and retention cleanup alongside the relevant vertical slices.

Broad test hardening remains deferred while UX is changing. Each P0 data change should still have
focused acceptance coverage because it protects persisted health records.

## Definition of success

- Interrupted and failed uploads clearly explain how to delete/re-upload or enter results manually.
- OCR suggestions never become analytics without explicit review.
- Reference ranges and units remain correct after review and profile changes.
- A phone user can upload, review, and finalize without navigating an eight-column table.
- Health Overview shows actions and relevant signals, not a wall of equal KPI cards.
- Biomarker charts always explain their unit, date range, and source measurements.
