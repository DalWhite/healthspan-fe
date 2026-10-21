# Healthspan Design System v1 (aprobado por Wall 2026-10-21)

Investigación aplicada: Hevy (dark-first, single-accent, tabular-nums), WHOOP (hero numbers, hairline borders, all-caps micro-labels), Apple Fitness (surface ladder, calm type), Strava (record-first), Pump Pal (1-tap quick start, FAB central).

Mock de referencia visual: `docs/design/redesign-mock.html` (abrir en navegador; screenshot: redesign_mock.png).

## 1. Color tokens

Canvas y superficies (escalera, SIN sombras — profundidad por luminosidad + borde 1px):
- Canvas: `#0E1116`
- Card/superficie 1: `#161B22`, borde `#262C36`
- Input/superficie 2: `#1F2630`
- Raised/pressed: `#283040`
- Scrim overlays: `rgba(0,0,0,.55)`

Acción (único color accionable en toda la app):
- Primary/accent: `#1E6FFF` (CTAs, tabs activos, títulos de ejercicio editables, FAB, timer)
- Accent soft (fondos tintados): `#14233F`

Semánticos (solo para su significado, nunca decorativos):
- Done/completado: `#2FBF71` (fila completada = wash 16% + check relleno)
- PR/hito: `#F5B83D` (badge ★, wash 18%)
- Error: `#F1545B`
- Readiness ramp: rojo→amarillo→verde (`#F1545B` / `#F5B83D` / `#2FBF71`)

Texto:
- Primary `#F2F4F8` · Secondary `#A9B1BE` · Tertiary/disabled `#78818F`

## 2. Tipografía

- Familia: **Inter** (400/500/600/700/800). Cargar vía Google Fonts; fallback system-ui.
- **`font-variant-numeric: tabular-nums` GLOBAL** (body). Ningún número debe reflowar al cambiar.
- Escala: título de pantalla 30/800 tracking -0.6 · nombre entreno 16-17/700 · nombre ejercicio 15/700 (en accent color = es también affordance de edición) · métrica hero 76/800 tracking -3 · valor tile 24/800 · body 14-15/500 · meta 12.5/500 secondary · micro-labels 11/700 UPPERCASE tracking +0.6-1.4 (solo labels de columna, chips, hero-labels; nunca body ni títulos).

## 3. Componentes

- **Card**: bg card, radio 14, borde 1px, padding 16. Sin sombra.
- **CTA primario**: accent, radio 12, padding 15, 700/16. **CTA secundario**: accent-soft bg + accent text.
- **Chip**: radio 8, 11.5/700, UPPERCASE, wash 16-18% del color semántico.
- **Bottom sheet**: radio superior 20, sombra `0 -12px 32px rgba(0,0,0,.6)`.
- **Set table**: columnas Serie | Anterior | Reps/kg | ✓; celdas editables bg input radio 8; fila completada wash done 10% + texto done; "Anterior" tertiary 12.5 (pre-fill del valor de la última sesión).
- **FAB**: 46px, radio 14, accent, glow `0 0 18px rgba(30,111,255,.45)`, centrado en la nav inferior, acción = registrar/quick-start desde cualquier pantalla.
- **PR badge**: chip gold "★ PR" inline en la fila.

## 4. Navegación

- **Móvil (<1000px)**: bottom bar 5 slots: Hoy · Historial · **FAB +** · Insights · Biblioteca. Bar `rgba(14,17,22,.92)` + blur, borde superior 1px. Tab activo en accent.
- **Desktop (≥1000px)**: sidebar con los mismos 5 destinos en el mismo orden. Nunca dos sistemas de navegación simultáneos.
- Terminología user-task: Hoy, Historial, Insights, Biblioteca.

## 5. Pantallas (spec)

1. **Hoy** = pantalla de activación. CTA "▶ Empezar entreno" (1 toque, sin wizard) + "↻ Repetir «X» · hace N días" (copia prefilled editable) + 2 últimas sesiones compactas + racha discreta (chip, sin presión).
2. **Entreno en curso**: timer accent arriba; bloques como cards (Superserie/Ronda n/m chip); ejercicios con thumb 52px + nombre accent; set tables con "Anterior"; intentos como dots (✗ fallido gold completado) + contador 3/8; chip de descanso automático; FAB verde Finalizar.
3. **Resumen post-entreno**: hero duración, tiles (series, intentos), badge PR si aplica, slider fatiga 1-5 (done wash), chips de delta por adaptación Galpin, CTA "Volver a Hoy" (el usuario aterriza en contexto, no en catálogo).
4. **Insights**: hero readiness (número 76/800, color según rampa) + radar 9 ejes Galpin (orden canónico, relleno accent 22%) + sugerencias con dot semántico y lenguaje de interferencia.
5. **Historial**: tira de calendario 7 días (punto accent = día con entreno), lista de sesiones (nombre, fecha+duración+adaptaciones, volumen a la derecha), tiles semanales (entrenos, racha).
6. **Biblioteca**: search arriba, grid 2 col, card image-first (foto full-width 110px, nombre canónico, 1 línea muscle·equipamiento·N variantes). Cero chips/botones en la card.

## 6. Reglas

- Dark-only por defecto (un toggle claro opcional después, no en este sprint).
- Un solo accent; los semánticos no decorativos.
- Errores: inline, on-blur, específicos, sin culpar, input preservado (NN/g).
- Flows: ≤4 taps de entrada a done.
- 44px targets mínimos en móvil.
- Loading/empty/error states intencionales en cada pantalla.

## 7. Implementación MudBlazor

- `MudTheme` custom en Program.cs: Primary #1E6FFF, Background #0E1116, Surface #161B22, Secondary/Drawer #161B22, AppbarElevation 0, radius 12-14.
- CSS global en app.css: tokens como variables, tabular-nums global, clases `hs-*` para set table/chips/FAB.
- Reemplazar MudAppBar+MudDrawer por: bottom nav fija (mobile) / MudNavMenu sidebar (desktop) con breakpoint 1000px.
- Inter vía index.html Google Fonts link.
