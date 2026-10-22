# Seguimiento de Etapas — Sticky Dock Utility

Tablero de estado del plan definido en `docs/plan/PLAN.md`. El inventario normalizado de requisitos y las matrices de trazabilidad viven en `docs/plan/TRAZABILIDAD.md`.

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
| 2026-10-22 | E0 | Resolución de Q-02 | Se adopta WPF (la versión de .NET se fija en Q-05). ADR-001 pasa a estado *Aceptada* con el detalle de la decisión y la deuda asumida. E0 queda desbloqueada. | Usuario |
| 2026-10-22 | E3 | Resolución de Q-03 | Hover fijo en 200 ms; temporizador de gracia con 400 ms por defecto y rango configurable de 300–500 ms. E3 queda desbloqueada. | Usuario |
| 2026-10-22 | E7 | Resolución de Q-04 | Se implementa solo el modo réplica espejada. La «configuración individual independiente» de RF05 se registra como desviación de alcance (I-04). E7 queda desbloqueada. | Usuario |
| 2026-10-22 | E0 | Control de versiones | Se inicializa el repositorio git en la rama `main`, con `.gitignore` y `.gitattributes` para .NET/WPF. Commit inicial con la especificación, el plan y la configuración del agente. | Director de Desarrollo |
| 2026-10-22 | E0 | Resolución de Q-05 | Se adopta .NET 10 (`net10.0-windows`) como TFM de destino: única versión LTS en soporte activo y única instalada en el equipo. Cumple «.NET 8 o superior» de §5.1. | Usuario |
| 2026-10-22 | — | Fase B: informe de trazabilidad | El `analista` consolida el inventario (5 RF, 5 RNF, 23 requisitos §4/§5, 5 HU, 11 CA) y entrega matrices directa e inversa. Veredicto: los 11 CA tienen cobertura completa, pero aparecen 14 huecos de trazabilidad, 3 contradicciones y 7 ambigüedades. | Analista de Requerimientos |
| 2026-10-22 | E0–E8 | Corrección del plan tras la Fase B | Se añaden CA que faltaban: RAM en reposo en E1 (T-02), arranque en Windows 10/11 x64 en E8 (T-01), cambio de resolución en E2 (T-03), creación/renombrado/eliminación de Cintas en E4 (T-04), despliegue desde el cuadro indicador en E4 (T-05), inspección MVVM en E0 (T-11). Se corrigen las trazas de E2 y E6 (T-12, T-13) y se marcan como criterios de robustez los de desconexión de monitor en E7. | Director de Desarrollo |
| 2026-10-22 | — | Interpretaciones y ampliaciones registradas | Se registran 9 ambigüedades resueltas por el propio documento (C2, C3, A1–A7) y 2 ampliaciones de alcance (AMP-01 persistencia del borde y la gracia, AMP-02 JSON en lugar de SQLite). Se registra I-05 (Mica/Acrylic) como deuda de ADR-001. | Director de Desarrollo |
| 2026-10-22 | E4, E5 | Nuevas preguntas abiertas | La Fase B deja dos contradicciones que requieren decisión del usuario: Q-06 (¿los argumentos del `.lnk` se editan en la aplicación?) bloquea E5; Q-07 (¿qué pasa con los accesos directos al eliminar una Cinta?) bloquea E4. | Analista de Requerimientos |
| 2026-10-22 | E5 | Resolución de Q-06 | Se adopta la **solo lectura**: la aplicación lee del `.lnk` destino, argumentos y *working directory* y los reproduce, sin editor de argumentos ni escritura de vuelta. Prevalece §4.5-03 sobre la «configuración» de RF04. E5 queda desbloqueada. | Usuario |
| 2026-10-22 | E4 | Resolución de Q-07 | Al eliminar una Cinta se eliminan también sus accesos directos, con **confirmación explícita** que indica el número de elementos afectados. La eliminación es atómica en la persistencia. E4 queda desbloqueada: no quedan preguntas abiertas. | Usuario |

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
- **Decisión**: **WPF sobre .NET 10**. Se adopta la Opción A de `docs/plan/decisiones/ADR-001-stack-y-arquitectura.md`, que queda en estado *Aceptada*. La versión de .NET se fija en Q-05.
- **Fecha**: 2026-10-22 · **Decidido por**: usuario
- **Bloquea**: E0 (desbloqueada)
- **Criterio de decisión, por peso**:
  1. **Compatibilidad y distribución** — ejecutable autocontenido, sin runtime de Windows App SDK en Windows 10 y sin fricción de empaquetado MSIX ni firma.
  2. **Interop Win32** — `HwndSource.AddHook`, `SetWinEventHook` y `SHAppBarMessage` tienen patrones maduros, lo que reduce el riesgo de RF01 y HU01.
  3. **RNF de reposo** — la huella de memoria base de WPF deja más margen bajo el límite de 50 MB.
