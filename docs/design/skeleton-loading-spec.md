# Especificación de carga percibida (skeletons) — t_fea01cd1

## Problema
Cold start 11-16 s (WASM ~2.96 MB) y navegación caliente instantánea por caché offline.
El punto débil es la **señal durante el arranque**: hasta ahora el primer feedback era un spinner
genérico tras ~2 s y luego la pantalla vacía; en móvil (el dispositivo primario) la pantalla está
literalmente en blanco durante 10-16 s. En modo cache-miss además hay un segundo hueco: shell
renderizado pero sin datos (el fetch tarda 0.2-3 s).

## Solución: dos niveles de placeholder, geometría exacta
**Tier-1 · skeleton de arranque (`wwwroot/index.html`, pintado con el HTML, 0 ms):**
estructura pura en CSS de la app shell real (topbar 52 px, sidebar 264 px, bottom-nav móvil 68 px,
header "Cargando", tiles 78×84 y tarjetas 128/190 px) con barrido de brillo. Sustituye al loader
anterior (spinner de borde + `icon-512.png`), que además **costaba una request extra** en arranque.

**Tier-2 · skeleton por fetch (`Components/ScreenSkeleton.razor`, variantes `today|list|cards|insights|settings`):**
cada pantalla lo renderiza como **estado inicial del fetch** (antes de disparar la carga). Las
variantes imitan la geometría de la pantalla cargada (contadores de bloques y alturas fijas) para
que el swap skeleton→contenido desplace lo mínimo: CLS medido **0.0234** (umbral 0.1).

## Decisiones de diseño (con el resultado de UX que justifican)
1. **Placeholder con forma de la pantalla, no spinner genérico.** Ver "esto va a ser Hoy" reduce el
   tiempo de espera percibido (expectativa clara) y evita el salto visual del swap — mismo patrón
   que Stripe/Linear aplican a sus vistas.
2. **Alturas fijas por variante y swap in-place** → CLS ≈ 0: una pantalla que "baila" se percibe
   más lenta y rompe la interacción táctil (el dedo apunta a un sitio que se movió).
3. **Skeleton solo en carga inicial.** Si ya hay contenido visible y se recarga (cambio de filtros),
   se mantiene el contenido con `aria-busy` + fila de filas de skeleton *debajo* (Historial): borrar
   el contenido visible para volver a cargarlo es la peor señal de progreso (regla ux-ui-good-apps:
   nunca destruir lo que el usuario ya puede leer).
4. **Señal accesible:** cada skeleton es `role="status"` + `aria-busy` + texto "Cargando…" en
   `visually-hidden`; un lector de pantalla no se queda mudo durante 15 s.
5. **Sin costo:** cero requests nuevas (solo HTML/CSS/JS inline en archivos ya cargados),
   **+1.9 KB gzip** total. Se **elimina** la request de `icon-512.png` del boot.
6. **Variante por destino sin requests:** un script inline elige la forma del skeleton según la URL
   (`today|list|cards|insights|settings`; el resto, genérico).

## Verificación local (preview 5001, Chromium 390×844, contexto fresco = cache-miss)
| Pantalla | Skeleton visible | CLS total | Nota |
|---|---|---|---|
| Hoy | 645 ms | 0.0234 | ≤ 1000 ms ✓ |
| Historial | 517 ms | 0.0234 | Tier-2 `list` |
| Rutinas | 311 ms | 0.0234 | Tier-2 `cards` |
| Perfil | 652 ms | 0.0234 | Tier-2 `settings` |
| Importación (Ajustes) | 333 ms | 0.0234 | Tier-2 `settings` |
| Delta assets | +1.9 KB gzip | — | ≤ 20 KB ✓ · sin requests nuevos |

El skeleton Tier-1 está en el primer frame del HTML (0 ms de latencia de diseño); los valores de la
tabla miden desde la navegación incluyendo el fetch hasta el primer rAF visible en esta máquina.
Tests `PerceivedLoadingTests` (8) fijan el contrato de selectores que `fe_bench` debe medir:
`#hs-boot-skeleton[data-hs-loading="boot"]` y `[data-hs-loading="skeleton"]`.

## Límites / para QA
- Medir con `fe_bench` en modo cache-miss contra preview desplegado (aquí el preview local sirve
  sin gzip; los KB absolutos de la tabla no son comparables a `fe_baseline.json`, el delta sí).
- El script inline usa `history.replaceState` al arrancar Blazor y se auto-elimina tras 2 s si no
  hay skeleton. En producción con Service Worker activo, el HTML cacheado ya incluye el skeleton,
  por lo que el Tier-1 se pinta igualmente con el primer frame.
- Si la app tarda >30 s, aparece el aviso de reintento (`hs-boot-hint`) para que la espera no sea
  ambigua; es texto existente, no un request.
