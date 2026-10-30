# Estado desautenticado en rutas protegidas — login-first con returnUrl

**Card:** t_cab4dcf5 · **Autor:** UX (ux-designer) · **Fecha:** 2026-10-26
**Evidencia:** audit t_2ee48f22 (`today-390-unauth.png`, REPORT.md: "estado desautenticado vacío"), código main @47c6b2f
**Mocks:** `docs/design/prototypes/unauth-redirect-t_cab4dcf5.html` · `unauth-390.png` · `unauth-1024.png` · `unauth-bridge-390.png`
**Implementación:** card hija ya creada → t_16be42d4 (pau, dependiente de esta spec)

---

## 1. Problema (verificado en main @47c6b2f)

1. `/training/today` sin sesión renderiza `Components/RedirectToLogin.razor` L3 — un `<div>Redirecting to login...</div>` en inglés, vacío, ~media pantalla — y luego navega a `/login`. El que recibe un link compartido ve media pantalla sin sentido.
2. `/login` (aunque reciba `?returnUrl=…`) es la página genérica en inglés ("Sign in", "Enhance Your Health Journey"): **no dice qué destino protege ni qué se obtiene al entrar**. El usuario no sabe que su link sobrevivió.
3. **Hueco funcional:** `NavigateToRegister()` (Login.razor L310) navega a `/register` **sin `returnUrl`**, y `Components/Identity/Register.razor` no tiene ninguna referencia a returnUrl (0 hits, verificado). Un usuario nuevo que llega por link compartido crea cuenta y pierde el destino.
4. Lo que sí funciona y se conserva: `MainLayout.OnInitializedAsync` construye `/login?returnUrl=<destino escapado>` y `GetSafeReturnUrl()` (Login.razor L318) valida que sea path interno seguro, con fallback `/training/today`. El audit midió `unauth_redirect: returnUrl OK`. **No se cambia este mecanismo; se le añade contexto visual y se propaga a registro.**

## 2. Decisión de diseño

**Mantener el redirección a `/login?returnUrl=` (ya probada y segura) y convertir el trayecto en un estado con contexto en tres momentos: puente → login contextual → retorno garantizado (también vía registro).**

Alternativa descartada: login inline en la ruta protegida (URL preservada, cero redirects). Mejor UX teórica, pero duplica el formulario de login fuera de `LoginLayout`, toca la inicialización de Google por duplicado y no aporta sobre el rediseño contextual con coste claramente mayor. Registrar como mejora futura si el funnel de conversión deep-link→sesión lo justifica.

## 3. Especificación (implementable sin preguntas)

### D1 · Estado puente en el destino (P0)
Sustituye el div vacío de `RedirectToLogin.razor`. Aparece ≤100 ms tras el render (el auth-check aún no ha resuelto) y dura lo que tarde el redirect.

- Contenido: icono `Lock` + nombre del destino (tabla §5) + spinner MudBlazor + microcopy `"Abriendo acceso a {destino}…"`.
- Centrado vertical, ancho máx 420 px, sobre card `#161B22`. Nunca texto en inglés, nunca pantalla vacía.
- Si el auth-check tarda >1,5 s se mantiene el puente (no splash vacío); el splash de arranque general lo cubre t_fea01cd1/t_70bfccd8 — no duplicar.
- Justificación UX: elimina la media pantalla muda que documenta el audit; el usuario entiende que su link es válido y hacia dónde va (NN/g: el coste de un login wall solo se acepta si se ve el valor).

### D2 · Login contextual cuando hay `returnUrl` (P0)
`Login.razor` ya recibe `ReturnUrl` (L131). Cuando sea distinto de null:

