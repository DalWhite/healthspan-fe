# Healthspan FE Context

## Scope

This file is frontend-only.

Use it when the unified workspace contains both frontend and backend, but you want AI to reason only about the Blazor WebAssembly app in `healthspan-fe-pwa-wasm`.

Do not infer backend structure from this file. It only documents FE architecture, FE conventions, and the FE's current understanding of the API contract.

## One-Screen Summary

- Frontend: .NET 8 Blazor WebAssembly PWA
- UI stack: MudBlazor + Blazor-ApexCharts
- Auth: JWT via `JwtAuthenticationStateProvider` + local storage
- API access: one scoped `HttpClient`, plus typed `HealthspanApiClient` for health features
- Product areas: workouts/exercises/trainees + health analytics
- Health area is the most structured FE feature and must stay backend-constrained
- Older workout/exercise pages still use direct `HttpClient` calls inside Razor pages

## FE Stack

- .NET 8 Blazor WebAssembly
- MudBlazor 6.21.0
- Blazor-ApexCharts 3.5.0
- Blazored.LocalStorage 4.5.0
- ASP.NET Core auth primitives via `AuthenticationStateProvider`
- PWA/service worker support enabled

Main package file:

- `healthspan-fe-wasm-pwa.csproj`

## FE Runtime Model

Entry point:

- `Program.cs`

What startup does:

- registers `App` and `HeadOutlet`
- registers local storage and auth core
- registers MudBlazor services
- registers `TitleService` and `AppStateService`
- registers `JwtAuthenticationStateProvider`
- creates one scoped `HttpClient` using `HealthspanApiBaseAddress`
- registers `HealthspanApiClient`

Routing/auth shell:

- `App.razor` uses `CascadingAuthenticationState`
- global routing is through `AuthorizeRouteView`
- `Shared/MainLayout.razor` sends unauthenticated users to `/login`
- authenticated users landing on `/` are redirected to `/health/dashboard`

## FE Config

Config files:

- `wwwroot/appsettings.json`
- `wwwroot/appsettings.Development.json`

Important client-side keys:

- `HealthspanApiBaseAddress`
- `GoogleClientId`

These are browser-visible settings, not secrets.

## FE Folder Mental Model

Important folders inside the FE project:

- `ApiClients/`: typed clients, currently health-specific client lives here
- `Authentication/`: auth provider and auth-related models
- `Components/`: reusable Razor components
- `Pages/`: routeable UI pages
- `Resources/`: DTOs/view models used by FE
- `Services/`: lightweight app-level state/services
- `Shared/`: layout and nav shell
- `wwwroot/`: CSS, JS, config, manifest, service worker

Feature hotspots:

- `Pages/Health/`
- `Components/Health/`
- `Pages/Exercises/`
- `Pages/ExerciseTypes/`
- `Pages/Trainees/`

## FE Conventions

The FE is mixed-pattern, not fully standardized.

Current conventions:

- older pages often call `HttpClient` directly inside the page
- health pages should use `HealthspanApiClient`
- health DTOs should live in `Resources/Health/HealthAnalyticsModels.cs`
- health display helpers live in `Resources/Health/HealthDisplayHelpers.cs`
- health-specific styling uses `hs-*` classes in `wwwroot/css/app.css`
- page title updates go through `TitleService`

Practical rule:

- for workout/exercise features, expect inline HTTP calls and lighter structure
- for health features, keep changes localized to the typed client + health DTOs + health pages/components

## Auth Flow

Primary FE auth implementation:

- `Authentication/JwtAuthenticationStateProvider.cs`

Behavior:

- login posts to `login`
- register posts to `register`
- Google exchange posts to `auth/google/exchange`
- current user bootstrap calls `manage/info`
- token refresh posts to `refresh`
- logout posts to `Logout`
- access token and refresh token are stored in local storage
- bearer token is attached to the shared `HttpClient`

## Navigation

Shell files:

- `Shared/MainLayout.razor`
- `Shared/NavMenu.razor`

Current health routes:

- `/health`
- `/health/dashboard`
- `/health/blood-tests`
- `/health/biomarkers`
- `/health/assessments`
- `/health/blood-tests/{id}/review`

Other main FE routes:

- exercises
- exercise types
- workouts
- trainees
- settings
- identity/login/register/logout

## Health FE Architecture

Health is the current "best-structured" FE area.

Core files:

- `ApiClients/HealthspanApiClient.cs`
- `Resources/Health/HealthAnalyticsModels.cs`
- `Resources/Health/HealthDisplayHelpers.cs`
- `Pages/Health/*.razor`
- `Components/Health/*.razor`

Health reusable components:

- `ClinicalStatusChip.razor`
- `TrendChart.razor`
- `SparklineChart.razor`

Health styling:

- centralized in `wwwroot/css/app.css`
- uses `hs-*` naming
- mobile/tablet aware
- calm, clinical, compact visual language

## Current Health FE Contract Assumptions

This is the FE's current API understanding. Treat it as FE contract context, not backend truth.

Blood tests:

- `GET /BloodTests`
- `GET /BloodTests/pending-review`
- `GET /BloodTests/{id}`
- `POST /BloodTests/manual`
- `POST /BloodTests/upload`
- `POST /BloodTests/{id}/review`
- `GET /BloodTests/markers/latest`
- `GET /BloodTests/markers/{metricKey}/trends`

