# Spec UX — Landing sin MudBlazor (t_aec4956d)

Depende de: decisión de arquitectura t_8c5090aa (Core eager + Feature/Mud/Apex lazy) y
alimenta t_7b26dfd8 (integración en la rama) y t_bb7bdc5b (objetivo wasm ≤1536 KB).
Base verificada: borrador real en `wt/t_bb7bdc5b` (`Pages/Landing.razor`,
`Shared/LandingLayout.razor`, split Core/Feature, lazy-load en `App.razor`,
CSS `hs-landing-*` en `app.css`). Esta spec fija copy, jerarquía, estados,
componentes y zonas; es vinculante para la implementación en t_7b26dfd8.

---

## 1. Objetivos UX

1. La landing debe **pintar contenido real antes de descargar cualquier ensamblaje
   lazy** (Feature, MudBlazor, ApexCharts). Ningún request a `*.Feature.wasm`,
   `MudBlazor.wasm` o `Blazor-ApexCharts.wasm` mientras el usuario está en `/`.
2. El visitante anónimo entiende en <5 s qué es Healthspan y tiene una acción
   primaria clara ("Empezar gratis") sin salir del pliegue a 390 px.
3. La única fricción de rendimiento (la primera navegación fuera de `/`,
   ~250–400 KB extra de por vida una sola vez) debe percibirse como **progreso
   controlado, no como página rota** → estado L4 obligatorio (sección 5).

## 2. Copy (cerrado — usar literal)

| Zona | Texto | Nota |
|---|---|---|
| Kicker | `Healthspan` | mayúsculas por CSS (`text-transform`), no en markup |
| H1 (único en la página) | `Entrena. Mide. Mejora.` | ritmo triple, memorizable |
| Subtítulo | `Registra tus entrenamientos, sigue tu salud y crea rutinas a tu medida. Todo en un mismo sitio.` | una frase de valor + promesa de unificación |
| CTA primaria → `/register` | `Empezar gratis` | "gratis" es el gancho; no "Regístrate" |
| CTA secundaria → `/login` | `Iniciar sesión` | también presente en header y footer |
| Card 1 | **Entrenamiento diario** — "Completa tu sesión de hoy y guarda series, repeticiones y sensaciones en segundos." | feature ancla = pantalla Hoy |
| Card 2 | **Salud medida** — "Biomarcadores, analíticas y test de bienestar en una evolución clara." | |
| Card 3 | **Rutinas a tu ritmo** — "Diseña rutinas, asigna trainees y repete lo que funciona." | |
| Footer | `Healthspan` + enlace `Iniciar sesión` | mínimo; sin links a rutas protegidas |
| Overlay de carga (L4) | Título: `Preparando tu espacio…` · Sub: `La primera vez tarda unos segundos.` | ver §5 |
| 404 (chrome de landing) | Título: `Página no encontrada` · Cuerpo: `Puede que ya no exista o que el enlace esté mal.` + enlace `Volver al inicio` → `/` | **fix**: hoy el `NotFound` de App.razor está en inglés; la landing es ES |

Microcopy prohibido en landing: precios, comparativas, "premium" (no hay datos
todavía para sostenerlo).

## 3. Jerarquía visual (mobile-first, 390×844)

Orden vertical único, una columna, ancho de lectura 720 px máx (ya en CSS):

1. **Header** (logo-izq + "Iniciar sesión"-der), 44 px táctiles.
2. **Hero**: kicker → H1 (`clamp(2rem, 8vw, 3.2rem)`; a 390 px ≈ 31 px, line-height 1.1) → subtítulo → par de CTAs. Presupuesto: CTAs visibles antes de 600 px de scroll.
3. **Features**: 3 cards apiladas (grid gap 12), scroll por debajo del pliegue — son refuerzo, no requisito de conversión.
4. **Footer** mínimo.

Reglas:
- Un solo `h1`; las cards usan `h2`.
- CTA primaria visualmente diferenciada (fondo accent) y secundaria ghost; orden DOM = orden visual (no revertir con CSS).
- Sin imágenes ni assets nuevos en landing salvo el logo `icon-192` (ya precacheable, pequeño). Nada de hero ilográfico: el coste de red es el enemigo #1 de esta tarea.
- Sin carruseles, sin animaciones de scroll, sin cookies-banner.

## 4. Componentes permitidos en la landing (vanilla/Blazor)

Todo en ensamblaje **Core**, cero referencias a tipos de Feature/Mud/Apex
(reflejo solo en App.razor, como ya está):

- `LandingLayout.razor`: `<header class="hs-landing-header">` (logo `<a>` + `<nav aria-label="Cuenta">`), `<main class="hs-landing-main">@Body</main>`, `<footer class="hs-landing-footer">`.
- `Landing.razor`: `<section>` hero (`p` kicker, `h1`, `p` subtitle, `<div class="hs-landing-actions">` con dos `<a class="hs-landing-btn">`), `<ul class="hs-landing-features">` con `<li>`.
- **Prohibido en Core:** cualquier `Mud*`, componentes Feature, ApexCharts, `MudIcon`/iconos Material, y CSS que dependa de variables que solo existen cuando llega Mud (ver §7).

## 5. Estados (todos definidos)