- **Opción descartada**: WinUI 3 sobre .NET 10 (Windows App SDK). La comparación documentada queda en el ADR-001.
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

### Q-05 — Versión de .NET como TFM de destino

- **Estado**: **Resuelta**
- **Decisión**: **.NET 10** (`net10.0-windows`).
- **Fecha**: 2026-10-22 · **Decidido por**: usuario
- **Bloquea**: E0 (desbloqueada)
- **Contexto**: §5.1 pide «.NET 8 (o superior)». En el momento de la decisión:
  - El SDK instalado en el equipo es el **10.0.401**; no hay SDK ni runtime de .NET 8 instalados.
  - Estado de soporte oficial (política de .NET, actualizada el 2026-10-13): .NET 10 (LTS) → fin de soporte el **14/11/2028**, en soporte activo. .NET 9 (STS) y .NET 8 (LTS) → fin de soporte el **10/11/2026**, en mantenimiento.
- **Justificación**: .NET 10 es la única versión **LTS en soporte activo**, la única instalada, y cumple «.NET 8 o superior».
- **Opciones descartadas**:
  - **.NET 8** — lectura literal, pero exige instalar SDK y runtime, y deja de recibir parches el 10/11/2026, dentro de tres semanas, con una migración a .NET 10 casi inmediata.
  - **.NET 9** — está instalada, pero es de soporte estándar (STS) y caduca el mismo día que .NET 8.
- **Efecto en la implementación**:
  - TFM de todos los proyectos: `net10.0-windows`.
  - Lenguaje: **C# 14**, nullable reference types habilitado.
  - Los paquetes que se agreguen deben ser compatibles con `net10.0-windows`.

### Q-06 — ¿Los argumentos del acceso directo se configuran en la aplicación?

- **Estado**: **Resuelta**
- **Decisión**: **Solo lectura**. La aplicación **lee** del `.lnk` el destino, los argumentos y el *working directory*, y los reproduce fielmente al ejecutar. No edita los argumentos ni escribe de vuelta en el `.lnk`. La configuración de argumentos se hace en Windows, sobre el propio archivo `.lnk`.
- **Fecha**: 2026-10-22 · **Decidido por**: usuario
- **Bloquea**: E5 (desbloqueada)
- **Contradicción detectada**:
  - **RF04** (§2): «Permite la **configuración y ejecución** de accesos directos con argumentos de línea de comandos (ej. invocaciones vía `wsl.exe` o scripts personalizados)».
  - **§4.5-03**: «Los accesos directos mantienen de forma **estricta** las propiedades y parámetros de su archivo `.lnk` original (incluidos argumentos de línea de comandos como `wsl.exe` o flags de ejecutable)».
- **Justificación**: §4.5-03 y HU04-CA1 son criterios verificables y hablan de *mantener*; un editor de argumentos dentro de la aplicación los contradiría y añadiría funcionalidad no solicitada. La parte de RF04 relativa a la *configuración* dentro de la aplicación queda fuera de alcance.
- **Opciones descartadas**:
  - *Edición persistida aparte* — crea dos fuentes de verdad (`.lnk` y JSON propio) que pueden divergir, y rompe la «integridad estricta».
  - *Escritura de vuelta al `.lnk`* — modifica archivos del usuario fuera de `%AppData%` y depende de permisos de escritura en la carpeta de origen.
