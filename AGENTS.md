# Sticky Dock Utility — Guía de proyecto

Aplicación de escritorio nativa **Windows 10/11 x64** que ofrece una barra flotante de accesos directos, adherible a los bordes de pantalla y organizada en **Cintas**.

## Fuentes de verdad

| Documento | Contenido |
| --- | --- |
| `requerimiento/Especificación de Requerimientos y Alcance - Sticky Dock Utility.md` | RF01–RF05, RNF, UI/UX (§4), arquitectura (§5) |
| `requerimiento/Product Backlog e Historias de Usuario - Sticky Dock Utility.md` | EP01–EP04, HU01–HU05 y Criterios de Aceptación |
| `docs/plan/PLAN.md` | Etapas de implementación y sus criterios de aceptación |
| `docs/plan/ESTADO.md` | Seguimiento: estado, bitácora, bloqueos, preguntas abiertas |
| `docs/plan/decisiones/` | ADRs (decisiones de stack, arquitectura y modelo de datos) |

Los documentos de `requerimiento/` **son intocables**. Ante ambigüedad o contradicción: se registra como *Pregunta Abierta* en `docs/plan/ESTADO.md` y se consulta al usuario. El código se ajusta al requerimiento, nunca al revés.

## Flujo de trabajo

El rol de **Director de Desarrollo** (`.opencode/agents/director-desarrollo.md`) planifica, delega y hace seguimiento. Los subagentes disponibles:

- `analista` — trazabilidad RF → HU → CA y detección de huecos. Solo lectura.
- `arquitecto` — decisiones de stack, estructura, MVVM, Win32 y persistencia; escribe ADR.
- `implementador` — ejecuta **una** etapa del plan y la deja verificada.
- `qa` — valida los criterios de aceptación de una etapa con evidencia. Solo lectura.

Comandos disponibles: `/planear`, `/avanzar`, `/estado`.

**Interacción**: las decisiones pendientes se preguntan **de una en una**, esperando la respuesta antes de formular la siguiente. El registro es español neutro, sin voseo.

## Convenciones de código

- **Idioma**: comunicación y documentación en español. Código, identificadores, comentarios y mensajes de commit en inglés.
- **Framework**: .NET 10 (C# 14, `net10.0-windows`) con **WPF**. Decidido en `docs/plan/decisiones/ADR-001-stack-y-arquitectura.md` (estado *Aceptada*) y en la pregunta Q-05 de `docs/plan/ESTADO.md`. Ventana principal: `WindowStyle="None"` + `AllowsTransparency="true"` + `WindowChrome`; bandeja con `System.Windows.Forms.NotifyIcon`; translucidez con `DwmEnableBlurBehindWindow` (sin Mica/Acrylic nativos).
- **MVVM**: la lógica de estado, el docking, las cintas, los temporizadores y las animaciones viven en ViewModels y servicios, nunca en el code-behind de las vistas.
- **Interop Win32**: todo `P/Invoke` a `User32.dll` / `Shell32.dll` vive en el proyecto de interoperabilidad, con tipos seguros. No usar `IntPtr` desnudo en la lógica de negocio.
- **Persistencia**: JSON versionado en `%AppData%`, con migración de esquema y escritura atómica.
- **RNF**: RAM < 50 MB en operación normal medidos como **memoria privada comprometida** (`PrivateBytes`, Q-08; el *working set* es informativo) y CPU < 1 % en reposo. Presupuesto interno de 42 MB de peor caso. Revisar timers, polling, hooks y animaciones permanentes en cada revisión.

## Reglas de contribución

- Cada cambio se trazabiliza a un ID de RF / HU / CA. Sin trazabilidad, no es revisable.
- El build y las pruebas deben pasar antes de dar por terminada cualquier etapa.
- Toda desviación de los requerimientos se documenta como ADR; nunca como cambio silencioso.
- Nada de funcionalidad no solicitada: las ideas valiosas fuera de alcance se registran y se consultan antes de implementarlas.

## Control de versiones

- Repositorio git en la rama `main`. Cada etapa del plan cierra con **un commit** que la implementa, más commits incrementales cuando la etapa sea grande.
- Los mensajes de commit van en **inglés**, con prefijo convencional (`feat`, `fix`, `docs`, `chore`, `refactor`, `test`).
- El cuerpo del commit explica el **qué** y el **por qué**, y referencia los IDs de RF / HU / CA afectados.
- `AGENTS.md`, `docs/plan/` y `.opencode/` son parte del producto documental: se versionan junto al código.
- El árbol de trabajo debe quedar **limpio** al cerrar una etapa. El Director lo verifica antes de darla por completada.

```bash
git add -A
git commit -F <archivo-de-mensaje>
```

> En PowerShell, `git commit -F -` con *here-string* no funciona: escribe el mensaje en un archivo y pásalo con `-F <ruta>`.

