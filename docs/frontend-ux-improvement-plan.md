# Healthspan Frontend UX Improvement Plan

## Purpose

Healthspan's main usability problem is structural rather than purely visual. The current product language exposes technical concepts such as "Exercise Types," while workout sessions, logged exercise records, and the exercise catalog overlap across several screens.

This plan reorganizes the experience around user tasks: browse, log, and review. It preserves the existing Training architecture and improves it incrementally instead of converting it to the Health typed-client pattern.

## Target navigation

| Section | Destination | Purpose |
|---|---|---|
| Health | Overview | Health summary and pending actions |
| Health | Blood tests | Upload, review, and history |
| Health | Biomarkers | Latest values and trends |
| Health | Assessments | Galpin assessments |
| Training | Today | Start or continue today's workout |
| Training | History | Past workout sessions |
| Training | Exercise library | Browse canonical exercises and variations |
| Coaching | Trainees | Trainer-only area |
| Account | Profile / Settings | Personal data, appearance, and data tools |

User-facing terminology should change as follows:

- "Exercise Types" becomes "Exercise Library."
- "Exercises" becomes "Today" or "Training History," depending on context.
- "Create Exercise" becomes "Log Exercise" or "Add to Workout."

## Delivery principles

- Preserve direct `HttpClient` usage in Training pages unless a specific change requires otherwise.
- Preserve the canonical exercise plus variation data model.
- Keep existing URLs as aliases while navigation is migrated.
- Improve API contracts and authorization alongside the workflows that depend on them.
- Deliver in vertical slices with regression coverage rather than performing a full frontend rewrite.

## 1. Stabilize the current baseline — P0, small

Complete the known prerequisite fixes before reorganizing navigation:

- Require an exercise selection before making workout or exercise API requests.
- Preserve the requested return URL when redirecting an unauthenticated user to login.
- Clear expired authorization headers and stored tokens reliably when refresh fails.
- Register local-storage dependencies correctly in the frontend test fixture.
- Make exercise update and delete flows check API responses and navigate consistently.

Acceptance criteria:

- Invalid exercise submissions remain on the form and make no workout request.
- Successful updates and deletes return to Training History.
- Failed updates and deletes remain on the edit screen and show an error.
- `dotnet test healthspan.sln` passes all API and frontend tests.

## 2. Establish the new information architecture — P0, medium

- Introduce Health, Training, and Coaching navigation groups.
- Rename Exercise Types to Exercise Library.
- Replace Exercises with Training History.
- Add a prominent Log Workout or Today action.
- Surface Assessments if it is considered production-ready.
- Keep Settings and Profile in the account area.
- Add active states, page headings, and consistent back navigation.
- Preserve legacy routes with redirects.

Recommended routes:

- `/training/today`
- `/training/history`
- `/training/library`
- `/training/library/{id}`

## 3. Modernize the shared visual foundation — P1, medium

- Expand design tokens for spacing, typography, surfaces, borders, shadows, and semantic colors.
- Remove or scope broad global `.mud-card`, `.mud-item`, `.mud-table`, and `.mud-button` overrides.
- Introduce reusable page-shell, page-header, toolbar, card, loading, empty, and error patterns.
- Align light and dark themes.
- Use a consistent responsive content width.
- Ensure every route exposes a real `h1` for focus management.
- Standardize keyboard focus, touch targets, and mobile spacing.

## 4. Redesign the Exercise Library — P1, medium

- Add filters for muscle group, equipment, movement type, and favourites.
- Show active filters and a result count.
- Make "Log this exercise" the primary card action.
- Move maintenance actions into an overflow menu.
- Display variations in a predictable expandable section.
- Add graceful image placeholders and keyboard-accessible cards.
- Replace comma-separated tag inputs with controlled tag pickers.
- Restrict catalog editing to an appropriate role.

API support:

- Add server-side search, filtering, and pagination to `GET /ExerciseTypes`.
- Add a focused favourite endpoint instead of sending the complete exercise type.
- Return controlled tag values or filter facets.

## 5. Introduce a session-first workout logging flow — P1, large

Create one primary workflow:

1. Open Today.
2. Resume today's session or start a new one.
3. Select an exercise from recent, favourite, or search results.
4. Enter sets, repetitions, and load.
5. Save and remain in the workout.
6. Add another exercise or finish the workout.

The first release should retain the current aggregated sets/repetitions/load model. A per-set schema can be considered later.

API support:

- Add an atomic get-or-create workout operation for a user and date.
- Add exercise records through `/Workouts/{workoutId}/exercises`.
- Return the updated session summary after saving.
- Avoid downloading all workouts before each save.
- Prevent accidental duplicate workouts where appropriate.

## 6. Make Training History workout-based — P1, large

- Show workout cards grouped by session rather than grouping paged exercise records in the browser.
- Include exercise count, total volume, and leading muscles in the summary.
- Add expandable exercise details.
- Add workout-level edit and delete actions.
- Link directly to exercise progression.

API support:

- `GET /Workouts?from=&to=&page=&pageSize=` should return workout summaries.
- `GET /Workouts/{id}` should return the workout and its exercises.
- Pagination should operate on workouts, not exercise records.
- Add consistent update/delete behavior and structured errors.

## 7. Simplify exercise details and editing — P2, medium

- Organize exercise details into Overview, History, Trends, and Variations.
- Replace runtime JavaScript `eval` with a static helper.
- Add empty and error states for missing history.
- Avoid misleading kilogram and 1RM displays for no-weight exercises.
- Confirm deletion and explain API conflicts.
- Preserve the originating route and filters when navigating back.

## 8. Reorganize Profile and Health screens — P2, large

Split the current profile experience into:

- Account/profile information.
- Health profile and reference-range settings.
- Training insights.
- Reusable weekly-summary, session-card, body-map, and adaptation components.

Add a weekly-training-summary API endpoint rather than downloading all exercise history and calculating the profile in the browser. Reuse the Health page patterns across Training without moving Training to the Health typed client.

## 9. Harden and normalize API contracts — P0/P1, medium

- Establish one source of truth for each request and response contract.
- Reconcile workout user ID types and nullability.
- Reconcile bodyweight fields between frontend and backend exercise contracts.
- Validate trainer-to-trainee access before accepting another user's ID.
- Keep global query filters as the default and authorize every bypass.
- Restrict exercise-library mutation endpoints appropriately.
- Standardize Problem Details responses.
- Add cancellation tokens and server-side pagination bounds.
- Normalize route casing and prefixes gradually.

## 10. Add UX-focused verification and release incrementally — P1, medium

Add coverage for:

- Role-aware navigation and legacy routes.
- Complete Today workout logging.
- Workout pagination and grouping.
- Library filtering and variations.
- Keyboard interaction.
- Responsive layouts near 360, 768, and 1440 pixels.
- Playwright smoke journeys.
- Automated accessibility checks.
- Basic visual-regression screenshots.

Recommended release sequence:

1. Baseline stabilization.
2. New navigation and route aliases.
3. Exercise Library redesign.
4. Today logging flow.
5. Workout-based history.
6. Profile and Health cleanup.
7. Removal of obsolete routes and duplicated components after usage is verified.

## Definition of success

- Users no longer need to understand the difference between an exercise record and an exercise type.
- A user can begin logging today's workout from anywhere in one action.
- Workouts never split across history pages.
- Exercise discovery supports meaningful facets and canonical variations.
- Mobile, keyboard, loading, empty, and error experiences are intentional.
- API access to another user's data is explicitly authorized.
- The complete automated regression suite remains green throughout delivery.
