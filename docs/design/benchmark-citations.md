# Verificación de citas — benchmark fitness (t_b6020ed5)

Fecha: 2026-10-23 · QA (Hermes) · Fuentes accedidas el 2026-10-23 (CEST).

Objeto: las afirmaciones de `docs/design/DESIGN.md` v1 (línea 3: "Investigación aplicada: Hevy…, WHOOP…, Apple Fitness…, Strava…, Pump Pal…") y su trazabilidad a fuentes verificables. El informe de benchmark (t_75c7b777) seguía en ejecución al cerrar esta verificación, así que aquí se valida lo que ya está escrito en el repo y se entrega el andamiaje de citas para el informe.

## 1. Veredicto por afirmación

| # | Afirmación (DESIGN.md v1) | Veredicto | Evidencia |
|---|---|---|---|
| 1 | Hevy = **dark-first** | ❌ **Contradicho** | 6/6 capturas oficiales de la App Store de Hevy son UI clara: 71–85 % de píxeles claros (#FFFFFF/#F6F6F6), solo 2,7–8,4 % oscuros. La ayuda oficial de Hevy documenta el tema como elección del usuario (claro/oscuro), no como modo por defecto. [F1][F2] |
| 2 | Hevy = **single-accent** | ✅ **Soportado en el concepto** | En las 6 capturas aparece un único color saturado dominante (#267EE8, 2–15 % de píxeles); ningún segundo matiz saturado. [F1] |
| 3 | Accent `#1E6FFF` ("Hevy Blue") | ❌ **Hex no trazable a Hevy** | El accent real de Hevy medido en sus capturas es **#267EE8** (6/6, estable). Su web de marketing usa #1D83EA/#1780EA. `#1E6FFF` no aparece en ningún asset de hevy.com ni en sus capturas. [F1][F3] |
| 4 | Hevy = **tabular-nums** | ⚠️ **No verificable** | Hevy no publica spec de diseño. La práctica sí es citable por la fuente tipográfica: Inter ofrece *tabular numbers* como OpenType feature. Recomienda re-atribuir a Inter/OpenType, no a Hevy. [F4] |
| 5 | WHOOP = **hero numbers** | ✅ **Soportado** | Guidelines oficiales: "Prefer to use **DINPro for numbers**", Proxima Nova para palabras → los números tienen tratamiento tipográfico propio. Las capturas muestran superficies oscuras con cifras grandes. [F5][F6] |
| 6 | WHOOP = **all-caps micro-labels** | ⚠️ **Parcial** | Confirmado en la fuente oficial solo el par de familias (DINPro/Proxima Nova). La regla "ALL CAPS + letter-spacing" no aparece en el texto del PDF oficial extraído; solo en descripciones de terceros. Necesita captura propia para sostenerla. [F5] |
| 7 | WHOOP = **hairline borders** | ❌ **Sin fuente** | No aparece en las guidelines oficiales. Lo que sí está documentado es la profundidad por luminosidad (gradiente de fondo #283339 → #101518) y superficies oscuras escalonadas medidas en capturas (#101518, #14191D, #111619, #1F282D, #292D30). Recomendar reformular como "profundidad por luminosidad, sin sombras". [F5][F6] |
| 8 | Readiness ramp rojo→amarillo→verde | ✅ **Concepto soportado; hexes distintos** | WHOOP oficial: High #16EC06 (100–67 %), Medium #FFDE00 (66–34 %), Low #FF0026 (33–0 %). DESIGN.md usa #2FBF71/#F5B83D/#F1545B: mismo semáforo, valores propios. Debe declararse como adaptación, no como "rampa WHOOP". [F5] |
| 9 | Apple Fitness = **surface ladder** | ✅ **Soportado a nivel de plataforma** | Apple HIG (Dark Mode): el sistema usa dos fondos — *base* y *elevated* — "to enhance the perception of depth when one dark interface is layered above another". Caveat: HIG define 2 niveles; DESIGN.md define 4 y los hexes son nuestros. [F7] |
| 10 | Apple Fitness = **calm type** | ❌ **Subjetivo / sin fuente** | No hay documento citable. Además, las capturas oficiales de Apple Fitness muestran un accent muy saturado (#A7FE01, anillos de Actividad), lo que debilita la lectura "calma cromática". Recomendar eliminar la atribución o reescribirla como decisión interna. [F8] |
| 11 | Strava = **record-first** | ✅ **Soportado** | Help oficial (10-sep-2026): "Navigate to the record screen by tapping **Record from the bottom navigation menu**"; "tap the **Start button near the bottom of the screen**". Coincide con el FAB central de quick-start de DESIGN.md. [F9] |
| 12 | Pump Pal = **1-tap quick start + FAB central** | ⚠️ **Fuente única (case study del diseñador)** | Case study de Ralph Dela Cruz (diseñador del producto, ene-2024→ene-2025): botón Quick Start a 1 toque en home sin wizard/confirmación, y botón central en la barra de nav que abre un popup de acciones rápidas. Sin corroboración independiente; Pump Pal no es benchmark de masa. [F10] |
| 13 | Dark-only por defecto (regla 6) | ⚠️ **Decisión, no hallazgo** | Ninguna de las apps de referencia es dark-only: Hevy es claro por defecto y ofrece tema; Apple Fitness alterna (capturas 23–58 % oscuro). WHOOP sí es dark-native (72–94 % oscuro). La regla es defendible solo apoyada en WHOOP + HIG dark mode, y debe presentarse como decisión de producto. [F1][F6][F7][F8] |

## 2. Evidencia cuantitativa (muestreo de píxeles, reproducible)

Método: descarga de capturas oficiales de App Store (600×1300) y conteo de píxeles por luminancia + colores saturados dominantes (PIL). Script y URLs en el anexo.

| App (capturas oficiales) | % oscuro (lum<60) | % claro (lum>180) | Superficies dominantes | Accent saturado |
|---|---|---|---|---|
| Hevy (6) | 2,7–8,4 | 71–85 | #FFFFFF, #F6F6F6 | **#267EE8** |
| WHOOP (4) | 72–94 | 1,9–4,7 | #101518, #14191D, #111619, #161B1F, #1F282D, #292D30 | #7CA1BB (sleep) |
| Apple Fitness (4) | 23–58 | 38–63 | #F2F2F2, #1C1C1C, #000000 | **#A7FE01** |

Complemento: assets de hevy.com (login Next.js) — `#008CFF` como azul; `#1E6FFF` ausente.

## 3. Hallazgo de procedencia (crítico para el rediseño)

Los tokens de DESIGN.md v1 (`#0E1116`, `#161B22`, `#1F2630`, `#283040`, `#262C36`, `#1E6FFF`, `#14233F`, `#F2F4F8`, `#2FBF71`, `#F5B83D`, `#F1545B`) coinciden **uno a uno** con el archivo `design-md/fitness/hevy/DESIGN.md` del repo `Meliwat/awesome-ios-design-md` (creado 2026-04-16, 614 ★, homepage spectr.to), que se describe a sí mismo como sistemas "reverse-engineered" pensados para "hand one to your AI agent". Su entrada de Hevy dice literalmente "Single-accent Hevy Blue `#1E6FFF` on a near-black training canvas `#0E1116`".

Consecuencia: la atribución "inspirado en Hevy" de DESIGN.md v1 **no es trazable a Hevy** (UI real clara, accent #267EE8) sino a un pack secundario generado para agentes. El mismo pack describe WHOOP con "#0A0A0A / neon Strain Green #00FF7B / DIN 2014", que **contradice** las guidelines oficiales de WHOOP (#000000/#FFFFFF branding, Teal #00F19F, Strain #0093E7, rampa #16EC06/#FFDE00/#FF0026, gradiente #283339→#101518, DINPro). Conclusión: ese pack **no es fuente fiable** para tokens; usar fuentes primarias (brand guidelines, help oficial, capturas propias).

## 4. Correcciones propuestas (sin añadir features)

1. Cambiar `#1E6FFF` → `#267EE8` (accent Hevy real) **o** declararlo color propio de Healthspan y quitar la atribución a Hevy. Lo mismo para `#14233F`.
2. Reescribir la línea 3 de DESIGN.md: `Hevy (single-accent, set-table-first) · WHOOP (dark-native, DINPro hero numbers, semáforo de recovery) · Apple HIG (superficies base/elevated, profundidad por luminosidad) · Strava (record-first en bottom nav) · Pump Pal (quick start 1 toque, FAB central)`.
3. Eliminar "dark-first" atribuido a Hevy y "calm type" atribuido a Apple Fitness (no sostenibles).
4. "Hairline borders" → "profundidad por luminosidad + borde 1px" (sostenido por Apple HIG dark mode y superficies WHOOP medidas).
5. Ramp de readiness: mantener los hexes propios pero citar la rampa oficial WHOOP como referencia conceptual y justificar la desviación (los verdes/rojos oficiales #16EC06/#FF0026 son agresivos sobre fondo oscuro).
6. Tabular-nums: citar Inter/OpenType, no Hevy.
7. Registrar explícitamente que **Fitbod y TrainingPeaks** (candidatos del benchmark) aún no tienen hallazgos verificados.

## 5. Evidencia pendiente (bloquea cerrar el benchmark)

- Capturas propias de Hevy (iOS, tema por defecto en instalación limpia) y de Fitbod/TrainingPeaks: sin ellas, "tema por defecto" queda inferido de material de marketing.
- Medición de tamaño de cifra hero en WHOOP (para justificar la escala 76/800 de DESIGN.md): falta captura con escala.
- Accesibilidad: contraste real de `#A9B1BE`/`#78818F` sobre `#0E1116` (no verificado contra WCAG; ninguna fuente de benchmark lo cubre).

## 6. Fuentes

- [F1] App Store — Hevy Workout Tracker (id1458862350), 6 capturas iOS analizadas por píxeles. https://apps.apple.com/us/app/hevy-workout-tracker-gym-log/id1458862350
- [F2] Hevy Help — "How to change the theme (light or dark)? (Android/iOS)". https://www.hevyapp.com/help/change-the-theme-android-ios
- [F3] Hevy marketing site (hexes #1D83EA/#1780EA) y CSS/JS de hevy.com (`#008CFF`; `#1E6FFF` ausente). https://www.hevyapp.com/ · https://hevy.com/login
- [F4] Inter (rsms) — OpenType features: "tabular numbers". https://rsms.me/inter/
- [F5] WHOOP Brand & Design Guidelines (PDF oficial): Proxima Nova para palabras, DINPro para números; Teal #00F19F (CTA), Strain #0093E7, Recovery Blue #67AEE6, High/Medium/Low #16EC06/#FFDE00/#FF0026, Sleep #7BA1BB, Background Gradient #283339→#101518. https://developer.whoop.com/assets/files/WHOOP%20-%20Brand%20&%20Design%20Guidelines-bdea3554e94b4ea09e68695b1e8dc8e7.pdf
- [F6] App Store — WHOOP 2.0 (id933944389), 4 capturas iOS (Sleep/Recovery/Strain/Healthspan) analizadas por píxeles. https://apps.apple.com/us/app/whoop-2-0/id933944389
- [F7] Apple Human Interface Guidelines — Dark Mode (base / elevated backgrounds para profundidad al superponer interfaces oscuras). https://developer.apple.com/design/human-interface-guidelines/foundations/dark-mode
- [F8] App Store — Apple Fitness (id1208224953), 4 capturas iOS analizadas por píxeles (#A7FE01). https://apps.apple.com/us/app/apple-fitness/id1208224953
- [F9] Strava Help Center — "How do I Record an Activity on Strava?" (Marie, 2026-09-10). https://support.strava.com/en-us/articles/15402137-how-do-i-record-an-activity-on-strava
- [F10] Ralph Dela Cruz — Pump Pal case study (diseñador del producto, ene-2024→ene-2025): Quick Start 1 toque + botón central de nav con popup. https://ralphdelacruz.com/pump-pal
- [F11] Meliwat/awesome-ios-design-md (procedencia de los tokens v1; creada 2026-04-16, spectr.to). https://github.com/Meliwat/awesome-ios-design-md · https://raw.githubusercontent.com/Meliwat/awesome-ios-design-md/main/design-md/fitness/hevy/DESIGN.md

## Anexo — script de muestreo

```python
# pip install pillow
import urllib.request, io, collections
from PIL import Image
def fetch(u):
    req = urllib.request.Request(u, headers={'User-Agent': 'Mozilla/5.0'})
    return Image.open(io.BytesIO(urllib.request.urlopen(req, timeout=40).read())).convert('RGB')
def lum(c): r, g, b = c; return 0.2126*r + 0.7152*g + 0.0722*b
im = fetch(URL); px = list(im.getdata()); n = len(px)
dark  = sum(1 for c in px if lum(c) < 60) / n * 100
light = sum(1 for c in px if lum(c) > 180) / n * 100
top   = collections.Counter(px).most_common(8)   # superficies
sat   = [(f"#{r:02X}{g:02X}{b:02X}", c) for (r, g, b), c in top
         if max(r, g, b) > 90 and max(r, g, b) - min(r, g, b) > 60]
```
