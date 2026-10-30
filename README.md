# Healthspan — web app (frontend)

Progressive Web App for **Healthspan**, a self-hosted personal training & health-tracking platform. Users log workouts, follow guided training plans, import training history from CSV, upload blood-test results for analysis, and track running assessments — with an offline-first exercise catalogue.

**Who it's for:** the same audience as the [Healthspan API](https://github.com/DalWhite/healthspan-api): people who want their fitness and health data on infrastructure they control.

## Built with

- .NET 8 **Blazor WebAssembly** (PWA, installable)
- **MudBlazor** UI kit + **Blazor-ApexCharts** for trends
- JWT auth (email/password and Google sign-in) via a custom `AuthenticationStateProvider`
- Local **SQLite** mirror of the exercise catalogue for offline browsing/search (hydrated through the API's sync endpoint)
- bUnit test suite under `tests/`
- Deployed as an Azure Static Web App talking to the Healthspan API

## Running it locally

1. Start the backend (see the API repo README), listening on `https://localhost:7278` by default.

2. Run the app:

   ```bash
   dotnet restore
   dotnet run            # http://localhost:5001
   ```

   Point it at a different API with the `HealthspanApiBaseAddress` configuration key if needed.

3. Run the tests (bUnit):

   ```bash
   dotnet test tests/healthspan-fe.tests
   ```

   If your machine only has a newer .NET runtime installed, add `DOTNET_ROLL_FORWARD=Major`.

## Layout

| Path | What lives there |
|---|---|
| `Pages/` | Feature pages (Workouts, Exercises, Health, Billing, Wellbeing, Trainees…) |
| `ApiClients/` | Typed HTTP clients for the API |
| `Authentication/` | JWT auth state provider |
| `docs/` | design system notes, UX audits and plans |

Third-party pieces we build on: MudBlazor extensions at https://codebeam-mudextensions.pages.dev/ and Blazor-ApexCharts (https://github.com/apexcharts/Blazor-ApexCharts).

## Roadmap

See [ROADMAP.md](ROADMAP.md).

## License

GPL-3.0 — see [LICENSE](LICENSE).
