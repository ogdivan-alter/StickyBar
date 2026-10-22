# Plan de Implementación — Sticky Dock Utility

Plan por etapas derivado de la especificación (`requerimiento/`). Este documento es la fuente de verdad del **alcance de cada etapa**; el estado y el seguimiento viven en `docs/plan/ESTADO.md`.

- **Propietario del documento**: Director de Desarrollo
- **Estado del plan**: borrador inicial (Fase B en curso)
- **Decisiones vigentes**: `docs/plan/decisiones/`

---

## Principios de descomposición

1. Cada etapa es **vertical**: deja algo funcionando y demostrable, no andamiaje.
2. Cada etapa es **verificable**: sus criterios de aceptación se derivan de los CA del backlog y son comprobables por el subagente `qa`.
3. Cada etapa es **trazable**: declara los IDs de RF / RNF / HU / CA que cubre.
4. El orden prioriza lo **bloqueante** (decisiones de stack) y lo **más incierto** (interop del docking, `.lnk`) antes que lo accesorio.
5. Una etapa abierta bloquea a las que dependen de ella. No se abren etapas en paralelo si comparten archivos.

---

## Mapa de trazabilidad

| Épica | Historias | Requerimientos | Etapas |
| --- | --- | --- | --- |
| EP01 Núcleo UI, Sticky y bordes | HU01, HU02 | RF01, RF02 | E1, E2, E3 |
| EP02 Cintas y contenedores | HU03 | RF03 | E4 |
| EP03 Gestión de accesos directos | HU04 | RF04 | E5 |
| EP04 Multimonitor y configuración | HU05 | RF05 | E6, E7 |
| Transversal | — | RNF (rendimiento, compatibilidad) | E0, E8 |

---

## E0 — Cimientos de solución y build

