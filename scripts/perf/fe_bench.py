#!/usr/bin/env python3
"""FE perf bench (navegador) — Blazor WASM PWA (t_4c61763c).

Protocolo QA estable, corrige los hallazgos del baseline t_56b5840a:
  F2: la cache offline (OPFS + service worker) elimina los requests de la
      navegacion caliente. Cada pantalla se mide con PERFIL LIMPIO por
      pantalla (se borra el profile dir antes de cada run) => cache-miss
      garantizado; se reportan requests_api, kb_api y nav_ms por pantalla.
  F3: el historial /wellbeing/activities usa la fila propia .hs-hist-row con
      data-testid="activity-row" (no componentes Mud). El bench espera a que
      se pinten filas y ASSERTA filas > 0 (con 120 filas seed en la cuenta
      bench qa-perf@dev.local).

Umbrales HARDCODEADOS (fail => exit 1):
  - cold start: kb_total p95 <= 3374KB y requests_total p95 <= 100
    (objetivo acordado 3174KB + margen; ver THRESHOLDS)
  - historial actividades: filas DOM > 0 en modo cache-miss
  - toda pantalla del protocolo debe alcanzar su ruta (reached)

Uso (necesita playwright + chromium):
  <venv-playwright>/bin/python scripts/perf/fe_bench.py [--cold 3] [--no-screens] [--out fe_perf.json]
  HS_APP_URL env override del target (default SWA dev de main).
"""
import argparse, json, re, shutil, sys, time
from playwright.sync_api import sync_playwright

APP = "https://salmon-island-045222d03.4.azurestaticapps.net"
API_HOST = "healthspan-api-dev.azurewebsites.net"
EMAIL, PASSWORD = "qa-perf@dev.local", "QaPerf!2026bench"

# pantalla, ruta, minimo de filas esperado (F3: historial de actividades)
SCREENS = [("today", "/training/today", 0),
           ("historial-actividades", "/wellbeing/activities", 1),
           ("historial-entrenamiento", "/training/history", 0),
           ("rutinas", "/training/routines", 0),
           ("perfil", "/profile", 0),
           ("importacion-settings", "/settings", 0)]

ROW_SELECTOR = '[data-testid="activity-row"], .hs-hist-row'

THRESHOLDS = {
    # Umbrales alineados con el objetivo acordado (opción C, t_97c8d7f9):
    # cold payload <= 3174KB (baseline recortado #37). Baseline real post-#44:
    # kb_p95 2965KB / req_p95 84; los 2048/50 originales eran pre-recorte y
    # señalaban rojo permanente sin regresión real. Margen +200KB sobre el
    # objetivo para no falsear por deriva de red.
    "cold_kb_max": 3374,       # KB totales del cold start (p95)
    "cold_requests_max": 100,  # requests del cold start (p95)
    "hist_rows_min": 1,        # filas historial actividades en cache-miss
}

CAT = re.compile(r"\.(wasm|dll|json|js|css|png|jpg|svg|woff2?|ttf|ico)(?:\?|$)")

def classify(url):
    m = CAT.search(url)
    return "api" if API_HOST in url else (m.group(1) if m else "otro")

class Net:
    def __init__(self, cdp):
        self.sent, self.done = {}, {}
        cdp.send("Network.enable")
        cdp.send("Performance.enable")
        cdp.on("Network.requestWillBeSent",
               lambda p: self.sent.setdefault(p["requestId"], (time.monotonic(), p["request"]["url"])))
        cdp.on("Network.loadingFinished",
               lambda p: self.done.__setitem__(p["requestId"], (time.monotonic(), p.get("encodedDataLength", 0))))

    def window(self, t_from, t_to):
        reqs = []
        for rid, (t, url) in self.sent.items():
            if t_from <= t <= t_to:
                _, size = self.done.get(rid, (None, 0))
                reqs.append({"cat": classify(url), "kb": round(size / 1024, 2),
                             "url": url.split("//", 1)[-1][:90]})
        return reqs

    def last_activity(self):
        ts = [t for t, _ in self.sent.values()] + [t for t, _ in self.done.values()]
        return max(ts) if ts else None

def metrics(cdp):
    return {m["name"]: m["value"] for m in cdp.send("Performance.getMetrics")["metrics"]}

