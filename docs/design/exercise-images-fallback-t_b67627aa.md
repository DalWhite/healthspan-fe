# Decisión: fallback de imágenes de ejercicios (t_b67627aa)

Estado: **decidida** · Aplica a: /training/library, /training/exercises/{id}, cualquier card de ejercicio.

## Regla

**La URL de la foto sale únicamente de `ImageRoute`.**

- `ImageRoute` no vacío → se sirve tal cual.
- `ImageRoute` vacío/nulo → **placeholder**, y **no se construye ninguna ruta**.

Prohibido derivar la URL del nombre (`$"ExerciseImages/{Name}.jpg"`). Si no hay dato, no hay petición.

## Evidencia

| Hecho | Dato |
|---|---|
| Catálogo dev (raíz) | 715 ejercicios raíz |
| Con `ImageRoute` | 873 / 876 (99.7 %) |
| Sin `ImageRoute` | 3: Kettlebell Halo, Kettlebell Halo with Overhead Extension, Kettlebell Overhead Triceps Extension |
| Formato de `ImageRoute` | `https://raw.githubusercontent.com/yuhonas/free-exercise-db/...` (muestra aleatoria 15/15 → HTTP 200) |
| Nombres en catálogo | inglés (`Ab Crunch Machine`, `Around The Worlds`); los 404 de QA venían de un catálogo con nombres en español |
| `wwwroot/ExerciseImages/` | 7 ficheros legacy; ninguno coincide con un `Name` del catálogo actual |

El "asset conocido" ya existe y es **dato** (`ImageRoute`), no un nombre de fichero. Una convención por nombre es inestable por diseño: cualquier renombrado o import de catálogo (p. ej. inglés → español) invalida todas las URLs de golpe, y cada tarjeta sin foto cuesta un 404 por visita.

## Implementación (t_8ffed894)

1. Un helper único (`GetExerciseImageUrl(string? imageRoute)` → `imageRoute` si no está vacío, si no `null`) y usarlo en los dos sitios que hoy derivan del nombre:
   - `Components/ExerciseTypeGrid.razor::GetImageUrl`
   - `Pages/ExerciseTypes/ExerciseTypeDetails.razor::HeroImageUrl`
   - `Pages/Today/TodayWorkout.razor` ya es data-driven: no tocar.
2. Render con `<img>` **solo** si hay URL; si no, se deja únicamente el contenedor con el placeholder CSS ya existente (`.hs-et-card-photo`, fondo `icon-512.png`). Sin `<img>`, sin `role`, sin `aria-*` extra: el nombre ya lo lleva la tarjeta (`aria-label="View X stats"` + `<h6>`).
3. `alt=""` en la foto: es decorativa (el nombre está duplicado en el título y en el `aria-label` de la card).
4. Mantener `onerror="this.remove()"` y `loading="lazy"`: un fallo de red remoto colapsa al **mismo** estado visual que "sin foto". Un solo aspecto para "no hay foto" y "la foto falló".
5. No tocar `wwwroot/ExerciseImages/`: los 7 ficheros se quedan (siguen siendo servibles si algún registro apunta ahí vía `ImageRoute`). No borrar, no añadir.

## Assets nuevos / priorización

**No se añaden assets.** El catálogo remoto ya está poblado; los 3 registros sin `ImageRoute` pasan a placeholder y no se inventan fotos. Priorizar por frecuencia de uso es prematuro sin métricas: si más adelante se quiere robustez, **tarjeta aparte** para espejar las imágenes en nuestro storage (el hotlink a `raw.githubusercontent.com` es frágil en producción), fuera del alcance de este fix.

## QA (t_31fcd3ac) — criterios de aceptación

- 0 peticiones a `/ExerciseImages/*` desde `/training/library` y `/training/exercises/{id}` (proxy log).
- 0 respuestas 404 same-origin. Los 404 de `raw.githubusercontent.com` son calidad de dato, no de código: se reportan aparte y no bloquean.
- Tarjeta sin foto: `div.hs-et-card-photo` **sin** `<img>` dentro, mismo tamaño que una tarjeta con foto (sin salto de layout ni hueco), placeholder visible.
- Tarjeta con foto: `<img>` con `alt=""` y `loading="lazy"`, una sola petición por tarjeta.
- Prueba de degradación: forzar un `src` roto → la imagen desaparece y queda el placeholder, sin 404 same-origin.

## UX (t_d9ac8ff8)

Validar que el placeholder se lee como deliberado (icono sobre el fondo de la tarjeta) y no como imagen rota. Si al verlo resulta pobre, se propone cambio de estilo como tarjeta nueva; no se rediseña dentro de la implementación.