- **Objetivo**: disposición del proyecto compilable, con estructura de proyectos, MVVM base y persistencia inicial, que cierre la decisión de stack.
- **Trazabilidad**: RNF (compatibilidad Windows 10/11 x64, stack), §5.1, §5.2, y soporte estructural de RF01–RF05.
- **Dependencias**: — (primera etapa; ADR-001 ya resuelta).
- **Entregables**:
  - Solución .NET 8 con **WPF** (ADR-001 *Aceptada*) y los proyectos: aplicación, núcleo de dominio, interoperabilidad Win32 y pruebas.
  - Estructura de carpetas y convenciones documentadas en `AGENTS.md`.
  - Infraestructura MVVM mínima: `ViewModelBase`, contenedor de dependencias, comandos observables.
  - Servicio de persistencia JSON versionado en `%AppData%\StickyDock\` con escritura atómica y esquema inicial.
  - Logging con niveles y archivo rotado.
  - `dotnet build` y `dotnet test` en verde, perfil Release y Debug.
- **Criterios de aceptación**:
  - `dotnet build -c Release` finaliza sin errores ni advertencias.
  - `dotnet test` finaliza en verde con la suite inicial.
  - El esquema de persistencia se crea, se relee y sobrevive a un reinicio de la aplicación (prueba automatizada).
  - La escritura de la configuración es atómica: una escritura interrumpida no deja un archivo corrupto (prueba automatizada).
  - ADR-001 está en estado *Aceptada* (WPF sobre .NET 8) y la estructura de proyectosCreated la respeta.
- **Riesgo**: `AllowsTransparency` encarece el renderizado → el riesgo queda trazado en R-02 y su mitigación es la medición de CPU/RAM ya incluida en los CA de E1.
- **Esfuerzo**: 8 puntos.

---

## E1 — Ventana base, tema y pestaña en reposo

- **Objetivo**: una barra flotante sin marco, translúcida, con la pestaña semioculta de alto contraste y sensible al tema claro/oscuro de Windows.
- **Trazabilidad**: RF02 (§4.1, §4.2), §5.2 (proceso sin marco), §4.1.
- **Dependencias**: E0.
- **Entregables**:
  - Ventana principal *borderless*, `Topmost`, con opacidad y *blur* de fondo.
  - Modelo de estado de la barra: `Repositorio` (pestaña) / `Desplegada`.
  - Servicio de detección de tema de Windows (claro/oscuro) y conmutación en vivo.
  - Renderizado de la pestaña translúcida con tono invertido según el tema y micro-efecto de expansión.
  - Posicionamiento inicial dentro del área de trabajo.
- **Criterios de aceptación**:
  - Con el tema Oscuro, la pestaña se muestra translúcida de tono **claro**; con el tema Claro, translúcida de tono **oscuro** (§4.2, CA de HU02 #1).
  - El cambio de tema de Windows se refleja en la barra **sin reiniciar** la aplicación.
  - La barra permanece visible por encima de las ventanas de otras aplicaciones.
  - La barra en reposo ocupa únicamente la franja de la pestaña; el resto de su área no intercepta el clic.
  - El consumo en reposo, con la barra colapsada, no supera el 1 % de CPU (medición en reposo de 30 s, `qa`).
- **Riesgo**: coste de renderizado de la transparencia en reposo → medir pronto, en esta etapa.
- **Esfuerzo**: 8 puntos.

---

## E2 — Acoplamiento Sticky y detección de la barra de tareas

- **Objetivo**: la barra se adhiere magnéticamente a cualquiera de los tres bordes libres y se reposiciona sola cuando el área de trabajo cambia.
- **Trazabilidad**: RF01, HU01 (CA #1, CA #2), §5.1 (hooks), §4.4 (orientación de la barra).
- **Dependencias**: E1.
- **Entregables**:
  - Máquina de estados del borde acoplado: `Superior`, `Izquierdo`, `Derecho`, con exclusión del borde de la barra de tareas.
  - Detección de la posición de la barra de tareas y del *work area* por monitor.
  - Hooks de sistema (`SHAppBarMessage`, `WM_SETTINGCHANGE`, `SetWinEventHook`) encapsulados en el proyecto de interoperabilidad.
  - Arrastre libre de la barra con *snap* magnético al borde más cercano.
  - Persistencia del borde elegido.
- **Criterios de aceptación**:
  - Con la barra de tareas en el borde inferior, al arrastrar la barra hacia los bordes superior, izquierdo o derecho, se adhiere magnéticamente al borde elegido (HU01 CA #1).
  - Al cambiar la posición de la barra de tareas, la utilidad se reubica automáticamente para no colisionar (HU01 CA #2).
  - El borde donde reside la barra de tareas nunca es un destino válido del acoplamiento.
  - El hook de interoperabilidad libera sus recursos al cerrarse (verificación de handle y de suscripción al evento).
  - El borde acoplado se conserva tras reiniciar la aplicación.
- **Riesgo**: diferencias de *work area* entre Windows 10 y 11 y en sesiones con DPI mixto → cubrir con pruebas y observación manual.
- **Esfuerzo**: 13 puntos.

---

## E3 — Activación por hover, *slide-in* y temporizador de gracia

- **Objetivo**: la barra se despliega al pasar el cursor por la pestaña y se colapsa sola con un margen de gracia configurable.
- **Trazabilidad**: RF02, HU02 (CA #2, CA #3), §4.3.
- **Dependencias**: E2.
- **Entregables**:
  - Detección de entrada y salida del puntero sobre la pestaña y el área interactiva.
  - Temporizador de activación de 200 ms con micro-efecto de expansión previo.
  - Animación *slide-in* / *slide-out* hacia dentro del área de trabajo.
  - Temporizador de gracia configurable (por defecto 300–500 ms) antes de colapsar.
  - Preferencia del temporizador de gracia persistida y editable: 400 ms por defecto, rango configurable de 300–500 ms. El hover es **fijo** en 200 ms (Q-03).
- **Criterios de aceptación**:
  - Con el cursor sobre la pestaña durante ~200 ms, la barra emite un micro-efecto de expansión y se despliega con animación *slide-in* (HU02 CA #2).
  - Al salir el cursor del área interactiva, la barra colapsa tras el temporizador de gracia, con 400 ms por defecto dentro del rango 300–500 ms (HU02 CA #3, Q-03).
  - Moverse entre la barra desplegada y sus cintas no provoca un colapso accidental (grace period cálido).
  - El temporizador de activación se cancela si el cursor se retira antes de los 200 ms.
  - El tiempo de gracia es configurable por el usuario dentro del rango 300–500 ms, con 400 ms por defecto, y su valor persiste entre ejecuciones.
- **Riesgo**: timers sin cancelar en el camino de la UI → cubrir con pruebas del ViewModel y revisión de *fugas*.
- **Esfuerzo**: 8 puntos.

---

## E4 — Cintas y despliegue exclusivo

- **Objetivo**: agrupar accesos directos en Cintas que se despliegan perpendicularmente a la barra, con apertura exclusiva.
- **Trazabilidad**: RF03, HU03 (CA #1, CA #2), §4.4.
- **Dependencias**: E2.
- **Entregables**:
  - Modelo de dominio `Cinta` (nombre, orden, estado) sin nivel de anidamiento.
  - Creación, renombrado, reordenamiento y eliminación de Cintas.
  - Despliegue perpendicular al eje de la barra con animación *slide* hacia el interior de la pantalla.
  - Comportamiento de acordeón: abrir una cinta cierra la anterior.
  - Longitud dinámica de la barra: crece con el número de elementos y cintas, con longitud mínima.
- **Criterios de aceptación**:
  - Con la barra horizontal, la cinta se despliega verticalmente; con la barra vertical, horizontalmente (HU03 CA #1).
  - Con la cinta 'A' abierta, al activar la cinta 'B' la 'A' se cierra automáticamente (HU03 CA #2).
  - No es posible crear una cinta dentro de otra cinta (§4.4, estructura plana).
  - La barra crece dinámicamente al añadir elementos o cintas y conserva una longitud mínima.
  - La apertura de la cinta no collide con los límites del área de trabajo en los cuatro bordes.
- **Riesgo**: bajo. Es la etapa con menor incertidumbre técnica del plan.
- **Esfuerzo**: 8 puntos.

---

## E5 — Accesos directos `.lnk`, drag & drop e integridad de ejecución

- **Objetivo**: arrastrar accesos directos `.lnk` desde el Explorador hacia la barra o una cinta, conservando icono y argumentos, con menú contextual y reordenamiento.
- **Trazabilidad**: RF04, HU04 (CA #1, CA #2), §4.5, §5.1 (extracción nativa de iconos y lectura de `.lnk`).
- **Dependencias**: E1, E4.
- **Decisión de alcance (Q-01, resuelta)**: la entrada por drag & drop acepta **únicamente** archivos `.lnk`. Cualquier otro tipo se rechaza con un mensaje que indique que debe empaquetarse previamente como acceso directo (HU04 CA #2).
- **Entregables**:
  - Interoperabilidad con `IShellLink` / `IShellItem` para extraer icono, ruta de destino, argumentos y *working directory*.
  - Drag & drop de archivos hacia la barra y hacia las cintas.
  - Rechazo con mensaje explícito de los tipos no permitidos.
  - Menú contextual: mover a una cinta o a la barra principal, y eliminar.
  - Reordenamiento por drag & drop interno, con persistencia del orden.
  - Ejecución del acceso directo con sus argumentos originales.
- **Criterios de aceptación**:
  - Al arrastrar un `.lnk` sobre la barra o una cinta, se agrega el elemento respetando el icono y los parámetros de línea de comandos originales (HU04 CA #1).
  - Un `.lnk` que apunta a `wsl.exe` con argumentos conserva esos argumentos al ejecutarse, y el proceso invocado es el esperado (verificable con un `.lnk` de prueba que registre su línea de comandos).
  - Al arrastrar un `.exe` o una carpeta sin empaquetar, la acción se rechaza indicando que debe empaquetarse previamente como acceso directo (HU04 CA #2).
  - El clic derecho sobre un acceso directo ofrece mover a cinta o a la barra principal, y eliminar (§4.5).
  - El reordenamiento por drag & drop interno persiste tras reiniciar la aplicación.
  - Los elementos añadidos conservan el orden de la barra y de cada cinta tras reiniciar.
- **Riesgo**: COM de `IShellLink` con *marshaling* y *bitmaps* de icono → es la etapa más técnica del plan.
- **Esfuerzo**: 13 puntos.

---

## E6 — Bandeja del sistema, ventana de configuración y auto-inicio

- **Objetivo**: gestionar la aplicación desde el área de notificación, con preferencias persistidas y arranque automático opcional.
- **Trazabilidad**: §4.6, HU05 (CA #1), RF02 (temporizadores configurables).
- **Dependencias**: E1.
- **Entregables**:
  - Icono permanente en la bandeja del sistema con menú contextual.
  - Ventana de configuración general: temporizador de gracia (rango 300–500 ms, 400 ms por defecto), comportamiento de la pestaña, auto-inicio.
  - Persistencia de preferencias con valor por defecto y validación de rangos.
  - Registro en `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` para el arranque automático.
- **Criterios de aceptación**:
  - Al hacer clic en el icono de la bandeja se abre la ventana de configuración general (HU05 CA #1).
  - El menú de la bandeja ofrece abrir configuración y salir, y salir termina el proceso.
  - Los cambios de preferencias surten efecto de inmediato y persisten entre ejecuciones.
  - Con el auto-inicio habilitado, la aplicación se ejecuta al iniciar sesión de Windows; deshabilitado, no.
  - La aplicación puede ejecutarse en segundo plano sin ventana visible salvo la barra.
- **Riesgo**: bajo.
- **Esfuerzo**: 8 puntos.

---

## E7 — Multimonitor y réplica espejada

- **Objetivo**: replicar de forma espejada el contenido de la barra en los monitores secundarios seleccionados.
- **Trazabilidad**: RF05 (parcial, ver Q-04), HU05 (CA #2), §4.6 (réplica).
- **Dependencias**: E2, E5, E6.
- **Decisión de alcance (Q-04, resuelta)**: se implementa **solo el modo réplica**. La «configuración individual independiente» de RF05 queda fuera de alcance y se registra como desviación (I-04 en `docs/plan/ESTADO.md`).
- **Entregables**:
  - Detección de monitores y de sus áreas de trabajo.
  - Opción por monitor para activar su presencia y, con ella, la réplica.
  - Barra por monitor con docking y *work area* propios.
  - Sincronización de altas, bajas y reordenamientos entre las réplicas.
- **Criterios de aceptación**:
  - Con la réplica habilitada, al añadir o reordenar un acceso directo los cambios se reflejan de forma espejada en la barra del monitor secundario (HU05 CA #2).
  - El contenido replicado es idéntico en todos los monitores: mismas cintas, mismos accesos directos y mismo orden.
  - Cada barra se acopla al borde libre de **su** monitor y no invade la barra de tareas de ese monitor.
  - Al desconectar un monitor secundario, la barra asociada se cierra sin afectar a la principal.
  - Al volver a conectar ese monitor, la réplica se restaura con el contenido vigente.
- **Riesgo**: sincronización de estado entre procesos de UI por monitor → definir un único origen de verdad en el núcleo.
- **Esfuerzo**: 8 puntos.

---

## E8 — Rendimiento, endurecimiento y entrega

- **Objetivo**: cumplir los RNF medidos, endurecer los caminos de error y dejar el producto instalable y documentado.
- **Trazabilidad**: RNF (RAM < 50 MB, CPU < 1 % en reposo), §4 (transparencias y estilos nativos), §5.2.
- **Dependencias**: E0–E7.
- **Entregables**:
  - Medición de RAM en operación normal y de CPU en reposo, con informe.
  - Perfilado del renderizado y de los temporizadores, y eliminación del trabajo periódico innecesario.
  - Manejo de errores: persistencia corrupta, permisos insuficientes, fallo al lanzar el acceso directo.
  - Suite de pruebas con cobertura de los criterios de aceptación automatizables.
  - Empaquetado instalable y desinstalable, con la opción de auto-inicio.
  - Documentación de uso y de arquitectura.
- **Criterios de aceptación**:
  - RAM en operación normal por debajo de 50 MB, medido con la barra desplegada y con todas las cintas abiertas.
  - CPU en reposo por debajo del 1 %, medido durante al menos 30 s con la barra colapsada.
  - Una configuración corrupta no impide el arranque: se recupera con valores por defecto y se registra el aviso.
  - Un acceso directo cuyo destino no existe produce un aviso al usuario, sin cierre de la aplicación.
  - El instalador añade y quita la entrada de auto-inicio de forma coherente.
  - La suite de pruebas cubre cada criterio de aceptación automatizable de E0–E7.
- **Riesgo**: el RNF de RAM depende del framework elegido en ADR-001.
- **Esfuerzo**: 13 puntos.

---

## Resumen del plan

| Etapa | Nombre | Depende de | RF / HU | Esfuerzo | Riesgo |
| --- | --- | --- | --- | --- | --- |
| E0 | Cimientos de solución y build | — | Transversal | 8 | Medio |
| E1 | Ventana base, tema y pestaña | E0 | RF02 | 8 | Medio |
| E2 | Sticky docking y barra de tareas | E1 | RF01, HU01 | 13 | Medio-alto |
| E3 | Hover, slide-in y gracia | E2 | RF02, HU02 | 8 | Bajo |
| E4 | Cintas y despliegue exclusivo | E2 | RF03, HU03 | 8 | Bajo |
| E5 | Accesos directos `.lnk` | E1, E4 | RF04, HU04 | 13 | Alto |
| E6 | Bandeja, configuración y auto-inicio | E1 | §4.6, HU05 | 8 | Bajo |
| E7 | Multimonitor y réplica | E2, E5, E6 | RF05, HU05 | 8 | Medio |
| E8 | Rendimiento y entrega | E0–E7 | RNF | 13 | Medio |

**Total estimado**: 87 puntos.

> Las estimaciones son relativas y de una iteración. El Director de Desarrollo debe recalibrarlas tras las primeras dos etapas ejecutadas.
