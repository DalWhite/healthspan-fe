#!/usr/bin/env python3
"""API perf bench — healthspan-api-dev (curl-only, sin navegador).

Suite de benchmarks perf re-ejecutable (t_4c61763c). Protocolo v2 (calibración
2026-10-26, runs CI 40393548925/40404006035):

  - Cada endpoint se mide sobre UNA conexión HTTP reutilizada (curl --next):
    WARMUP peticiones de calentamiento (descartadas) + ITERS peticiones medidas.
  - Métrica: TTFB p90 sobre las muestras medidas; umbral 0.5s (SE MANTIENE).
  - El propio bench completa en <= 90s (presupuesto CI: el pipeline de push a
    main debe mantenerse <= 180s; este bench no debe sumar mas de 90s).

  Justificación del cambio de protocolo (evidencia en la card t_4c61763c):
  v1 (8 muestras, TLS nuevo por request, p95) fallaba en runners CI con TTFB
  uniforme 0.50-0.72s incluso en endpoints de 0 KB: el handshake TLS+TCP
  dominaba la medida, y p95 de 8 muestras == muestra más lenta (1 outlier de
  red = rojo). Calibración local sobre la misma API: fresh p50=0.22-0.31s;
  warm (keepalive, desde el request #2) p50=0.06-0.07s, max tipico 0.11s.
  El umbral 0.5s no baja: cambia QUE se mide (API caliente sobre conexion
  establecida, p90 sobre 15 muestras tolera 1 outlier de red).

Cuenta: qa-perf@dev.local (bench dev-only, ya seedeada con 120 activities;
si faltan filas, re-seedea in-situ — solo aplica en dev).

Uso:
  python3 scripts/perf/api_bench.py [--warmup 5] [--iters 15] [--base URL] [--out FILE]
  HS_API_BASE env override de --base.
Salida: tabla PASS/FAIL por metrica + JSON con distribucion completa (--out,
default api_perf.json). Exit 1 si cualquier umbral falla (rojo en CI).
"""
import argparse, json, os, subprocess, sys, time

BASE = os.environ.get("HS_API_BASE", "https://healthspan-api-dev.azurewebsites.net")
EMAIL = "qa-perf@dev.local"        # dev-only bench account (regla de casa)
PASSWORD = "QaPerf!2026bench"

# ── Umbrales hardcodeados (no tocar sin decision de equipo) ─────────────────
THRESHOLDS = {
    "ttfb_p90_s": 0.5,      # warm (keepalive+warmup) p90 por endpoint
    "runtime_max_s": 90.0,  # el bench mismo; CI push-main objetivo total <=180s
}
SEED_ROWS_MIN = 120
OK_CODES = (200, 204)  # 204 = sin contenido legitimo (Workouts/today vacio)
WARMUP_DEFAULT, ITERS_DEFAULT = 5, 15

def curl_one(method, path, token=None, body=None):
    cmd = ["curl", "-sS", "--max-time", "15", "-o", "/tmp/hs_perf_body.bin",
           "-w", "%{time_starttransfer} %{time_total} %{size_download} %{http_code}",
           "-X", method, BASE + path]
    if token: cmd += ["-H", "Authorization: Bearer " + token]
    if body is not None:
        cmd += ["-H", "Content-Type: application/json", "-d", json.dumps(body)]
    out = subprocess.run(cmd, capture_output=True, text=True).stdout.strip().split(" ")
    ttfb, total, size, code = float(out[0]), float(out[1]), int(out[2]), int(out[3])
    return ttfb, total, size, code

