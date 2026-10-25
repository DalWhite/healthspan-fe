#!/usr/bin/env python3
"""API perf bench — healthspan-api-dev (curl-only, sin navegador).

Suite de benchmarks perf re-ejecutable (t_4c61763c). Corrige el protocol del
baseline t_56b5840a y define umbrales HARDCODEADOS que hacen FAIL (exit 1)
si se superan — el job CI de push a main los ejecuta tal cual:

  - TTFB p95 <= 0.5s por endpoint y HTTP 200 en todas las iteraciones.
  - El propio bench completa en <= 90s (presupuesto CI: el pipeline de push a
    main debe mantenerse <= 180s; este bench no debe sumar mas de 90s).

Cuenta: qa-perf@dev.local (bench dev-only, ya seedeada con 120 activities;
si faltan filas, re-seedea in-situ — solo aplica en dev).

Uso:
  python3 scripts/perf/api_bench.py [--iters 10] [--base URL] [--out FILE]
  HS_API_BASE env override de --base.
Salida: tabla PASS/FAIL por metrica + JSON de resultados (--out, default
api_perf.json). Exit 1 si cualquier umbral falla (rojo en CI).
"""
import argparse, json, os, subprocess, sys, time

BASE = os.environ.get("HS_API_BASE", "https://healthspan-api-dev.azurewebsites.net")
EMAIL = "qa-perf@dev.local"        # dev-only bench account (regla de casa)
PASSWORD = "QaPerf!2026bench"

# ── Umbrales hardcodeados (no tocar sin decision de equipo) ─────────────────
THRESHOLDS = {
    "ttfb_p95_s": 0.5,      # por endpoint
    "runtime_max_s": 90.0,  # el bench mismo; CI push-main objetivo total <=180s
}
SEED_ROWS_MIN = 120
OK_CODES = (200, 204)  # 204 = sin contenido legitimo (Workouts/today vacio)

def curl(method, path, token=None, body=None):
    cmd = ["curl", "-sS", "--max-time", "15", "-o", "/tmp/hs_perf_body.bin",
           "-w", "%{time_starttransfer} %{time_total} %{size_download} %{http_code}",
           "-X", method, BASE + path]
    if token: cmd += ["-H", f"Authorization: Bearer {token}"]
    if body is not None:
        cmd += ["-H", "Content-Type: application/json", "-d", json.dumps(body)]
    out = subprocess.run(cmd, capture_output=True, text=True).stdout.strip().split(" ")
    ttfb, total, size, code = float(out[0]), float(out[1]), int(out[2]), int(out[3])
    return ttfb, total, size, code

def login_retry(n=5):
    last = None
    for _ in range(n):
        ttfb, total, size, code = curl("POST", "/login", body={"email": EMAIL, "password": PASSWORD})
        if code == 200:
            tok = json.load(open("/tmp/hs_perf_body.bin"))["accessToken"]
            return tok, ttfb
        last = code; time.sleep(1)
    raise SystemExit(f"login failed ({last}) — bench account qa-perf@dev.local")

def ensure_seed(token):
    """Dataset determinista dev-only: >=120 actividades. Barato si ya seedeado."""
    ttfb, total, size, code = curl("GET", "/api/activities", token=token)
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
        _, _, _, code = curl("POST", "/api/activities", token=token, body=body)
        if code not in (200, 201):
            print(f"seed POST {i} -> {code}", file=sys.stderr)
            break
    ttfb, total, size, code = curl("GET", "/api/activities?page=1&pageSize=100", token=token)
    rows = json.loads(open("/tmp/hs_perf_body.bin").read() or "{}")
    return rows.get("total", 0)

def pct(xs, p):
    xs = sorted(xs)
    k = max(0, min(len(xs) - 1, int(round((p / 100) * (len(xs) - 1)))))
    return xs[k]

def bench(name, path, token, iters):
    ttfb_s, kb, codes = [], [], set()
    for _ in range(iters):
        ttfb, total, size, code = curl("GET", path, token=token)
        if code == 401:  # token instance-pinned: re-login una vez y reintenta
            token, _ = login_retry()
            ttfb, total, size, code = curl("GET", path, token=token)
        codes.add(code)
        ttfb_s.append(ttfb); kb.append(size / 1024)
    out = {"name": name, "path": path, "iters": iters, "http": sorted(codes),
           "ttfb_p95_s": round(pct(ttfb_s, 95), 3),
           "kb_p50": round(pct(kb, 50), 2), "kb_p95": round(pct(kb, 95), 2)}
    ok_http = all(c in OK_CODES for c in codes)
    ok_ttfb = out["ttfb_p95_s"] <= THRESHOLDS["ttfb_p95_s"]
    return token, out, ok_http and ok_ttfb, out

def main():
    global BASE
    ap = argparse.ArgumentParser()
    ap.add_argument("--iters", type=int, default=10)
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
        token, rec, ok, _ = bench(name, path, token, args.iters)
        results.append(rec)
        checks.append((f"{name}: http en (200,204)", all(c in OK_CODES for c in rec["http"]), f"http={rec['http']}"))
        checks.append((f"{name}: ttfb_p95 <= {THRESHOLDS['ttfb_p95_s']}s",
                       rec["ttfb_p95_s"] <= THRESHOLDS["ttfb_p95_s"],
                       f"ttfb_p95={rec['ttfb_p95_s']}s kb_p95={rec['kb_p95']}"))

    runtime_s = round(time.monotonic() - t0, 1)
    checks.append((f"bench runtime <= {THRESHOLDS['runtime_max_s']}s",
                   runtime_s <= THRESHOLDS["runtime_max_s"], f"runtime={runtime_s}s"))

    summary = {"base": BASE, "account": EMAIL, "iters": args.iters,
               "seed_rows": seed_rows, "runtime_s": runtime_s,
               "thresholds": THRESHOLDS, "login_ttfb_s": round(login_ttfb, 3),
               "endpoints": results}

    failed = 0
    print(f"api_bench {BASE} (iters={args.iters}, runtime={runtime_s}s)")
    for name, ok, detail in checks:
        print(f"  {'PASS' if ok else 'FAIL'}  {name}  [{detail}]")
        failed += 0 if ok else 1
    json.dump(summary, open(args.out, "w"), indent=1)
    print(f"{'GREEN' if failed == 0 else 'RED'}: {len(checks) - failed}/{len(checks)} checks PASS (fails={failed})")
    if os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(os.environ["GITHUB_STEP_SUMMARY"], "a") as f:
            f.write("### API perf bench\n| check | resultado |\n|---|---|\n")
            for name, ok, detail in checks:
                f.write(f"| {name} | {'PASS' if ok else 'FAIL'} ({detail}) |\n")
    sys.exit(1 if failed else 0)

if __name__ == "__main__":
    main()
