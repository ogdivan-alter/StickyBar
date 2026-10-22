# Seguimiento de Etapas — Sticky Dock Utility

Tablero de estado del plan definido en `docs/plan/PLAN.md`.

- **Propietario**: Director de Desarrollo
- **Última actualización**: 2026-10-22
- **Etapa actual**: E0 (no iniciada)

---

## Tablero de estado

| Etapa | Nombre | Estado | Avance | Actualizado | Depende de | Bloqueo |
| --- | --- | --- | ---: | --- | --- | --- |
| E0 | Cimientos de solución y build | No iniciada | 0 % | 2026-10-22 | — | — |
| E1 | Ventana base, tema y pestaña | No iniciada | 0 % | 2026-10-22 | E0 | — |
| E2 | Sticky docking y barra de tareas | No iniciada | 0 % | 2026-10-22 | E1 | — |
| E3 | Hover, slide-in y gracia | No iniciada | 0 % | 2026-10-22 | E2 | — |
| E4 | Cintas y despliegue exclusivo | No iniciada | 0 % | 2026-10-22 | E2 | — |
| E5 | Accesos directos `.lnk` | No iniciada | 0 % | 2026-10-22 | E1, E4 | — |
| E6 | Bandeja, configuración y auto-inicio | No iniciada | 0 % | 2026-10-22 | E1 | — |
| E7 | Multimonitor y réplica | No iniciada | 0 % | 2026-10-22 | E2, E5, E6 | — |
| E8 | Rendimiento, endurecimiento y entrega | No iniciada | 0 % | 2026-10-22 | E0–E7 | — |

**Estados válidos**: `No iniciada` · `En curso` · `En revisión` · `Bloqueada` · `Completada`

---

## Bitácora de seguimiento

