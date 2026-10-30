# Sesión guiada — mockups hi-fi (t_eb3ab918)

Alta fidelidad sobre los wireframes aprobados de t_fc52ab22. Spec funcional intacta en
`guided-session-ux-t_fc52ab22.md` (máquina de estados, logging sobre /sync, a11y) — este doc
añade solo lo visual: fidelidad real, assets, movimiento y geometría medida.

Prototipo: `docs/design/prototypes/guided-session-hifi-t_eb3ab918.html` (11 frames, cada
teléfono = viewport real 390×844). Capturas: `guided_session_hifi_t_eb3ab918_wide.png`,
`guided_session_hifi_t_eb3ab918_mobile.png`.

## Decisiones de fidelidad (cada una justificada por el outcome)

| Decisión | Por qué (outcome) |
|---|---|
| Tokens DESIGN.md v1 verbatim + Inter real vía Google Fonts | El mockup es la especificación visual final; FE copia valores sin reinterpretar |
| Fotos reales del catálogo (free-exercise-db, misma ruta `ImageRoute`: `/api/exercises/{id}/image`) | Elimina la duda "¿cómo se ve una foto 4:3 recortada en card de 52 px / hero de 170 px" antes de implementar |
| Microinteracciones animadas (pop +1, dots, ring, FAB) en el propio HTML | La tarjeta pedía "tarjeta de ejercicio con animación": se resuelve con motion de sesión sin vídeo de ejercicio (corte MVP: assets estáticos), coste ~0 para FE |
| Silueta muscular SVG inline (front/back) + leyenda de texto obligatoria | Mantiene el riesgo ya flaggado (sin asset por MuscleGroup) sin bloquear el diseño; la leyenda garantiza información accesible haya o no asset |
| Chip dorado `FIRMADO POR COACH ANA` (✍ + texto, nunca solo color/icono) | Confianza en el plan es el driver de retención del producto coach; legible con daltonismo y en lector de pantalla |
| Frame J (resumen post-sesión) nuevo | El wireframe terminaba en el sheet de confirmación; el flujo coherente (aceptación) exige aterrizaje con recap + sync confirmado |

## Frames

A activación en Hoy (1 toque) · B vacío plan_empty · C sheet detalle (foto, tip del coach, mapa
muscular) · D sesión en curso (segbar + coachline + dots) · E contador en vivo · F descanso
(role=timer, skippable) · G confirmar fin (recap 12/14 · 86% · PENDING) · H offline (contador
sigue vivo) · H2 problem/retry · I completed 409 · J resumen final.

## Assets para FE

- Imágenes demo (reemplazar por `ImageRoute` real, `object-fit:cover`, `object-position:center 30%`):
  `Barbell_Bench_Press_-_Medium_Grip`, `Dumbbell_Floor_Press`, `Dumbbell_Flyes`, `Bench_Dips`,
  `Seated_Dumbbell_Press` (free-exercise-db; ya es la fuente del catálogo).
- `alt` reales solo en la imagen hero del sheet; thumbs decorativos `alt=""`.
- SVG silueta: inline en el componente (clases `.sil/.prim/.sec`); relleno = grupos primarios,
  contorno = secundarios; la leyenda de texto se renderiza siempre (fallback y a11y).

## Spec de movimiento (obligatoria, ya incluida en el HTML)

| Momento | Animación | Timing |
|---|---|---|
| +1 rep | pop número hero scale 1→1.12→1 | 120 ms ease-out |
| Serie hecha | dot scale 0→1 (stagger 60 ms) + wash done al cerrar slot | 220 ms |
| Descanso | barrido cónico lineal del ring; al 0 pulso de borde + háptico + sr-only | lineal, pulso 600 ms |
| Cambio de slot | saliente −16 px fade, entrante +16→0 | 220 ms `cubic-bezier(.2,.7,.3,1)` |
| Fin disponible | glow del FAB verde pulsa | 1.8 s loop |

Tokens: `--hs-ease`, `--hs-t-fast:120ms`, `--hs-t-med:220ms`. Todo se apaga con
`prefers-reduced-motion:reduce`; el feedback de commit (dot + wash + sr-only) funciona igual sin
animación — la animación nunca es el único canal.

## Verificación medida (Chromium, cada teléfono 390×844)

- 11/11 frames renderizan; 5/5 fotos reales cargan (naturalWidth>0); Inter `document.fonts=loaded`.
- Desborde vertical dentro del teléfono: 0 en los 11 frames. Desborde horizontal: 0 (único hit
  fue el `.sr-only` intencional).
- Targets táctiles medidos ≥44 px: back 44×44, CTA 326×50, plus-zone 316×120, botones descanso
  158×46, detail-link 44 alto, FAB 46×46.
- Coherencia de datos entre frames: slot 2 con 1 serie hecha → segbar 25%, "Series 5/14 ·
  reps 6/10" coincide con frame E (serie 2, 6 reps); recap G 12/14 · 86% = 2 series omitidas.
- A11y en markup: `role=timer` + aria-label en ring, `role=status` sr-only "Serie 2 de 4 · 6
  reps de 10", chips color+texto+icono, hero `aria-hidden` con sr-only.

## Siguiente paso

Handoff a Pau (implementación FE de /training/session sobre esta spec + spec funcional
t_fc52ab22); QA puede auditar el HTML con la checklist de geometría anterior.