| ID | Estado | Qué se muestra | UX |
|---|---|---|---|
| L1 | Anónimo en `/` | Landing completa (L1 es el único estado que pinta sin ensamblajes lazy) | objetivo principal |
| L2 | Sesión activa en `/` (cookie/session viva) | Redirect inmediato a `/training/today`; durante la carga de Feature → **L4** | la landing es un detour, no un paso obligatorio; ya implementado en `OnInitializedAsync` de Landing |
| L3 | Tap en `Empezar gratis` / `Iniciar sesión` | Navegación SPA a `/register` o `/login`; dispara carga lazy → **L4** | |
| L4 | **Carga lazy de Feature+Mud** (primera navegación fuera de `/`, o deep link) | Overlay/barra de progreso **vanilla en Core**: barra de progreso CSS-only sobre el header + botón origen en estado busy (label pasa a `Preparando…`), `aria-live="polite"`, `aria-busy="true"` en `<main>` | **Gap actual**: `OnNavigateAsync` carga ensamblajes *antes* de renderizar el destino — hoy el usuario pulsa y no pasa nada visible hasta que Feature llega (1,5–4 s en móvil medio). Exigido: el feedback debe ser inmediato (mismo frame del click). No usar `icon-512.png` como spinner (113 KB que el parent va a reemplazar): spinner/barra 100 % CSS |
| L5 | No autorizado en ruta protegida (deep link `/health` sin sesión) | `RedirectToLogin` actual (Feature+Mud ya cargados; destino `/login`) | unchanged |
| L6 | 404 | Chrome de landing con copy ES de §2 | fix de idioma |
| L7 | Offline (SW activo) | `/` servida del precache (core wasm + app.css + icon-192); links a `/login`/`/register` muestran L4→fallo: mensaje `Sin conexión. Tu sesión anterior sigue disponible en la app.` fuera del scope de esta iteración, pero L4 debe poder mostrar estado de error genérico | nota |

Presupuesto percibido de L4: <2 s p50 en móvil medio post-warm (wa1260). Si se
supera, la mitigation es dividir la carga (§8), no añadir animaciones.

## 6. Navegación y zonas de carga perezosa

**Zona Core (sin Mud, siempre disponible):** `/`, 404.

**Zona Feature+Mud (carga perezosa en la primera navegación tras `/`):**
`/login`, `/register`, `/training/*` (Hoy, Rutinas, create/edit), `/health/*`,
`/workouts*`, `/exercises*`, `/exercises/types*`, `/trainees/*`, `/settings*`,
`/billing/*`, `/logout`.

**Zona ApexCharts:** `/health` (Dashboard, Biomarkers) y cualquier gráfico.
Recomendación a Pau (decisión de implementación, no UX-blocking): mover
`Blazor-ApexCharts` a un segundo grupo lazy que se dispare solo al navegar a
rutas con gráficos. Justificación UX: el camino de mayor tráfico tras la
landing es login/register — ahí no debe pagarse el peso del lib de charts.

Regla de oro para el chrome de landing: ningún enlace directo a rutas
protegidas (evita L5 desde marketing); el único acceso a rutas lazy es por
login/register o deep links.

## 7. Tokens y contraste (medido, no estimado)

Fondo oscuro base `#0F1520`, card `#161B22`, accent `#1E6FFF`.

- Texto secundario `#97A3B6` sobre fondo: **7,17:1 ✓** (usar token propio de
  core `--hs-text-secondary: #97A3B6`; **no** `var(--mud-palette-text-secondary,…)`
  — la variable Mud no existe hasta que llega Mud; el fallback funciona pero el
  landing debe ser independiente de tokens Mud, es la regla de esta tarea).
- Enlaces/accent-ink: **~9:1 ✓**.
- CTA primaria: blanco sobre `#1E6FFF` = **4,40:1 ✗ para texto normal (AA 4,5)**.
  Fix elegido: label de `.hs-landing-btn` a `1.125rem/700` (18 px bold =
  "large text" AA → umbral 3:1, pasa con holgura) **y** opcionalmente deepen
  fill solo en landing con `--hs-accent-strong: #1750C0` (**7,13:1 ✓**) si Pau
  prefiere resolver por color. Ambas baratas; al menos una obligatoria.
- Secondary button = ghost sobre card con borde `#262C36`: legible, no usar
  texto secundario dentro del botón.
- Botones ya cumplen 44 px (`--hs-touch-target`).

## 8. Criterios de aceptación UX (verificables)

1. Red log en `/` cold boot: **cero** requests a `*.Feature.wasm`,
   `MudBlazor.wasm`, `Blazor-ApexCharts.wasm`, y cero `mud*.css`/`mud*.js`.
2. Landing DOM: exactamente un `h1`; copy literal de §2; dos CTAs con href
   `/register` y `/login`; target táctil ≥44 px.
3. L4 visible en el mismo frame del click (progreso CSS-only en Core, sin
   dependencia de icon-512), con `aria-live`/`aria-busy`.
4. Deep link `/login` en frío: landing nunca queda en blanco — muestra L4.
5. 404 con chrome de landing en español.
6. Ningún identificador `Mud` en el grafo eager (esto lo verifica el build del
   parent, no UI).

## 9. Qué NO cambia

Login/Register mantienen MudBlazor (Mud entra con la interacción, tal permite
la decisión del parent). MainLayout, nav post-auth y todas las páginas Mud
quedan fuera de esta spec.