Dashboard:

- `GET /Dashboard/summary`

Galpin assessments:

- `GET /GalpinAssessments/catalog`
- `GET /GalpinAssessments`
- `GET /GalpinAssessments/latest`
- `GET /GalpinAssessments/trends/{adaptation}`
- `POST /GalpinAssessments`

Important note:

- the FE currently assumes the Galpin trends route above; if backend changed, revalidate before editing assessment trend behavior

## Health FE DTOs

Main FE health models currently include:

- `GetBloodTestResource`
- `BloodTestMarkerResource`
- `AddBloodTestResource`
- `BloodTestUploadRequest`
- `ReviewBloodTestResource`
- `LatestMarkerSnapshotResource`
- `GetMarkerTrendResource`
- `GalpinAdaptationCatalogItemResource`
- `GalpinAssessmentScoreResource`
- `AddGalpinAssessmentResource`
- `GetGalpinAssessmentResource`
- `GetGalpinAdaptationTrendResource`
- `GetHealthDashboardSummaryResource`
- `TrendSeriesResource`
- `TrendPointResource`

Important FE-side enums/assumptions:

- `BloodTestProcessingStatus`: `PendingReview`, `Processed`, `OcrIssues`
- `MarkerStatus`: `Unknown`, `InRange`, `Low`, `High`, `Borderline`
- `GalpinAdaptationType`: nine FE-friendly adaptation values

## Health FE State Of Play

### Dashboard

`Pages/Health/Dashboard.razor`

- uses only dashboard summary + pending-review blood test list
- shows actionable pending-review panel
- does not invent deeper biomarker detail than the API exposes

### Blood Tests

`Pages/Health/BloodTests.razor`

- segmented into pending review, OCR/problem states, and processed results
- upload flow is review-aware
- manual entry remains first-class fallback
- detail panel shows processing notes and extracted markers
- no file preview/download UI
- no edit/delete flow for saved blood tests

### Review Flow

`Pages/Health/ReviewBloodTest.razor`

- dedicated OCR cleanup screen
- editable marker list
- allows test date + lab name edits before final submission
- confidence is read-only context
- submits final payload through review endpoint

### Biomarkers

`Pages/Health/Biomarkers.razor`

- latest marker snapshots
- client-derived categories
- explicit date-bounded trend queries
- missing trend points mean "not measured", never zero

### Assessments

`Pages/Health/Assessments.razor`

- loads adaptation catalog
- submits owner assessment scores
- shows latest assessment, history, and trend chart

## Non-Health FE Areas

These are older CRUD-style pages and are less structured:

- `Pages/Workouts.razor`
- `Pages/CreateWorkout.razor`
- `Pages/Exercises/*`
- `Pages/ExerciseTypes/*`
- `Pages/Trainees/*`

Important convention:

- many of these pages still use `HttpClient.GetFromJsonAsync` and `PostAsJsonAsync` directly
- do not refactor them into health-style architecture unless there is a clear reason

## Shared FE Helpers

`Services/AppStateService.cs`

- caches exercise types, muscles, and user roles
- includes fallback muscle data if lookup fails
- provides simple search helpers

`Services/TitleService.cs`

- tracks current page title for the app bar

## FE Build / Working State

Known FE commands/tasks:

- `dotnet build healthspan-fe-wasm-pwa.sln`
- `dotnet publish healthspan-fe-wasm-pwa.sln`
- `dotnet watch run --project healthspan-fe-wasm-pwa.sln`

Current FE state:

- FE builds successfully
- there are still unrelated nullable/obsolete warnings elsewhere in older parts of the FE
- those warnings were intentionally not cleaned up during health work

## FE Constraints

Constraints already encoded in the FE:

- do not invent preview/demo health data
- do not invent health endpoints or fields that do not exist in the contract the FE is using
- blood test uploads may create a record even if OCR quality is poor
- pending review is now a first-class workflow
- biomarker trends only include measured observations
- dashboard summary is intentionally shallow
- no FE support yet for sharing/coaching permissions in health analytics

## Best FE Entry Points For AI

For startup/auth/layout work:

- `Program.cs`
- `App.razor`
- `Shared/MainLayout.razor`
- `Shared/NavMenu.razor`
- `Authentication/JwtAuthenticationStateProvider.cs`

For health work:

- `ApiClients/HealthspanApiClient.cs`
- `Resources/Health/HealthAnalyticsModels.cs`
- `Resources/Health/HealthDisplayHelpers.cs`
- `Pages/Health/*.razor`
- `Components/Health/*.razor`
- `wwwroot/css/app.css`

For workout/exercise CRUD work:

- `Pages/Exercises/*`
- `Pages/ExerciseTypes/*`
- `Pages/Workouts.razor`
- `Services/AppStateService.cs`

## Working Rules For Another AI

- stay inside the FE project unless explicitly asked to touch backend
- assume this file is FE-only even if BE exists in the same workspace
- keep health work typed and localized
- keep non-health refactors minimal
- preserve the current MudBlazor shell and `hs-*` health styling conventions
- if an API route looks uncertain, verify it before reinforcing it in FE code