def curl_warm_batch(path, token, n):
    """n GETs en UNA conexion HTTP (curl --next reutiliza TLS).
    Devuelve ([(ttfb, code, kb)], rc). El request #1 incluye el handshake TLS;
    del #2 en adelante TTFB mide solo la API sobre conexion caliente."""
    w = ["-w", "%{time_starttransfer} %{http_code} %{size_download}\n"]
    auth = ["-H", "Authorization: Bearer " + token]
    cmd = ["curl", "-sS", "--max-time", "120", "-o", "/tmp/hs_perf_body.bin", *w,
           "-X", "GET", *auth, BASE + path]
    for _ in range(n - 1):
        cmd += ["--next", "-X", "GET", "-o", "/tmp/hs_perf_body.bin", *w,
                *auth, BASE + path]
    r = subprocess.run(cmd, capture_output=True, text=True)
    samples = []
    for line in r.stdout.strip().splitlines():
        p = line.split(" ")
        if len(p) == 3:
            samples.append((float(p[0]), int(p[1]), int(p[2]) / 1024))
    return samples, r.returncode

def login_retry(n=5):
    last = None
    for _ in range(n):
        ttfb, total, size, code = curl_one("POST", "/login", body={"email": EMAIL, "password": PASSWORD})
        if code == 200:
            tok = json.load(open("/tmp/hs_perf_body.bin"))["accessToken"]
            return tok, ttfb
        last = code; time.sleep(1)
    raise SystemExit(f"login failed ({last}) — bench account qa-perf@dev.local")