- **Efecto en la implementación**:
  - E5 **no** incluye editor de argumentos. El menú contextual de un acceso directo ofrece únicamente mover a Cinta, regresar a la barra principal y eliminar (§4.5-04).
  - La prueba de ejecución con argumentos (`.lnk` de `wsl.exe`) verifica que se respetan los del archivo, no que se puedan cambiar.
  - Si en el futuro se quiere editar argumentos, se registra como idea fuera de alcance y se consulta antes de implementarla.

### Q-07 — ¿Qué ocurre con los accesos directos al eliminar una Cinta?

- **Estado**: **Resuelta**
- **Decisión**: **Se eliminan junto con la Cinta**, previa **confirmación explícita** que indique cuántos accesos directos se van a eliminar. Cancelar la confirmación deja la Cinta y su contenido intactos.
- **Fecha**: 2026-10-22 · **Decidido por**: usuario
- **Bloquea**: E4 (desbloqueada)
- **Hueco detectado**: RF03 exige «**creación y gestión** de Cintas» y §4.5-04 define cómo eliminar *elementos* de una Cinta o de la barra, pero ningún documento dice qué sucede con los accesos directos que contiene una Cinta cuando se elimina la Cinta.
- **Justificación**: la eliminación de una Cinta es una acción explícita del usuario sobre el contenedor; conservar sus elementos en la barra convertiría «eliminar» en «disolver», con un resultado que el usuario no pidió. La confirmación con el conteo es la salvaguarda contra el borrado accidental.
- **Opciones descartadas**:
  - *Regresan a la barra principal* — no destruye nada, pero desborda la barra principal con elementos que el usuario no colocó allí y hace que «eliminar» signifique dos cosas distintas.
  - *No se puede eliminar una Cinta con elementos* — obliga a vaciarla elemento por elemento y añade fricción desproporcionada a una operación de gestión ordinaria.
- **Efecto en la implementación**:
  - El comando de eliminar Cinta abre un diálogo de confirmación con el número de accesos directos afectados; sin confirmación, no se elimina nada.
  - La eliminación de la Cinta y de sus elementos es **atómica** desde el punto de vista de la persistencia: o se guarda el estado nuevo completo, o se conserva el anterior.
  - La eliminación de un elemento individual sigue el camino de §4.5-04, sin diálogo de confirmación por Cinta.

---

## Interpretaciones resueltas por el propio documento (Fase B)

Registradas para que no se reabran como preguntas. En cada caso, la resolución se apoya en el texto de la especificación, no en una decisión nueva.

