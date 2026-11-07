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

## Observación opcional (no bloquea merge)
En el **hero de detalle** la banda es mucho más alta que la de la card (16/9 ≈ 225px en desktop) pero el icono sigue a 56px → se ve pequeño y solo (25% de la altura vs 51% en la card). Sugerencia para una tarjeta futura si se pule el detalle: `background-size: clamp(72px, 24%, 112px)` en `.hs-exercise-hero-media` (mantener 56px en la card). Es cosmético; en móvil (banda ~200px) el tamaño actual ya es aceptable.

## Riesgos/limitaciones de esta validación
- El mock replica el CSS de card/grid tokens del commit exacto; no renderiza el Blazor WASM host completo (en esta máquina no arranca por falta de SSE4.1 — la evidencia de navegador real del padre t_8ffed894 ya cubrió el render real: 3 placeholder-only, 0 404).
- QA funcional queda en t_31fcd3ac; esta tarjeta valida solo el aspecto visual/UX.
