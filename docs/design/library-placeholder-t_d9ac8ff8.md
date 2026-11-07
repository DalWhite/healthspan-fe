# Validación UX — placeholder de imagen en /training/library (t_d9ac8ff8)

**Veredicto: APROBADO.** El placeholder cumple el criterio de aceptación (visualmente aceptable, sin img roto, coherente con la UI). Validado sobre el código del PR #60 (commit `34920b9`) replicando el CSS exacto en un mock y midiendo geometría/contraste reales. Solo una mejora opcional no-bloqueante para el hero.

## Qué se validó y con qué evidencia

| Criterio | Resultado | Evidencia |
|---|---|---|
| Sin img roto | ✅ | Sin `ImageRoute` no se renderiza `<img>` ni se hace request (solo placeholder CSS). Además `onerror="this.remove()"` como red de seguridad: si una foto 404a en runtime, el `<img>` se autoelimina y queda el placeholder. |
| No rompe layout / proporción | ✅ | Banda de imagen fija en **110px** con o sin foto. Medido en mock a 390px: cards de 171px, placeholder icon centrado de 56px con 27px de aire vertical; cards placeholder miden exactamente lo mismo que las vecinas con foto (0 desplazamiento, CLS 0). A 1280px: 3 cols × 405px, misma banda 110px. Grid xs=6/sm=6/lg=4 → 2 cols en móvil/tablet, 3 en desktop. |
| Contraste | ✅ | Paleta real de `icon-512.png` sobre la superficie de la card (`#161B22`): marca blanca **17.3:1**, trazo rojo **#EF2D15 4.2:1**. El disco teal oscuro (#013437) es casi invisible (1.3:1) → el icono se lee como marca de la app flotando en la card, **no como un agujero negro ni como error**. |
| Alt text / a11y | ✅ | Con placeholder no hay `<img>` que anunciar. Las fotos usan `alt=""` (decorativo) correctamente: el nombre ya va en el `aria-label` de la card ("View {name} stats") y en el título visible. Sin doble lectura en lectores de pantalla. |
| Consistencia | ✅ | Misma regla en card (110px) y en hero de detalle (`aspect-ratio 16/9`, mismo icono): foto solo desde `ImageRoute`, si no, placeholder CSS, sin request. En la biblioteca real solo 3/876 ejercicios van a placeholder (873 con ImageRoute válido). |

## Capturas (mock con CSS clonado del commit)
- `library_placeholder_t_d9ac8ff8_390.png` — móvil 390px @2x, cards placeholder alternadas con fotos.
- `library_placeholder_t_d9ac8ff8_wide.png` — desktop 1280px.
- Mock reproducible: `docs/design/prototypes/library-placeholder-t_d9ac8ff8.html` (usa assets relativos de wwwroot).

## Ajuste del hero aplicado (t_5cbed1af, 2026-11-07)
La observación opcional anterior se aplicó: en el **hero de detalle** la banda es mucho más alta que la de la card pero el icono seguía a 56px → se veía pequeño y solo. Cambio en `app.css` → `.hs-exercise-hero-media`: `background-size: clamp(72px, 24%, 112px)` (las cards de biblioteca siguen fijas en 56px, sin tocar).

Medición aplicada (mock `docs/design/prototypes/hero-placeholder-t_5cbed1af.html`, CSS literal del repo + gutters de MudContainer; el glyph pintado es ~0.98 del recuadro por el margen interno del artwork de `icon-512.png`):

| Viewport | Banda hero | Recuadro (clamp) | Glyph pintado | Glyph/banda | Cards |
|---|---|---|---|---|---|
| 320px | 262×147 | 72px (suelo) | 70.5×54 | 37% | 56px intacto |
| 390px | 332×187 | 79.7px | 77.5×59 | 32% | 56px intacto |
| 699px (grid apila) | 641×361 | 112px (techo) | 110×83.5 | 23% | 56px intacto |
| 700px (grid 2 col) | 220×124 | 72px (suelo) | 70.5×54 | 44% | 56px intacto |
| 1024px | 220×124 | 72px (suelo) | 70.5×54 | 44% | 56px intacto |
| 1280px | 220×124 | 72px (suelo) | 70.5×54 | 44% | 56px intacto |

El glyph mantiene relación de aspecto idéntica a la de la card (0.761 vs 0.764) → sin deformación; overflow horizontal 0 en los 6 viewports. Evidencia: `qa-evidence/t_5cbed1af/` (screenshots 390/699/1024 @2x + JSON de geometría y pixel-check). Nota: en desktop la columna del hero está capada a 220px por el grid `minmax(140px, 220px)`, por eso el clamp vive entre el suelo (72px) y el techo (112px) sobre todo en móvil/tablet.

## Riesgos/limitaciones de esta validación
- El mock replica el CSS de card/grid tokens del commit exacto; no renderiza el Blazor WASM host completo (en esta máquina no arranca por falta de SSE4.1 — la evidencia de navegador real del padre t_8ffed894 ya cubrió el render real: 3 placeholder-only, 0 404).
- QA funcional queda en t_31fcd3ac; esta tarjeta valida solo el aspecto visual/UX.