| ID | Ambigüedad | Resolución | Apoyo documental |
| --- | --- | --- | --- |
| C1 | RF04 pide «configuración y ejecución» de accesos directos con argumentos; §4.5-03 exige «integridad estricta» respecto del `.lnk` original | Prevalece **§4.5-03**: la aplicación **lee** los argumentos y los reproduce, sin editor. La «configuración» de RF04 no se ejerce dentro de la aplicación | Resuelta por el usuario en **Q-06** |
| C2 | EP02 dice «organización **jerárquica**»; §4.4-03 prohíbe el anidamiento | Estructura **plana**: un solo nivel de Cintas. «Jerárquica» se lee como «un nivel de agrupación», no como profundidad variable | §4.4-03 lo prohíbe de forma explícita y taxativa |
| C3 | §4.2-04 quiere «garantizar la visibilidad»; §4.2-02/03 atan el tono al tema | Prevalece la **regla literal del tono invertido**. §4.2-04 se declara **intención de diseño no normativa** | HU02-CA1 verifica literalmente la inversión de tono; §4.2-04 no es observable |
| A1 | «Cuadro indicador de la Cinta» sin definir | Indicador **clicable con etiqueta**; la Cinta se abre con **clic** | HU03-CA2 fija el clic; §4.4-01 fija que la animación parte del cuadro indicador |
| A2 | «Ventana/menú de configuración» desde la bandeja | **Clic izquierdo** abre la ventana de configuración; **clic derecho** abre el menú contextual | HU05-CA1 exige que el clic abra la ventana; §4.6-01 admite ambas vías |
| A3 | Umbral del acoplamiento magnético sin definir | **Parámetro de implementación**: franja de 48 px al soltar, documentada en E2 y ajustable en un punto | No es un requisito; es el parámetro que hace reproducible HU01-CA1 |
| A4 | Qué abarca el «área interactiva» | Unión de la barra desplegada **y sus Cintas abiertas**; el tránsito entre ambas lo absorbe el temporizador de gracia | §4.3-03 y el propósito declarado de la gracia (evitar cierres accidentales) |
| A5 | Métrica de RAM y de CPU | *Working set* con la barra colapsada en el CA temprano de E1; peor caso (desplegada, todas las Cintas) en E8. CPU medida durante 30 s | RNF-01 y RNF-02 no fijan métrica; la elección es conservadora |
| A6 | «Detección de entrada de puntero **global**» (§5.1-04) | Detección por el **área de la ventana**, sin hook global de ratón, para no comprometer RNF-02 | El comportamiento observable de RF02 se satisface igual; el arquitecto lo confirma en E3 |
| A7 | «Longitud mínima» de la barra sin valor | Igual a la **pestaña en reposo más la zona de indicadores**; con cero elementos y cero Cintas, exactamente la pestaña | RF03 exige un mínimo y un crecimiento dinámico, sin fijar valor |

## Ampliaciones de alcance registradas (Fase B)

Cambios del plan que **exceden** lo que la especificación enumera. Se registran para que la desviación sea visible y no pase por requisito.

| ID | Ampliación | Motivo | Estado |
| --- | --- | --- | --- |
| AMP-01 | Persistir el **borde acoplado** y el **temporizador de gracia** | §5.2-02 enumera solo orden, Cintas y preferencias multimonitor. Sin persistir el borde, la barra vuelve a su posición inicial en cada arranque (E2-CA5, E3-CA5) | Aceptada como preferencia de usuario |
| AMP-02 | **SQLite no se implementa**: solo JSON versionado | §5.2-02 ofrece «JSON / SQLite liviano» como alternativa. Se elige JSON por simplicidad y menor huella de memoria (RNF-01) | Aceptada (ADR-001 ya lo asume) |

---

## Ideas fuera de alcance

Registradas para una evaluación posterior. **No se implementan** hasta que se aprueben.

| ID | Idea | Origen | Estado |
| --- | --- | --- | --- |
| I-01 | Agrupación de cintas por perfil de trabajo | Propuesta propia | Pendiente |
| I-02 | Exportación e importación de la configuración en JSON | Propuesta propia | Pendiente |
| I-03 | Posicionamiento de la barra por arrastre con *snap* en las esquinas | §4.4 sugiere el comportamiento de apertura | Pendiente |
| I-04 | Configuración independiente por monitor (cintas y accesos directos distintos en cada pantalla) | RF05, segunda mitad de la frase; excluida por Q-04 | Pendiente |
| I-05 | Mica/Acrylic nativos y estilo Fluent del sistema (§5.1-02) | Deuda asumida por ADR-001 al elegir WPF: la translucidez se resuelve con opacidad por píxel y `DwmEnableBlurBehindWindow` | Pendiente |

---

## Resumen para reporte

- **Etapas completadas**: 0 de 9.
- **Etapa en curso**: ninguna.
- **Próxima acción**: no quedan decisiones pendientes. Ejecutar **E0** (cimientos de solución y build), que no depende de ninguna otra etapa.
- **Bloqueos que requieren tu decisión**: ninguno.
