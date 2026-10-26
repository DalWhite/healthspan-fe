# Benchmarks de perf (suite QA — t_4c61763c)

Suite estable y re-ejecutable. Umbrales HARDCODEADOS en cada script; cualquier
umbral superado => exit 1 (CI rojo). Cambiar un umbral exige decision de equipo.

| script | que mide | donde corre |
|---|---|---|
| `api_bench.py` | TTFB p95 por endpoint (curl-only, sin navegador) | job CI `Perf API Bench` en push a `main` |
| `fe_bench.py` | cold start WASM (KB/requests/interactividad), navegacion por pantalla en modo cache-miss (F2), filas del historial (F3) | job nightly/manual `Perf nightly (FE bench)` (workflow_dispatch o cron 03:00 UTC) |

Umbrales: TTFB p95 <= 0.5s por endpoint; cold KB <= 2048KB; cold requests <= 50;
filas historial > 0; runtime del api_bench <= 90s (presupuesto CI push-main
objetivo <= 180s; el job ademas lleva `timeout-minutes: 3`).

Nota roadmap: los umbrales de cold KB/requests son objetivo de la roadmap de
reduccion WASM y hoy se superan (~3.6MB / ~86 reqs): el nightly FE es
deliberadamente rojo hasta que baje el payload. El job CI de push (api_bench)
si debe estar verde.

## Ejecucion local

```bash
# api_bench (solo curl)
python3 scripts/perf/api_bench.py --iters 10
HS_API_BASE=https://healthspan-api-dev.azurewebsites.net python3 scripts/perf/api_bench.py

# fe_bench (necesita playwright + chromium)
python3 -m venv ~/perfvenv && ~/perfvenv/bin/pip install playwright && ~/perfvenv/bin/playwright install chromium
~/perfvenv/bin/python scripts/perf/fe_bench.py --cold 3
HS_APP_URL=https://salmon-island-045222d03.4.azurestaticapps.net ~/perfvenv/bin/python scripts/perf/fe_bench.py
```

Cuenta bench dev-only: `qa-perf@dev.local` (120 activities seed; `api_bench`
re-seedea si faltan filas). Nunca usar esta cuenta en prod.

## Protocolo FE (F2/F3)

- F2: cada pantalla se mide con perfil chromium limpio por pantalla (OPFS +
  service worker + HTTP cache vacios) para que la navegacion SPA dispare los
  requests reales; se reportan `requests_api`, `kb_api` y `nav_ms` por pantalla.
- F3: el historial usa `data-testid="activity-row"` (y clase `.hs-hist-row`);
  el bench espera filas y asserta filas > 0 con la seed de 120 actividades.
