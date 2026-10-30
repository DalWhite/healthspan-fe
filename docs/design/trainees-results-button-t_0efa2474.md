# UX: 4º botón "Results" en /trainees (t_0efa2474)

**Decision-lock:** `docs/results-health-kpis-t_358e09e9.md` (API, @pau) + spec FE autoritativo
`docs/design/results-kpis-ux-t_33c412cf.md` §1 (línea 15) y §9.1 — QA t_268ca483 ya APROBÓ el
diseño (frame C del prototipo auditado en navegador: targets ≥44px, 0 overflow a 390px).
**Este doc no re-decide nada:** fija la implementación exacta del botón y su checklist visual
medible, lista para Pau (código en t_e1eef40f, no en esta card UX).

## 1. Cambio exacto (único archivo: `Pages/Trainees/Trainees.razor`)

Añadir un 4º `MudItem`/`MudButton` tras "Rutinas", mismo patrón que los 3 existentes
(`Variant.Outlined`, `Color.Secondary`, `FullWidth`):

```razor
<MudItem xs="12" sm="6">
    <MudButton Variant="Variant.Outlined" Color="MudBlazor.Color.Secondary"
               FullWidth="true"
               OnClick='@(() => NavigationManager.NavigateTo(TraineeRoute.Results(trainee.Id)))'>
        Results
    </MudButton>
</MudItem>
```

Y en `Helpers/TraineeRoute.cs` (source of truth de rutas de trainee — no incrustar el string
en la página, coherencia con `TypeDetailsRoute`):

```csharp
public static string Results(string? traineeId)
    => $"/trainees/{Escape(traineeId)}/results";
```

- **Copy:** `Results` (literal, como en el prototipo frame C y el lock; no "Resultados").
- **Orden:** Exercises → Favourites → Rutinas → **Results** (último; los otros 3 no se tocan).
- **Navegación:** `OnClick` → `NavigateTo`, igual que los vecinos (no `Href`, para no cambiar
  el patrón de SPA navigation ya auditado).
- 0 librerías, 0 lógica de negocio, CSS existente.

## 2. Por qué este diseño (una línea por decisión)

- *Mismo variant/estilo que los 3 existentes* → cero fricción de aprendizaje; Results se lee
  como una acción más de la ficha, no comofeature nueva ajena.
- *xs="12" sm="6"* (una fila por botón en móvil, 2 columnas en ≥sm) → hereda el layout ya
  auditado sin overflow a 390px; el grid solo gana una fila.
- *Route helper en TraineeRoute* → la ruta Results queda en el mismo sitio canónico que el
  resto de rutas de trainee; un único punto si cambia.

## 3. Riesgo UX único y decisión

La ruta destino `/trainees/{id}/results` **no existe todavía en ningún branch** (page = t_e1eef40f).
Botón publicado antes que la página = destino 404 → experiencia rota (regla nº1 de ux-ui-good-apps).
**Decisión:** este botón se fusiona en el MISMO PR que la página Results (o después). No antes.

## 4. Checklist visual del decision-lock (medible, para QA/t_e1eef40f)

A 390×844 con seed `qa@dev.local` en `/trainees`, sobre la 1ª tarjeta:

| # | Criterio | Cómo se mide (paso a paso en `qa-t0efa/verify_results_button.cjs`, ya escrito) |
|---|---|---|
| 1 | 4 botones, textos `Exercises/Favourites/Rutinas/Results` | `.mud-card:first .mud-card-actions .mud-button-root` → texts |
| 2 | Results target ≥44px | `getBoundingClientRect().height ≥ 44` (el CSS global `--hs-touch-target` ya lo fuerza en `.hs-page-shell`) |
| 3 | Nombre accesible = "Results" | snapshot AX de la card; sin `aria-label` extra necesario (texto visible = nombre) |
| 4 | Foco visible | `focus()` + `:focus-visible` outline del token existente (regla `.mud-button-root:focus-visible` ya en CSS) |
| 5 | Sin overflow | `documentElement.scrollWidth == 390` |
| 6 | Navegación | click → `location.pathname == /trainees/{id}/results` |

## 5. Handoff

Implementación y E2E viven en **t_e1eef40f** (pau). Script de verificación QA reutilizable:
`/home/david/qa-t0efa/verify_results_button.cjs` (Playwright, 390×844, pasos 1–6 de arriba).
