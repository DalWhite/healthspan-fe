# Cómo contribuir

## Política de PRs

- **Pocas PRs, grandes lo suficiente.** Trabaja integrado en `main` con commits frecuentes y abre PR solo cuando el cambio es una unidad completa y verificable. No abras una PR por cada ajuste suelto.
- **Verifica localmente ANTES de abrir PR.** CI no es tu entorno de pruebas: una PR debe llegar ya verde.
- **Las PR no generan entornos de staging.** El workflow solo hace build+test en PR; el despliegue a Azure Static Web Apps ocurre **únicamente en push a `main`**.
- **Build y tests verdes obligatorios en la PR.** Si `dotnet test` falla en CI, la PR no se fusiona.

## Verificación local mínima

```bash
dotnet restore
dotnet build
dotnet test tests/healthspan-fe.tests/healthspan-fe.tests.csproj --configuration Release
```

No hay step de lint en CI; `.editorconfig` aplica al editar.

## Benchmarks de perf (`scripts/perf/`)

- **CI (push a `main`):** job `Perf API Bench` corre `scripts/perf/api_bench.py` (curl-only, sin navegador) tras el deploy y **falla si se supera algún umbral** (TTFB p95 <= 0.5s por endpoint; el propio bench debe completar en <= 90s). Presupuesto de feedback: el pipeline de push a main objetivo es **<= 180s**; el job de bench lleva `timeout-minutes: 3` y no debe sumar más de 90s al job actual.
- **Nightly/manual (navegador):** workflow `Perf nightly (FE bench)` (`.github/workflows/perf-nightly.yml`) corre `scripts/perf/fe_bench.py` cada noche (cron 03:00 UTC) y puede lanzarse a mano desde Actions → *Run workflow* (`workflow_dispatch`). Mide cold start WASM (umbrales hardcodeados cold KB <= 2048KB y cold requests <= 50 — hoy por encima, objetivo de la roadmap de reducción WASM), navegación por pantalla en modo cache-miss (`requests_api`, `kb_api`, `nav_ms`) y filas del historial (assert filas > 0).
- Cuenta bench **dev-only**: `qa-perf@dev.local` (120 activities seed). Nunca en prod.
- Cómo correrlos a mano, protocolo F2/F3 y umbrales: ver [`scripts/perf/README.md`](scripts/perf/README.md).

## Al tocar el workflow de CI (`.github/workflows/azure-static-web-apps-salmon-island-045222d03.yml`)

El archivo tiene finales de línea mixtos (CRLF/LF). Conserva el EOL de las líneas cercanas a tu cambio para no ensuciar el diff.