- Encabezado del form (arriba del título): chip con `Lock` + `"Vista protegida · {destino}"` (tabla §5).
- Título del form: `"Continúa donde lo dejaste"`.
- Línea de valor bajo el título (explica qué se protege y por qué): `"Tu plan, tus sesiones y tus métricas son privados: solo tú los ves. Al entrar vuelves directamente a {destino}."`
- **Login-first:** CTA primaria = `"Continuar con Google"` (filled, acento #1E6FFF, ancho completo) — es el login real de prod. Debajo, separador `"o con tu email"` y el formulario email/password actual (sin cambios funcionales; el copy del form lo arregla t_5517437f).
- Link de registro visible bajo el form: `"¿No tienes cuenta? Crear cuenta gratis"` → debe navegar a `/register?returnUrl=<mismo>` (ver D3).
- Sin `returnUrl`: comportamiento actual intacto (login genérico).
- Justificación UX: convierte un login anónimo en la continuación de una tarea empezada; NN/g HealthCare.gov — si el usuario no ve qué hay detrás del registro, entra en modo defensivo y abandona.

### D3 · `returnUrl` sobrevive al registro (P0)
- `Register.razor` añade `[Parameter] public string? ReturnUrl` (query, igual que Login).
- El link "Crear cuenta gratis" de Login propaga el returnUrl.
- Tras registro exitoso, en el panel de éxito actual (que hoy no redirige): CTA primaria `"Iniciar sesión y volver a {destino}"` enlazada a `/login?returnUrl=<destino escapado>`; secundario "Ir a mi entrenamientos" → `/training/today`. No auto-login: el backend de identidad no lo garantiza hoy y asumiría behavior no verificado.
- Justificación UX: el alta es el caso más caro del funnel (link compartido → usuario nuevo); sin esto el esfuerzo de registrarse no devuelve al usuario al lugar por el que vino.
- Seguridad: reutilizar la misma validación de `GetSafeReturnUrl` (path interno, no `//`).

### D4 · Postal de previsualización del destino protegido (P1, optativa)
Bajo el card de login (móvil) / en el panel izquierdo (escritorio): preview **honesto** del destino — skeleton de 2–3 tarjetas con geometría real de la página destino, atenuadas (opacidad 0.45), overlay `Lock` + pie `"Así se ve {destino} cuando inicias sesión"`. Sin datos inventados: skeleton, no contenido falso.
- Justificación UX: principio de reciprocidad (NN/g login-walls): mostrar algo antes de pedir credenciales aumenta la probabilidad de completar el registro.
- Es P1 por coste (una variante de skeleton por destino); la spec funciona sin ella.

### D5 · Sesión caducada en mitad de sesión (P0 copy)
Cuando el redirect a `/login?returnUrl` llega por expiración de token (no deep-link frío): mismo login contextual + `MudAlert Severity.Info` en cabeza: `"Tu sesión caducó. Inicia sesión para continuar donde lo dejaste."` Persistir una flag query `expired=1` desde `RedirectToLogin`.

### Fuera de alcance
- Splash de arranque WASM (t_70bfccd8 / t_fea01cd1), copy en inglés residual del form (t_5517437f), labels a11y (t_dade41d7). Esta spec fija solo los textos nuevos que introduce.

## 4. Componentes MudBlazor sugeridos

| Pieza | Componente | Notas |
|---|---|---|
| Puente (D1) | `MudPaper` + `MudStack` + `MudProgressCircular Indeterminate` + `MudIcon Icons.Material.Filled.Lock` | Reemplaza el div de RedirectToLogin.razor |
| Chip contexto (D2) | `MudPaper Class="hs-chip-protected"` (rounded, `#262E38` border) con `MudIcon Lock` Size.Small | No usar MudChip (contraste del tema) |
| CTA Google | `MudButton Variant.Filled Color.Primary Size.Large FullWidth` | Ya existe `StartGoogleLoginAsync`; no cambiar lógica |
| Separador | texto plano `<div class="hs-divider">o con tu email</div>` | — |
| Registro | `MudButton Variant.Text Color.Primary` | OnClick → `/register?returnUrl=…` |
| Alert caducada (D5) | `MudAlert Severity.Info` | Bajo el chip de contexto |
| Postal (D4) | `MudPaper` con skeletons CSS (patrón `hs-boot-skeleton` ya usado en index.html) + overlay `MudIcon Lock` | Reutilizar tokens del skeleton de arranque |

Tokens v1 (DESIGN.md): canvas `#0E1116`, card `#161B22`, borde `#262E38`, acento único `#1E6FFF`, texto `#F5F7FA` / `#B4BCC9` / `#8A94A3`. Botones táctiles ≥44 px.

## 5. Tabla destino → nombre (para puente, chip y copy)

| Ruta (prefijo) | {destino} |
|---|---|
| `/training/today` | tu entrenamiento de hoy |
| `/training/history` | tu historial de entrenamientos |
| `/training/insights` | tus insights |
| `/training/library` | tu biblioteca |
| `/wellbeing/activities` | tus actividades de bienestar |
| `/health/biomarkers` | tus biomarcadores |
| `/settings` | tus ajustes |
| `/premium` | tu plan Premium |
| cualquier otra | tu Healthspan |

## 6. Copy exacto (español)

| Momento | Texto |
|---|---|
| Puente | `Abriendo acceso a {destino}…` |
| Chip | `Vista protegida · {Destino Capitalizado}` (p. ej. "Vista protegida · Entrenamiento de hoy") |
| Título login | `Continúa donde lo dejaste` |
| Valor | `Tu plan, tus sesiones y tus métricas son privados: solo tú los ves. Al entrar vuelves directamente a {destino}.` |
| CTA Google | `Continuar con Google` |
| Separador | `o con tu email` |
| Registro | `¿No tienes cuenta? Crear cuenta gratis` |
| Éxito registro | `Cuenta creada. Inicia sesión para volver a {destino}.` CTA: `Iniciar sesión y volver` |
| Caducada | `Tu sesión caducó. Inicia sesión para continuar donde lo dejaste.` |
| Postal (D4) | `Así se ve {destino} cuando inicias sesión` |

## 7. Ejemplos reales citados

1. **Strava** — *Activity privacy controls* (support.strava.com/en-us/articles/15401987): un visitante desautenticado que abre un link de actividad "Everyone" es "redirected to a sign-in **or create-account** page". Verificado en vivo (2026-10-26): `strava.com/login` muestra en una sola pantalla `Sign In With Google`, `Sign In With Apple`, email y **"Create a New Account"** + "Join for Free" en la barra. Resultado: deep-link de entrenamiento compartido → ambas salidas visibles desde la primera pantalla; ningún visitante ve una página vacía.
2. **NN/g** — *Login Walls Stop Users in Their Tracks* (nngroup.com/articles/login-walls): el login wall tiene coste de interacción alto y "there may be no next time"; se justifica solo con beneficio percibido evidente (reciprocidad). De ahí D2 (decir qué se protege) y D4 (mostrar algo antes del muro).
3. **NN/g** — *HealthCare.gov's Account Setup* (nngroup.com/articles/affordable_care_act_usability_issues), regla 1: "Allow users to see the products or information before registering" — sin información previa, "users tend to move into a defensive mode". Es el fracaso que evita la postal D4 y la línea de valor de D2.
4. **NN/g** — *A Checklist for Registration and Login Forms on Mobile* (nngroup.com/articles/checklist-registration-login): explicar los beneficios del registro, ofrecer login social, pedir lo mínimo. Aplicado en D2 (Google primero, email segundo, ningún campo extra en el estado puente).

## 8. Criterios de aceptación para t_16be42d4

1. Deep link frío sin sesión: 0 pantallas sin contenido interpretable en ningún momento del trayecto (390 y 1024); puente visible <1 s.
2. `/login?returnUrl=…`: chip de contexto + título "Continúa donde lo dejaste" + copy de valor; `/login` sin returnUrl sigue igual.
3. Registro desde un deep link: al crear cuenta el usuario puede volver al destino original en ≤2 taps; returnUrl validado igual que login.
4. Post-login y post-Google-login el usuario aterriza en el destino original (regression sobre el returnUrl ya medido OK en el audit).
5. Textos nuevos todos en español; táctiles ≥44 px; contraste AA sobre tokens v1.
