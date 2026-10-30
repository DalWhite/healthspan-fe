# Healthspan frontend — roadmap

Companion to the [API roadmap](https://github.com/DalWhite/healthspan-api/blob/main/ROADMAP.md).

## Now

- [ ] Offline catalogue: hydrate the full exercise mirror in the background; keep first render of `/training/library` and `/training/today` under 3 s (today the 1.4 MB catalogue payload causes a ~10 s long task on the main thread).
- [ ] CSV import UX: progress feedback and bulk-import flow.
- [ ] Accessibility sweep: label→input wiring and focus management (tracked in `docs/design/accessibility-checklist.md`).

## Next

- [ ] Guided follow-along sessions (series/reps pacing).
- [ ] Results page with monthly cards, muscle-load captions and empty/loading/error states.
- [ ] Weekly report view backed by the API's weekly metrics.

## Later / ideas

- [ ] Migrate the older workout pages from in-page `HttpClient` calls to the typed API client.
- [ ] Move to .NET 9 once the shared toolchain is unified.
- [ ] PWA update-flow polish (new-version prompt handling).