| Fecha | Etapa | Evento | Cambio / Resultado | Responsable |
| --- | --- | --- | --- | --- |
| 2026-10-22 | — | Planificación inicial | Se crea el plan de 9 etapas (E0–E8) a partir de RF01–RF05, RNF y los CA de HU01–HU05. Se registra ADR-001 en estado *Propuesta*. | Director de Desarrollo |
| 2026-10-22 | — | Puesta en marcha del rol | Se crea el agente `director-desarrollo` (agente por defecto) con el equipo `analista`, `arquitecto`, `implementador` y `qa`, los comandos `/planear`, `/avanzar` y `/estado`, y este tablero de seguimiento. | Director de Desarrollo |
| 2026-10-22 | E5 | Resolución de Q-01 | Se decide que solo se admiten `.lnk` por drag & drop; el resto de tipos se rechaza con mensaje (HU04 CA #2). E5 queda desbloqueada. | Usuario |
| 2026-10-22 | E0 | Resolución de Q-02 | Se adopta WPF sobre .NET 8. ADR-001 pasa a estado *Aceptada* con el detalle de la decisión y la deuda asumida. E0 queda desbloqueada. | Usuario |
| 2026-10-22 | E3 | Resolución de Q-03 | Hover fijo en 200 ms; temporizador de gracia con 400 ms por defecto y rango configurable de 300–500 ms. E3 queda desbloqueada. | Usuario |
| 2026-10-22 | E7 | Resolución de Q-04 | Se implementa solo el modo réplica espejada. La «configuración individual independiente» de RF05 se registra como desviación de alcance (I-04). E7 queda desbloqueada. | Usuario |

---

## Riesgos abiertos

| ID | Riesgo | Etapa | Probabilidad | Impacto | Mitigación | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| R-01 | El runtime de Windows App SDK complica el empaquetado en Windows 10 | E0, E8 | — | — | Cerrado por Q-02: con WPF no hay dependencia de runtime externo | **Cerrado** |
| R-02 | `AllowsTransparency` encarece el coste en reposo y pone en riesgo el RNF de RAM | E1 | Media | Medio | Medición de CPU/RAM ya incluida en los CA de E1 | Abierto |
| R-03 | Interop COM de `IShellLink` con *marshaling* de iconos | E5 | Media | Alto | Etapa aislada y con prueba dedicada de un `.lnk` con argumentos | Abierto |
| R-04 | Sincronización de estado entre las réplicas multimonitor | E7 | Media | Medio | Un único origen de verdad en el proyecto de núcleo | Abierto |
| R-05 | *Work area* y DPI mixto con varias escalas de pantalla | E2, E7 | Alta | Medio | Pruebas en configuración de dos monitores con escalas distintas | Abierto |

---

## Preguntas abiertas al usuario

Estas preguntas bloquean el cierre de las etapas indicadas. El Director de Desarrollo **no las resuelve por su cuenta**.

### Q-01 — Tipos de acceso directo admitidos

- **Estado**: **Resuelta**
- **Decisión**: **Solo `.lnk`**. Se aplica §4.5 y HU04 al pie de la letra.
- **Fecha**: 2026-10-22 · **Decidido por**: usuario
- **Bloquea**: E5 (desbloqueada)
- **Contradicción detectada**:
  - **RF04** (§2): «Compatibilidad completa con archivos `.lnk`, ejecutables `.exe`, carpetas del sistema y URIs».
  - **§4.5**: «Acepta exclusivamente archivos de acceso directo (`.lnk`). Se restringe el ingreso directo de ejecutables (`.exe`), carpetas o scripts directamente sin estar empaquetados en un archivo `.lnk`».
  - **HU04 CA #2**: al arrastrar un `.exe` o una carpeta, «el sistema restringe la acción indicando que debe empaquetarse previamente como acceso directo».
- **Opciones consideradas**:
  1. **Solo `.lnk`** — **ELEGIDA**. Es la lectura más restrictiva y la que satisface HU04 sin reescribir la especificación.
  2. `.lnk` en la entrada, `.exe`/carpeta/URI admitidos internamente — descartada: contradice HU04 CA #2 tal como está redactada.
  3. Admisión ampliada con confirmación (ofrecer crear el `.lnk`) — descartada: exigiría una redacción nueva para HU04 CA #2.
- **Efecto en la implementación**:
  - La entrada por drag & drop acepta **únicamente** archivos `.lnk`.
  - Cualquier otro tipo (`.exe`, carpeta, script, URI) se rechaza con un mensaje que indique que debe empaquetarse previamente como acceso directo (HU04 CA #2).
  - La línea de RF04 sobre `.exe`, carpetas y URIs se considera **no vigente** para la vía de ingreso por arrastre. La especificación no se modifica (es de solo lectura); esta decisión es la que prevalece.
  - La mención a que «§4.5 también permite configurar URIs» se resuelve en el mismo sentido: una URI se admite **como destino ya empaquetado dentro de un `.lnk`**, nunca como elemento arrastrado directamente.

### Q-02 — Framework de UI: WinUI 3 o WPF

- **Estado**: **Resuelta**
- **Decisión**: **WPF sobre .NET 8**. Se adopta la Opción A de `docs/plan/decisiones/ADR-001-stack-y-arquitectura.md`, que queda en estado *Aceptada*.
- **Fecha**: 2026-10-22 · **Decidido por**: usuario
- **Bloquea**: E0 (desbloqueada)
- **Criterio de decisión, por peso**:
  1. **Compatibilidad y distribución** — ejecutable autocontenido, sin runtime de Windows App SDK en Windows 10 y sin fricción de empaquetado MSIX ni firma.
  2. **Interop Win32** — `HwndSource.AddHook`, `SetWinEventHook` y `SHAppBarMessage` tienen patrones maduros, lo que reduce el riesgo de RF01 y HU01.
  3. **RNF de reposo** — la huella de memoria base de WPF deja más margen bajo el límite de 50 MB.
- **Opción descartada**: WinUI 3 sobre .NET 8 (Windows App SDK). La comparación documentada queda en el ADR-001.
- **Deuda asumida**:
  - No hay Mica/Acrylic nativos. La translucidez se resuelve con opacidad por píxel y, si se necesita, desenfoque de fondo vía `DwmEnableBlurBehindWindow`.
  - El estilo visual Fluent hay que construirlo a mano o con una biblioteca ligera.
  - `AllowsTransparency="true"` puede encarecer el renderizado: la medición de RAM y CPU en reposo figura como criterio de aceptación de E1. Si incumpliera el RNF, el ADR-001 debe revisarse.

### Q-03 — Valores por defecto de los temporizadores

- **Estado**: **Resuelta**
- **Decisión**:
  - **Hover: 200 ms, fijo.** No es configurable, tal como lo fija RF02 y §4.3.
  - **Gracia: 400 ms por defecto, configurable en el rango 300–500 ms.**
- **Fecha**: 2026-10-22 · **Decidido por**: usuario
- **Bloquea**: E3 (desbloqueada)
- **Justificación**: 400 ms es el punto medio del rango de §4.3, con margen suficiente para moverse entre la barra desplegada y sus cintas sin provocar un colapso accidental, que es la razón de ser del temporizador de gracia.
- **Opciones descartadas**: 300 ms (colapso demasiado rápido, riesgo de cierre accidental durante la navegación) y 500 ms (barra demasiado tiempo desplegada tras salir el cursor). Hacer configurable el hover queda fuera: la especificación lo fija en 200 ms.

### Q-04 — Alcance de la réplica multimonitor

- **Estado**: **Resuelta**
- **Decisión**: **Solo modo réplica**. Se implementa la réplica espejada; la «configuración individual independiente» de RF05 **queda fuera de alcance**.
- **Fecha**: 2026-10-22 · **Decidido por**: usuario
- **Bloquea**: E7 (desbloqueada)
- **Justificación**: §4.6 y HU05 CA #2 describen únicamente el caso de réplica. Implementar solo ese modo cubre el criterio de aceptación verificable y reduce el coste de la sincronización multi-monitor (riesgo R-04).
- **Desviación registrada**: RF05 dice «Opción para habilitar **réplica o** configuración individual independiente en monitores adicionales». Con esta decisión, **la segunda mitad de esa frase no se implementa**. Es una reducción de alcance consciente, no un olvido: si en el futuro se necesita, se trata como una etapa nueva (ver I-04).
- **Efecto en la implementación**:
  - La configuración expone una opción por monitor para activar o desactivar su presencia, y con ella la réplica.
  - El contenido replicado es **idéntico** en todos los monitores: mismas cintas, mismos accesos directos y mismo orden.
  - Cada barra se acopla al borde libre de **su** monitor y respeta el *work area* de ese monitor; esos elementos son siempre por monitor, con independencia del modo de réplica.

---

## Ideas fuera de alcance

Registradas para una evaluación posterior. **No se implementan** hasta que se aprueben.

| ID | Idea | Origen | Estado |
| --- | --- | --- | --- |
| I-01 | Agrupación de cintas por perfil de trabajo | Propuesta propia | Pendiente |
| I-02 | Exportación e importación de la configuración en JSON | Propuesta propia | Pendiente |
| I-03 | Posicionamiento de la barra por arrastre con *snap* en las esquinas | §4.4 sugiere el comportamiento de apertura | Pendiente |
| I-04 | Configuración independiente por monitor (cintas y accesos directos distintos en cada pantalla) | RF05, segunda mitad de la frase; excluida por Q-04 | Pendiente |

---

## Resumen para reporte

- **Etapas completadas**: 0 de 9.
- **Etapa en curso**: ninguna.
- **Próxima acción**: no quedan decisiones pendientes. Ejecutar la **Fase B** (consolidación de la matriz de trazabilidad con el subagente `analista`) y la **Fase C** (ejecución de la etapa E0 con `implementador`).
- **Bloqueos que requieren tu decisión**: ninguno.
