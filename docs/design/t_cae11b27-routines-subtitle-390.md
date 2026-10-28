# t_cae11b27 — Subtítulo de /training/routines cortado a 390px

## Diagnóstico (caíz raíz, no un ancho fijo)

El subtítulo no tenía un `width: 482px`. El corte venía de una **ocultación
fake-sr-only mal implementada** en `wwwroot/css/app.css` (media ≤768px):

```css
.hs-page-header-title-only,
.hs-page-header-with-actions .hs-page-header-copy {
  width: 1px; overflow: hidden; clip: rect(0 0 0 0);
  white-space: nowrap;   /* ← el veneno */
}
```

`.hs-page-header-copy` es un *flex item* del header con `white-space: nowrap`
heredado → su ancho intrínseco pasa a **max-content (~482px)**, escala por
encima del contenedor de 1px y como `clip` ya no recorta (deprecado, sigue
recortando pero el item vive fuera), el rect medido era `left 11 → right 493`
a 390px de viewport: texto invisible **y** elemento fuera del viewport.

## Decisión de diseño (por qué "visible", no "mejor oculto")

Opción A (técnica): arreglar el sr-only con `clip-path: inset(50%)` + `unset`
de nowrap. Opción B (UX): **las páginas que declaran Subtitle siempre muestran
su copy en móvil**.

Se elige **B**, y se justifica por resultado de usuario:

1. El subtítulo de routines es contenido accional — *"Follow them in Training
   › Today."* dice **qué hacer después**. Ocultarlo en móvil rompe el flujo
   assignment → ejecución: el usuario móvil es el caso de uso principal de un
   plan asignado.
2. La regla "ocultar copy en móvil" solo tenía sentido para headers sin
   sustancia (título = eco del nav). Con subtítulo, ocultar elimina identidad
   de página (D6: el `<h1>` también quedaba oculto a 390px).
3. Hay precedente en el propio diseño system: `.hs-today-page` ya exime al
   header de Today de esta regla. Se generaliza como componente, no como
   parche por página.

Tradeoff aceptado: +~2 líneas verticales en el header móvil de las 6 páginas
con subtítulo (routines, today, trainees, workouts, profile, health/dashboard,
health/blood-tests). Es ganancia neta: contexto visible, cero desbordamiento.

## Cambio

- `Components/PageHeader.razor`: cuando `Subtitle` no está vacío, el header
  recibe `hs-page-header-keeps-copy` (regla de componente, no por página).
- `wwwroot/css/app.css` (≤768px): las reglas sr-only se excluyen con
  `:not(.hs-page-header-keeps-copy)`. No hace falta regla de layout nueva:
  el header móvil base ya es `flex-wrap: wrap` y `.hs-page-header-copy` lleva
  `min-width: 0` → el subtítulo partea líneas y se queda dentro de 390.

## Verificación (harness autoritativo a11y_run7, 390×844)

- `/training/routines`: subtítulo visible en 2 líneas dentro del viewport,
  `document.scrollWidth = 390`, `overflowEls = 0`.
- Páginas con subtítulo + acciones (`/profile`, `/health/dashboard`,
  `/health/blood-tests`, `/trainees`): mismo resultado; las acciones pasan a
  su propia línea (wrap) en vez de solaparse.
- Páginas sin subtítulo: comportamiento idéntico a antes (copy compactada).
- Capturas y JSON en `qa-evidence/t_cae11b27/`.