def ensure_seed(token):
    """Dataset determinista dev-only: >=120 actividades. Barato si ya seedeado."""
    ttfb, total, size, code = curl_one("GET", "/api/activities", token=token)
    rows = json.loads(open("/tmp/hs_perf_body.bin").read() or "{}")
    n = rows.get("total", 0) if isinstance(rows, dict) else len(rows)
    if n >= SEED_ROWS_MIN:
        return n
    for i in range(SEED_ROWS_MIN):
        day = (i % 30) + 1
        month = ["06", "07", "08", "09"][i // 30]
        t = ["strength_session", "sauna", "mobility_flow"][i % 3]
        body = {"type": t, "dimension": ["strength", "health", "mobility"][i % 3],
                "occurredOnUtc": f"2026-{month}-{day:02d}T{6 + i % 12:02d}:00:00Z",
                "durationMinutes": 30 + (i % 30)}
        _, _, _, code = curl_one("POST", "/api/activities", token=token, body=body)
        if code not in (200, 201):
            print(f"seed POST {i} -> {code}", file=sys.stderr)
            break
    ttfb, total, size, code = curl_one("GET", "/api/activities?page=1&pageSize=100", token=token)
    rows = json.loads(open("/tmp/hs_perf_body.bin").read() or "{}")
    return rows.get("total", 0)

def pct(xs, p):
    xs = sorted(xs)
    k = max(0, min(len(xs) - 1, int(round((p / 100) * (len(xs) - 1)))))
    return xs[k]

def bench(name, path, token, warmup, iters):
    """Mide el endpoint: warmup requests descartados + iters miden, todo en una
    conexion. 401 (token instance-pinned) => re-login 1 vez y batch nuevo."""
    expected = warmup + iters
    for attempt in (1, 2):
        samples, rc = curl_warm_batch(path, token, expected)
        codes = {c for _, c, _ in samples}
        if 401 in codes and attempt == 1:
            token, _ = login_retry()
            continue
        break
    warm_ts = samples[:warmup]
    measured = samples[warmup:]
    ttfb = [t for t, _, _ in measured]
    kb = [k for _, _, k in measured]
    all_codes = sorted({c for _, c, _ in samples})
    ok_batch = rc == 0 and len(samples) == expected
    ok_http = ok_batch and all(c in OK_CODES for c in all_codes)
    ok_ttfb = bool(ttfb) and round(pct(ttfb, 90), 3) <= THRESHOLDS["ttfb_p90_s"]
    out = {"name": name, "path": path, "warmup": warmup, "iters": iters,
           "http": all_codes,
           "warmup_ttfb_s": [round(t, 3) for t, _, _ in warm_ts],
           "ttfb_min_s": round(pct(ttfb, 0), 3) if ttfb else None,
           "ttfb_p50_s": round(pct(ttfb, 50), 3) if ttfb else None,
           "ttfb_p90_s": round(pct(ttfb, 90), 3) if ttfb else None,
           "ttfb_p95_s": round(pct(ttfb, 95), 3) if ttfb else None,
           "ttfb_max_s": round(pct(ttfb, 100), 3) if ttfb else None,
           "ttfb_samples_s": [round(t, 3) for t in ttfb],
           "kb_p50": round(pct(kb, 50), 2) if kb else None,
           "kb_p95": round(pct(kb, 95), 2) if kb else None}
    detail = (f"ttfb_p90={out['ttfb_p90_s']}s min={out['ttfb_min_s']} "
              f"p50={out['ttfb_p50_s']} max={out['ttfb_max_s']} kb_p95={out['kb_p95']}")
    if not ok_batch:
        detail += f" [batch incompleto {len(samples)}/{expected} rc={rc}]"
    return token, out, ok_http, ok_ttfb, detail

def main():
    global BASE
    ap = argparse.ArgumentParser()
    ap.add_argument("--warmup", type=int, default=WARMUP_DEFAULT)
    ap.add_argument("--iters", type=int, default=ITERS_DEFAULT)
    ap.add_argument("--base", default=BASE)
    ap.add_argument("--out", default="api_perf.json")
    args = ap.parse_args()
    BASE = args.base
    t0 = time.monotonic()

    token, login_ttfb = login_retry()
    seed_rows = ensure_seed(token)

    endpoints = [
        ("GET activities p1(10f)", "/api/activities"),
        ("GET activities p1(100f)", "/api/activities?page=1&pageSize=100"),
        ("GET workouts list", "/Workouts?page=1&pageSize=14"),
        ("GET workouts today", "/Workouts/today"),
        ("GET health-profile", "/HealthProfile"),
        ("GET routines", "/routines"),
        ("GET import template", "/data/import/template"),
    ]
    results, checks = [], []
    checks.append(("seed rows >= 120", seed_rows >= SEED_ROWS_MIN, f"rows={seed_rows}"))
    for name, path in endpoints:
        token, rec, ok_http, ok_ttfb, detail = bench(name, path, token, args.warmup, args.iters)
        results.append(rec)
        checks.append((f"{name}: http en (200,204) y batch completo", ok_http, f"http={rec['http']}"))
        checks.append((f"{name}: ttfb_p90 <= {THRESHOLDS['ttfb_p90_s']}s (warm, {args.iters} muestras)",
                       ok_ttfb, detail))

    runtime_s = round(time.monotonic() - t0, 1)
    checks.append((f"bench runtime <= {THRESHOLDS['runtime_max_s']}s",
                   runtime_s <= THRESHOLDS["runtime_max_s"], f"runtime={runtime_s}s"))

    summary = {"base": BASE, "account": EMAIL, "warmup": args.warmup, "iters": args.iters,
               "seed_rows": seed_rows, "runtime_s": runtime_s,
               "thresholds": THRESHOLDS, "login_ttfb_s": round(login_ttfb, 3),
               "endpoints": results}

    failed = 0
    print(f"api_bench {BASE} (warmup={args.warmup}, iters={args.iters}, runtime={runtime_s}s)")
    for name, ok, detail in checks:
        print(f"  {'PASS' if ok else 'FAIL'}  {name}  [{detail}]")
        failed += 0 if ok else 1
    json.dump(summary, open(args.out, "w"), indent=1)
    print(f"{'GREEN' if failed == 0 else 'RED'}: {len(checks) - failed}/{len(checks)} checks PASS (fails={failed})")
    if os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(os.environ["GITHUB_STEP_SUMMARY"], "a") as f:
            f.write("### API perf bench (protocolo v2 warm)\n| check | resultado |\n|---|---|\n")
            for name, ok, detail in checks:
                f.write(f"| {name} | {'PASS' if ok else 'FAIL'} ({detail}) |\n")
    sys.exit(1 if failed else 0)

if __name__ == "__main__":
    main()