def do_login(page, net):
    """Login real (credenciales de la seed bench dev-only). Devuelve True si la
    app quedo autenticada (appbar/drawer presente). Resiliente a arranques
    lentos de WASM: reintentos con wait explicito por paso."""
    try:
        page.wait_for_selector("h1:has-text('Sign in'), .hs-login-card", timeout=60000)
    except Exception:
        if page.locator(".mud-appbar, .mud-drawer").count() > 0:
            return True
    last_err = None
    for _attempt in range(5):
        try:
            # Normaliza: si la app cae en una deep-link sin card de login,
            # usa la ruta canonica /login (siempre tiene email+password).
            if page.evaluate("()=>document.querySelectorAll(\"form input[type='password']\").length") == 0:
                page.goto(APP + "/login", wait_until="domcontentloaded")
                page.wait_for_selector("h1.hs-login-title", timeout=60000)
            page.wait_for_selector("form input[type='password']", timeout=20000)
            page.fill("form input:first-of-type", EMAIL)
            page.fill("form input[type='password']", PASSWORD)
            page.click("button:has-text('Log In')")
            try:
                page.wait_for_selector(".mud-appbar, .mud-drawer", timeout=45000)
                return True
            except Exception:
                # Fallback: login exitoso si la card desaparece (WASM lento
                # post-login; el drawer puede tardar >15s en el primer render).
                if page.evaluate("()=>!document.querySelector('.hs-login-card') && !document.querySelector(\"h1.hs-login-title\")"):
                    return True
                raise
        except Exception as e:
            last_err = e
    inputs = page.evaluate("()=>Array.from(document.querySelectorAll('input')).map(i=>i.type)")
    print(f"login FAILED tras 5 intentos: {last_err} | url={page.url} | inputs={inputs}", file=sys.stderr)
    return False

def fresh_profile(name):
    """F2: perfil chromium limpio por pantalla — borra OPFS, cache SW y HTTP."""
    d = f"/tmp/hs_perf_profile_{name}"
    shutil.rmtree(d, ignore_errors=True)
    return d

def wait_idle(net, cap_s=25):
    """Espera a que la red se aquieta (fin de requests de la pantalla)."""
    t0 = time.monotonic()
    while time.monotonic() - t0 < cap_s:
        la = net.last_activity()
        if la is not None and time.monotonic() - la > 2.0:
            return True
        time.sleep(0.25)
    return False

def cold_runs(pw, n):
    out = []
    for i in range(n):
        browser = pw.chromium.launch()
        ctx = browser.new_context(viewport={"width": 1280, "height": 800})
        page = ctx.new_page(); cdp = ctx.new_cdp_session(page); net = Net(cdp)
        t0 = time.monotonic()
        page.goto(APP, wait_until="domcontentloaded")
        try:
            page.wait_for_selector("h1:has-text('Sign in'), .hs-login-card", timeout=60000)
        except Exception:
            pass
        render_ms = (time.monotonic() - t0) * 1000
        # interactividad real: click login -> POST /login observado en la red
        login_sent = None
        click_t = time.monotonic()
        if not do_login(page, net):
            # fail-fast (t_4c61763c): sin login no hay nada que medir; abortar el
            # job en ~5 min en vez de quemar el timeout en reintentos (40393299266).
            print("fe_bench ABORTADO (fail-fast): login no completo en cold run "
                  f"{i+1} — regresión de arranque/post-login en la app desplegada.",
                  file=sys.stderr)
            browser.close()
            sys.exit(1)
        deadline = time.monotonic() + 15
        while time.monotonic() < deadline and login_sent is None:
            for _, (t, url) in net.sent.items():
                if API_HOST in url and "/login" in url and t >= click_t - 2:
                    login_sent = t; break
            time.sleep(0.1)
        interactive_ms = (login_sent - t0) * 1000 if login_sent else None
        wait_idle(net)
        reqs = net.window(t0, time.monotonic())
        m = metrics(cdp)
        rec = {"run": i + 1, "first_render_ms": round(render_ms),
               "interactive_ms": round(interactive_ms) if interactive_ms else None,
               "requests_total": len(reqs), "kb_total": round(sum(r["kb"] for r in reqs), 1),
               "heap_js_kb": round(m.get("JSHeapUsedSize", 0) / 1024)}
        out.append(rec)
        print(f"cold {i+1}: render={rec['first_render_ms']}ms kb={rec['kb_total']}KB reqs={rec['requests_total']}")
        browser.close()
    return out

def screen_runs(pw):
    """Protocolo F2: una visita cache-miss por pantalla (perfil limpio + login + SPA nav)."""
    out = []
    for name, path, min_rows in SCREENS:
        prof = fresh_profile("screen_" + name)
        ctx = pw.chromium.launch_persistent_context(prof, viewport={"width": 1280, "height": 800})
        page = ctx.new_page(); cdp = ctx.new_cdp_session(page); net = Net(cdp)
        page.goto(APP, wait_until="domcontentloaded")
        if not do_login(page, net):
            # fail-fast (t_4c61763c): sin login no hay nada que medir; abortar
            # el job en ~5 min en vez de quemar el timeout en reintentos.
            print(f"fe_bench ABORTADO (fail-fast): login no completo en pantalla "
                  f"'{name}' — regresión de arranque/post-login en la app desplegada.",
                  file=sys.stderr)
            ctx.close()
            sys.exit(1)
        wait_idle(net)  # asentar requests post-login ANTES de medir la pantalla
        t0 = time.monotonic()
        page.evaluate("(u)=>{const a=document.createElement('a');a.href=u;document.body.appendChild(a);a.click();a.remove();}", APP + path)
        try:
            page.wait_for_url("**" + path, timeout=15000)
        except Exception:
            pass
        # F3: en pantallas con filas esperadas, esperar a que se pinten
        if min_rows > 0:
            try:
                page.wait_for_selector(ROW_SELECTOR, timeout=20000)
            except Exception:
                pass
        wait_idle(net)
        nav_ms = (time.monotonic() - t0) * 1000
        reqs = net.window(t0, time.monotonic())
        api = [r for r in reqs if r["cat"] == "api"]
        rows = page.evaluate(f"()=>document.querySelectorAll('{ROW_SELECTOR}').length")
        m = metrics(cdp)
        rec = {"screen": name, "path": path, "nav_ms": round(nav_ms),
               "requests_total": len(reqs), "requests_api": len(api),
               "kb_total": round(sum(r["kb"] for r in reqs), 2),
               "kb_api": round(sum(r["kb"] for r in api), 2),
               "dom_rows": rows, "rows_min": min_rows,
               "reached": path in page.url,
               "heap_js_kb": round(m.get("JSHeapUsedSize", 0) / 1024)}
        out.append(rec)
        print(f"screen {name}: nav_ms={rec['nav_ms']} reqs_api={rec['requests_api']} kb_api={rec['kb_api']} rows={rows} reached={rec['reached']}")
        ctx.close()
    return out

def pct(xs, p):
    xs = sorted(x for x in xs if x is not None)
    if not xs: return None
    k = max(0, min(len(xs) - 1, int(round((p / 100) * (len(xs) - 1)))))
    return xs[k]

def main():
    import os
    global APP
    APP = os.environ.get("HS_APP_URL", APP)
    ap = argparse.ArgumentParser()
    ap.add_argument("--cold", type=int, default=3)
    ap.add_argument("--no-screens", action="store_true")
    ap.add_argument("--out", default="fe_perf.json")
    args = ap.parse_args()

    with sync_playwright() as pw:
        cold = cold_runs(pw, args.cold)
        hot = [] if args.no_screens else screen_runs(pw)

    checks = []
    kb_p95 = pct([c["kb_total"] for c in cold], 95)
    req_p95 = pct([c["requests_total"] for c in cold], 95)
    checks.append((f"cold kb p95 <= {THRESHOLDS['cold_kb_max']}KB",
                   kb_p95 is not None and kb_p95 <= THRESHOLDS["cold_kb_max"], f"kb_p95={kb_p95}"))
    checks.append((f"cold requests p95 <= {THRESHOLDS['cold_requests_max']}",
                   req_p95 is not None and req_p95 <= THRESHOLDS["cold_requests_max"], f"req_p95={req_p95}"))
    for rec in hot:
        if rec["rows_min"] > 0:
            checks.append((f"{rec['screen']}: filas > 0 (cache-miss)",
                           rec["dom_rows"] >= THRESHOLDS["hist_rows_min"], f"rows={rec['dom_rows']}"))
        checks.append((f"{rec['screen']}: reached {rec['path']}", rec["reached"], f"url={rec['path']}"))

    summary = {"app": APP, "thresholds": THRESHOLDS,
               "cold_render_p95_ms": pct([c["first_render_ms"] for c in cold], 95),
               "cold_interactive_p95_ms": pct([c["interactive_ms"] for c in cold], 95),
               "cold_kb_p95": kb_p95, "cold_requests_p95": req_p95}
    out = {"summary": summary, "cold": cold, "screens": hot, "checks": [
        {"check": c, "ok": ok, "detail": d} for c, ok, d in checks]}

    failed = 0
    print(f"\nfe_bench {APP}")
    for name, ok, detail in checks:
        print(f"  {'PASS' if ok else 'FAIL'}  {name}  [{detail}]")
        failed += 0 if ok else 1
    json.dump(out, open(args.out, "w"), indent=1)
    print(f"{'GREEN' if failed == 0 else 'RED'}: {len(checks) - failed}/{len(checks)} checks PASS (fails={failed}) — wrote {args.out}")
    sys.exit(1 if failed else 0)

if __name__ == "__main__":
    main()
