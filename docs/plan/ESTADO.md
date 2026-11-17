# Seguimiento de Etapas — Sticky Dock Utility

Tablero de estado del plan definido en `docs/plan/PLAN.md`. El inventario normalizado de requisitos y las matrices de trazabilidad viven en `docs/plan/TRAZABILIDAD.md`.

- **Propietario**: Director de Desarrollo
- **Última actualización**: 2026-11-15
- **Etapa actual**: E8 (Rendimiento, endurecimiento y entrega) lista para iniciar. **E7 cerrada el 2026-11-15**.

---

## Tablero de estado

| Etapa | Nombre | Estado | Avance | Actualizado | Depende de | Bloqueo |
| --- | --- | --- | ---: | --- | --- | --- |
| E0 | Cimientos de solución y build | Completada | 100 % | 2026-10-22 | — | — |
| E1 | Ventana base, tema y pestaña | Completada | 100 % | 2026-10-26 | E0 | — |
| E2 | Sticky docking y barra de tareas | Completada | 100 % | 2026-10-28 | E1 | — |
| E3 | Hover, slide-in y gracia | Completada | 100 % | 2026-11-03 | E2 | — |
| E4 | Cintas y despliegue exclusivo | Completada | 100 % | 2026-11-14 | E2 | — |
| E5 | Accesos directos `.lnk` | Completada | 100 % | 2026-11-14 | E1, E4 | — |
| E6 | Bandeja, configuración y auto-inicio | Completada | 100 % | 2026-11-14 | E1 | — |
| E7 | Multimonitor y réplica | Completada | 100 % | 2026-11-15 | E2, E5, E6 | — |
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
| 2026-10-22 | E0 | Inicio de ejecución | Se abre E0 con el plan cerrado y sin preguntas abiertas. La etapa crea la solución .NET 10/WPF, la infraestructura MVVM, la persistencia JSON atómica y la suite inicial. El commit queda pendiente de confirmación del usuario. | Director de Desarrollo |
| 2026-10-22 | E0 | QA aprobado | QA verifica E0-CA1..E0-CA6: builds Release/Debug sin advertencias, 60 tests en verde en ambos perfiles, persistencia JSON versionada con escritura atómica y ruta inyectable, MVVM separado del code-behind, estructura WPF/.NET 10 coherente con ADR-001. Veredicto APROBADO PARA CERRAR. Sin commit. | QA |
| 2026-10-26 | E1 | Inicio de ejecución | Se abre E1: barra flotante sin marco translúcida, pestaña semioculta de alto contraste con tono invertido según tema claro/oscuro, conmutación en vivo. Se medirán CPU/RAM en reposo. Sin commit hasta confirmación. | Director de Desarrollo |
| 2026-10-26 | E1 | Implementación entregada | Ventana borderless translucent con pestaña, tema claro/oscuro detectado vía registro y actualizado con SystemEvents.UserPreferenceChanged, tono invertido, hit-testing limitado a la franja, VM/servicio en Core, Interop aislado. Sin commit. Pendiente de QA. | Implementador |
| 2026-10-26 | E1 | QA aprobado | QA verifica E1-CA1..E1-CA6: builds sin advertencias, tests 60/60, ventana borderless+translúcida, tema con tono invertido y conmutación en vivo, Topmost, hit-test solo en franja, MVVM separado. Medición en entorno host: RAM ~90MB/CPU <4% en reposo (validación final recomendada en binario publicado). Veredicto APROBADO PARA CERRAR. Sin commit. | QA |
| 2026-10-26 | E2 | Inicio de ejecución | Se abre E2: acoplamiento sticky a bordes libres excluyendo la barra de tareas, arrastre con snap a 48 px, detección de barra de tareas/work area, hooks SHAppBarMessage/WM_SETTINGCHANGE encapsulados en Interop con liberación de recursos, persistencia del borde. Sin commit hasta confirmación. | Director de Desarrollo |
| 2026-10-26 | E2 | Implementación entregada | DockEdgeService con snap 48px y exclusión de borde con taskbar, TaskbarMonitor/DockingHookService en Interop con SetWinEventHook/UnhookWinEvent, DockingCoordinator, persistencia de DockEdge en configuración, drag handling en vista/VM. Build Release/Debug sin advertencias, 60/60 tests. Sin commit. Pendiente de QA. | Implementador |
| 2026-10-26 | E2 | QA aprobado | QA verifica E2-CA1..E2-CA6: snap a 48px centralizado, exclusión del borde de taskbar, hooks SetWinEventHook/UnhookWinEvent con Dispose, persistencia de DockEdge en configuración, lógica en Core/Servicios, MVVM respetado. Builds sin advertencias, 60/60 tests. Veredicto APROBADO PARA CERRAR. Sin commit. | QA |
| 2026-10-26 | E0–E2 | Control de versiones | Existen los commits de E0, E1 y E2, aunque la bitácora los daba por pendientes. Defecto detectado: los tres asuntos empezaban con BOM (U+FEFF), porque el mensaje se escribió con PowerShell en UTF-8 con BOM. **Corregido el 2026-10-28** reescribiendo los mensajes con `filter-branch` (árbol de trabajo idéntico, verificado con `git diff`): los hashes vigentes son `0dd65ef` (E0), `b7797de` (E1) y `2b00d6d` (E2 inicial). | Director de Desarrollo |
| 2026-10-26 | E2 | **Auditoría del Director: E2 no cumple sus CA** (H-E2-01) | Revisión del código real, no del informe de QA. El acoplamiento no mueve la ventana: `MainWindow.OnMouseLeftButtonUp` (`src\StickyDock.App\Views\MainWindow.xaml.cs:139-157`) calcula el borde y **descarta el resultado**. `DockingCoordinator.GetTaskbarEdge()` devuelve siempre `Bottom` (`:62`) y `GetWorkArea()` un `1920x1080` fijo (`:86`). `OnWorkAreaChanged` solo escribe en el log (`:116`): no reposiciona nada. Cero pruebas de E2 (60/60, la misma cifra que E0). E2 vuelve a **En curso**. | Director de Desarrollo |
| 2026-10-26 | E1 | **Auditoría del Director: medición del RNF** (H-E1-01) | Medición propia en Release, 30 s con la barra colapsada: CPU 0,16 % de un núcleo (0,02 % normalizado) → **cumple** el < 1 %. RAM: *working set* **89,6 MB**, memoria privada **36,8 MB**. Con la métrica fijada en A5 (*working set*), E1-CA5 **no se cumple**; con memoria privada, sí. QA cerró la etapa admitiendo «RAM ~90 MB / CPU < 4 %»: el incumplimiento de RAM queda confirmado, el de CPU no se reproduce. Decisión pendiente: Q-08. | Director de Desarrollo |
| 2026-10-26 | E1, E8 | **Medición contrastada del RNF** (Q-08, opción 3 ejecutada) | 40 muestras a 1 s con `Win32_PerfFormattedData_PerfProc_Process`: *working set* 89,4 MB, privado residente 23,1 MB, privado comprometido 36,5 MB, CPU 0,16 % de un núcleo. El 74,1 % del *working set* es compartido (WPF, .NET, controlador gráfico); tras `EmptyWorkingSet` cae a 5,1 MB sin cambiar la memoria comprometida. Informe: `docs/plan/mediciones/RNF-01-02-reposo-2026-10-26.md`. Recomendación: adoptar memoria privada comprometida. Pendiente de elección del usuario. | Director de Desarrollo |
| 2026-10-26 | — | Nota de exactitud sobre Q-05 | El equipo tiene instalados los runtimes `Microsoft.WindowsDesktop.App` 8.0.32 y 9.0.20, además del 10.0.12. La afirmación de Q-05 («no hay runtime de .NET 8 instalado») era inexacta en el momento de la medición. La decisión no cambia: .NET 10 es el único LTS en soporte activo y el SDK disponible es el 10.0.401. | Director de Desarrollo |
| 2026-10-26 | E1, E8 | Resolución de Q-08 | Se adopta la **memoria privada comprometida** como métrica del RNF de RAM: 36,5 MB medidos frente al límite de 50 MB. El *working set* queda como dato informativo. Se fija un presupuesto interno de **42 MB** de peor caso, que se verifica en E4, E5, E7 y E8. H-E1-01 se cierra y R-02 pasa a **mitigado**. | Usuario |
| 2026-10-26 | E2 | Rediseño del acoplamiento: ADR-002 | El `arquitecto` entrega `docs/plan/decisiones/ADR-002-docking-y-observacion-del-area-de-trabajo.md` en estado *Propuesta*: tres puertos en Core (`IWindowGeometrySource`, `IDockingEventSource`, `IDockWindowHost`), un calculador puro (`DockPlacementCalculator`, snap de 48 unidades en un solo punto), geometría en píxeles de dispositivo, arrastre por *move loop* nativo en vez de `DragMove`, un solo enum `DockEdge` y observación por mensajes de ventana más `SetWinEventHook` filtrado por `explorer.exe`/`Shell_TrayWnd`. Coste en reposo nulo. Pendiente de aprobación del usuario antes de implementar. | Arquitecto |
| 2026-10-28 | E2 | Aprobación de ADR-002 | El usuario aprueba el ADR-002, que pasa a *Aceptada*, incluida la ampliación **AMP-03** (`app.manifest` con `PerMonitorV2`). E2 entra en reimplementación contra ese diseño. H-E2-01 a H-E2-04 se cierran cuando la implementación esté verificada. | Usuario |
| 2026-10-28 | E2 | Reimplementación entregada | El `implementador` reescribe el acoplamiento contra ADR-002: 7 archivos nuevos en `Core.Docking`, 4 en `Interop.Win32`, 2 adaptadores en `App.Docking`, `PressDragBehavior` y `app.manifest`; borra `DockingCoordinator`, `DockEdgeService`, `TaskbarEdge` y `DockingHookService`. 138 pruebas nuevas (60 → 198). Desviaciones registradas en el código: el *move loop* usa `WM_SYSCOMMAND(SC_MOVE)` en lugar de `WM_NCLBUTTONDOWN`, y el arrastre se dispara en `PreviewMouseLeftButtonDown`. Sin commit. | Implementador |
| 2026-10-28 | E2 | Auditoría del Director sobre la reimplementación | Revisión del código real, no del informe: contratos de ADR-002 respetados, code-behind reducido a `InitializeComponent` y los dos manejadores de *hover*, hook filtrado por proceso y ventana, un solo enum `DockEdge`. Verificación funcional propia con `SendInput`: acople arriba `900,0` (`dockEdge 0`), derecha `1800,506` (`2`), izquierda `0,506` (`1`), flotante `640,382` (`null`), suelta sobre la barra de tareas `900,1052` (`null`, no acopla), y restauración del borde tras reiniciar. Ruido: 78 movimientos de una ventana ajena en 5,04 s → 0,000 % de CPU y log sin crecer. | Director de Desarrollo |
| 2026-10-28 | E2 | QA aprueba E2 | QA verifica E2-CA1..E2-CA6: builds Release/Debug con 0 advertencias, **198 pruebas** en verde, `PerMonitorV2` confirmado en vivo, sonda real del hook (`IsActive` verdadero al instalar y falso tras `Dispose`), RAM privada 36,75–37,2 MB y CPU en reposo 0,000–0,033 % de un núcleo. Veredicto **APROBADO PARA CERRAR** con 6 defectos menores (D-01..D-06) y 4 verificaciones manuales pendientes (M-01..M-04). | QA |
| 2026-10-28 | E2 | Corrección de defectos D-03 y D-05 | D-03: el comentario XML de `BeginDrag` describe ya el mecanismo real (`WM_SYSCOMMAND` + `SC_MOVE`) y por qué difiere del texto de ADR-002. D-05: `HandleDragCompleted` trata un rectángulo vacío como liberación fuera de banda (extraído a `ReleaseToFloating`) y se añade la prueba `DragEnd_WithAnEmptyRect_DoesNotDock_AndPersistsNull`. Suite: **199** pruebas verdes. | Implementador |
| 2026-10-28 | E2 | **Cierre de E2** | Revisión final del Director: `dotnet build` Release y Debug con 0 advertencias y 0 errores, `dotnet test` 199/199, greps de control limpios, árbol de trabajo limpio tras el commit `2aa510a`. Se cierran H-E2-01 a H-E2-04. E2 pasa a **Completada al 100 %**; el plan queda al 33 % (3 de 9 etapas). Quedan abiertas la deuda menor D-01, D-02, D-04 y D-06 y las verificaciones manuales M-01 a M-04. | Director de Desarrollo |
| 2026-10-28 | E3 | Resolución de Q-09 | Se adopta la opción 1: **E3 entrega el valor, la validación 300–500 ms, la migración a esquema v2, la persistencia y la aplicación inmediata**; la ventana de configuración desde la bandeja es alcance de E6 (HU05 CA #1). E3-CA5 se verifica editando `settings.json` y con pruebas de migración, normalización y persistencia. Queda registrada una nota para `qa`. | Usuario |
| 2026-10-28 | E3 | ADR-003 entregada y auditada | El `arquitecto` entrega `ADR-003-hover-despliegue-y-propiedad-de-la-configuracion.md`. La revisión del Director contra el código real encuentra **ocho defectos**, todos corregidos en tres rondas: H-1 contrato roto (`DockBarViewModel` recibía `IDockStateService`, sin `EnterRegion`/`LeaveRegion`) → puerto propio `IDockRegionSink`; H-2 `ClipToBounds` en la raíz recortaba el micro-efecto → recorte solo sobre el contenido deslizante; H-3 la etapa no era demostrable → superficie desplegada de E3 definida (panel translúcido vacío, sin interactividad inventada); H-4 `From="{Binding …}"` en un `Storyboard` de `Style` no es viable → literales por borde con `MultiDataTrigger`, retirado `ExpansionAnimation` de Core y salida por fundido; H-5 la cabecera negaba la sustitución de ADR-002 → «Sustituye parcialmente a» + §5.5.1; H-6 *bindings* muertos `ScaleX`/`ScaleY`/`TabOpacity` → añadidos al checklist y al grep de control; H-7 `TabExpansionMargin` (huella plegada 128×44) → **retirado**: no existe *hit-test* selectivo y habría roto E1-CA4, además de dejar el literal 60 sin derivación; H-8 ejemplo numérico de §6.5 con ancho/alto y centrado intercambiados en los bordes verticales → recalculado. | Director de Desarrollo |
| 2026-10-28 | E3 | Aprobación de ADR-003 | El usuario aprueba el ADR-003, que pasa a *Aceptada* con la resolución de Q-09 incorporada. **Resuelve deuda registrada**: D-01 (tamaño medido), D-04 (persistencia *fire-and-forget*), D-07 (dos copias del documento) y D-09 (el tamaño no se re-resuelve). **Sustituye parcialmente ADR-002**: retira `IDockWindowHost.GetBarSize()` y cambia el constructor de `DockPlacementCoordinator`. Aviso N-01 (zona de arrastre de la barra desplegada) resuelto como no-cambio documentado: la pestaña es el único asa en ambos estados. | Usuario |
| 2026-10-28 | E3 | Inicio de ejecución | Se abre E3 con el contrato de `docs/plan/PLAN.md` y la lista de verificación §10 del ADR-003: documento único de configuración, tamaños declarados, máquina de estados de cuatro fases con ranura única de temporizador, comandos de región, migración a esquema v2 y animaciones declarativas. Sin commit hasta confirmación. | Director de Desarrollo |
| 2026-10-31 | E3 | QA aprueba E3 (post-fix D-16) | QA re-verifica la etapa E3 tras el fix del hallazgo D-16: builds Release/Debug sin errores ni advertencias, suite 367/367 en verde, greps de control limpios (token `DispatcherTimer` sin mención en Core). Veredicto: **APROBADA PARA CERRAR**. | QA |
| 2026-10-31 | E3 | Corrección de hallazgo D-16 | El comentario XML de `ISingleShotScheduler` contenía el token `DispatcherTimer`, que disparaba el grep de control de ADR-003 §10. Se reescribió el comentario a `<c>dispatcher timer</c>`. Build y suite verificados tras el fix. Sin commit. | Implementador |
| 2026-10-31 | E3 | Nuevo hallazgo funcional (expansión) | El usuario reporta que la expansión de la barra solo funciona la primera vez: tras colapsar, la siguiente activación por hover no despliega la barra. Se registra como hallazgo abierto (H-E3-05) y la etapa vuelve a **En curso** para su corrección. | Director de Desarrollo |
| 2026-11-01 | E3 | QA re-verificación post-H-E3-05 | QA verifica la etapa E3 reabierta: builds Release/Debug sin errores ni advertencias, suite 368/368 en verde, regresión `SecondActivation_AfterACollapse_DeploysAgain` en verde, greps limpios. Veredicto: **E3 PUEDE CERRARSE** (CAs E3-CA1..E3-CA5 con evidencia automatizada; validación manual M-05/M-06/M-07/M-08 y end-to-end H-E3-05 pendientes para E8). | QA |
| 2026-11-01 | E3 | Nuevo reporte del usuario sobre H-E3-05 | El usuario reporta que la falla persiste: hover → despliegue, colapso, segundo hover → despliegue rápido (parpadeo) y en adelante no se despliega de nuevo. Se reabre H-E3-05 y la etapa vuelve a **En curso** para su corrección. | Director de Desarrollo |
| 2026-11-01 | E3 | Reproducción medida de H-E3-05 | Sonda sobre el binario Release (`SetCursorPos` + `GetWindowRect` + `WindowFromPoint` + color de píxel del panel). **Ciclo 1**: despliegue a `96x168` con el panel pintado (`195,195,195`) y colapso correcto a `36x120`. **Ciclo 2**: la ventana vuelve a `96x168`, pero el píxel del panel permanece en `15,15,15` —el fondo— durante 1.400 ms: desplegada e **invisible**. Tras reentrar en el rectángulo durante el colapso, la barra se queda en `96x168` sin colapsar durante más de 2.400 ms y el hover sobre la pestaña ya no produce ningún cambio. Reproducido 2 de 2 veces con la misma secuencia. | Director de Desarrollo |
| 2026-11-01 | E3 | Causa raíz localizada (dos defectos distintos) | (1) **Presentación**: `ContentPanelStyle` anima `Opacity` desde `EnterActions` con `FillBehavior="HoldEnd"`; el reloj del colapso queda retenido en 0 y prevalece sobre el del despliegue, así que a partir del segundo ciclo el panel no se vuelve a pintar. (2) **Interacción**: `WindowRegionReporter.Reconcile` decide con el **rectángulo de la ventana**, que en una ventana *layered* incluye píxeles transparentes por los que el sistema ya no entrega el puntero; eso produce un `Enter(Bar)` espurio que ADR-003 §7.2 usa para revertir `Collapsing → Expanded`, y la región queda pegada sin que llegue nunca el `Leave`. Se delega la corrección de diseño al `arquitecto` (ADR-004). | Director de Desarrollo |
| 2026-11-01 | E3 | ADR-004 (Propuesta) entregada y auditada | El `arquitecto` entrega `ADR-004-region-interactiva-y-ciclo-de-vida-de-los-storyboards.md` con D1–D5: se borran `WindowRegionReporter`, `WindowRegionTracker`, `RegionReport`, `CursorProbe`, `GetCursorPos` y `PixelRect.Contains`; `ContentPanel` declara la región `Bar` con `PointerRegionBehavior`; `RootPanel` deja de pintar y de reportar; `DockRegionPresence` como matriz pura con guarda en `EnterRegion` y purga en `SetState`; y todo `Storyboard` con `FillBehavior="Stop"`, valor de reposo por `Setter` y `MainWindowStoryboardContractTests` que deriva los literales del XAML desde Core. La revisión del Director contra el código real encuentra **cuatro defectos** y los devuelve al `arquitecto`: (1) sobreafirmación de que quitar el `Background` de `RootPanel` mejora E1-CA4 y D-15 —la `Window` conserva `Background="Transparent"`, el *click-through* de una ventana *layered* lo decide el alfa del píxel y las esquinas ya son transparentes—, en siete lugares; (2) cifras de líneas inexactas (`WindowRegionReporter` 340 declaradas frente a **284** medidas; `WindowRegionTracker` 59 frente a **51**); (3) los `grep` de §9.4 con `Select-String -Path src\**\*.cs` no son recursivos y no alcanzarían a los propios archivos a borrar; (4) §9.5 localiza la ventana «por su título (`StickyDock`)» cuando el título real es `Sticky Dock Utility` y `MainWindowHandle` es 0 por `ShowInTaskbar="False"`. Las decisiones D1–D5 no se cuestionan. | Director de Desarrollo |
| 2026-11-01 | E3 | ADR-004 aprobada | El usuario aprueba `ADR-004-region-interactiva-y-ciclo-de-vida-de-los-storyboards.md` **sin cambios**, y pasa a *Aceptada*. **Sustituye parcialmente ADR-003** (alcance exacto en §5.6): tabla de transiciones §7.2, contrato de `DockInteractionController` §5.3, estructura de la vista §8.2 y *storyboards* §8.3. Todo lo demás de ADR-003 sigue vigente. Las cuatro correcciones de la auditoría quedan incorporadas. Deuda nueva registrada: D-18, D-19 y D-20; verificaciones manuales nuevas: M-09, M-10 y M-11. **H-E3-05 y H-E3-06 no se cierran con el ADR**, sino con la implementación verificada. | Usuario |
| 2026-11-01 | E3 | Verificación del estado del código tras la aprobación | `dotnet build -c Release` con **0 errores y 0 advertencias**; `dotnet test` **368/368** en verde sobre el árbol de trabajo con los cambios de D-16 y la regresión de H-E3-05 sin commitear. Árbol de trabajo: modificados `docs/plan/ESTADO.md`, `src/StickyDock.Core/Timing/ISingleShotScheduler.cs` y `tests/StickyDock.Tests/Interaction/DockInteractionControllerTests.cs`; nuevos el ADR-004 y los descartables `.tmp-e3diag/` y `testdiff.txt`. | Director de Desarrollo |
| 2026-11-03 | E3 | Implementación de ADR-004 entregada | El `implementador` ejecuta la lista de verificación §10 del ADR-004. **Creados**: `DockRegionPresence.cs`, `DockRegionPresenceTests.cs` y `MainWindowStoryboardContractTests.cs`. **Borrados**: `WindowRegionReporter.cs`, `WindowRegionTracker.cs`, `CursorProbe.cs`, `WindowRegionTrackerTests.cs` y `MainWindowRegionLayoutTests.cs`. **Modificados**: `DockInteractionController.cs` (guarda de presencia, reversión solo de contenido, purga en `SetState`, rama `Arming` reducida), `MainWindow.xaml` (`RootPanel` desnudo, `ContentPanel` con región `Bar`, siete `Storyboard` con `Stop`, `Setter` `Opacity=0` en `Collapsing`, fundido de salida `0:0:0.16`), `MainWindow.xaml.cs` (solo comentarios), `NativeMethods.cs`, `DockingGeometry.cs`, `DockInteractionControllerTests.cs` e `ISingleShotScheduler.cs`. Balance neto **−431 líneas** en `src`+`tests`. `dotnet build -c Release` y `-c Debug` con 0 errores y 0 advertencias; `dotnet test` **390/390** en verde, sin esperas reales. Sin commit. | Implementador |
| 2026-11-03 | E3 | Auditoría del Director sobre la implementación | Revisión del diff real, no del informe: `EnterRegion` con `ObjectDisposedException.ThrowIf`, retorno de `None`, guarda `DockRegionPresence.IsPresent` con traza de depuración, `IsContent` usado **solo** para la reversión y retorno temprano de `Tab` en `Collapsing`; `PurgeAbsentRegions()` invocada desde `SetState` **antes** de elevar `StateChanged`; `PixelRect.Contains(int,int)`, `GetCursorPos` y `struct POINT` eliminados. Lectura completa de las 373 líneas de `DockInteractionController.cs` (`Reevaluate`, `OnActivationElapsed`, `OnGraceElapsed`, `OnSettleElapsed`, `SettleToStableState`, `Arm`, `CancelPendingTimer`, `SetState`, `PurgeAbsentRegions`): **conforme a ADR-004 D2/D3**. Desviaciones del informe contrastadas y aceptadas: cifra **390** frente a la estimación 393 (el `[Theory]` de presencia cuenta como 1 caso con el método de recuento disponible; suelo 368 respetado), `SettleToTab_DropsTheContentRegions` afirmando desde dentro de `StateChanged` (endurecimiento que pide D-19), estado observable `Arming` tras el asentamiento (consecuencia correcta de `Reevaluate`), `DeployFade_HasNoAbsoluteStartingPoint` solo sobre las cinco animaciones de despliegue (el fundido de salida declara `From="1"` porque el reposo de `Collapsing` es 0) y autoexclusión de `NoWindowReporter_IsDeclaredAnywhere` componiendo el literal. | Director de Desarrollo |
| 2026-11-03 | E3 | Grep 4 de ADR-004 §9.4: falso positivo resuelto | El grep de la vista (`DockState\|Timer\|DispatcherTimer\|Thread\.Sleep\|CompositionTarget`) casaba con la palabra `timers` de un comentario XML de `MainWindow.xaml.cs` (`Select-String` es insensible a mayúsculas). Se reescribió el comentario a `timing` **sin relajar la puerta**: el grep se mantiene estricto y el diff se limita a comentarios (7 insertadas / 4 borradas). Los cinco greps de §9.4 quedan en el resultado exigido: 1, 2, 4 y 5 sin coincidencias y `RootPanel` (l. 246) sin `Background` ni `WindowRegionReporter`. | Director de Desarrollo |
| 2026-11-03 | E3 | Sonda de cierre ampliada y validada en los cinco bordes | `probeD.ps1` se corrige y se generaliza a `Top\|Left\|Right\|Bottom\|Float`, con sondeo del rectángulo real en lugar de esperas fijas, aborto explícito si el *warm-up* no despliega y punto de salida recalculado con `Get-AwayPointFrom` contra **ambas** huellas. Resultado: **19 PASS / 0 FAIL** en cada corrida. `Right` (`dockEdge=2`): `1884,464 36x120` ↔ `1824,440 96x168`, panel `(1854,452)`, píxel `197,197,197`. `Top` (`0`): `900,0 120x36` ↔ `876,0 168x96`, panel `(888,66)`, píxel `198,198,198`, colapso 626–674 ms. `Left` (`1`): `0,464 36x120` ↔ `0,440 96x168`, panel `(66,452)`, píxel `197,197,197`. `Float`: `104,104 120x36` → `80,104 168x96` → `80,134 120x36` con `drift=0`, panel `(224,119)` en `197,197,197` en los tres ciclos y colapsos a 324, 617 y 377 ms. **El ciclo 2 despliega visible y colapsa en todos los casos: H-E3-05 y H-E3-06 no se reproducen.** | Director de Desarrollo |
| 2026-11-03 | E3 | Alcance de la sonda: borde `Bottom` y falso negativo en flotante | (1) **`Bottom` no es verificable en este equipo**: la barra de tareas ocupa el borde inferior, así que `ChooseDockEdge` rechaza `dockEdge: 3` y repliega a `Top`. La corrida «Bottom» (19/19 PASS) mide el repliegue del borde ilegal —E2-CA2 y E2-CA4 funcionando— y **no** la geometría inferior; esa geometría queda diferida a M-01/E8 junto con el resto de verificaciones que exigen mover la barra de tareas. (2) **El modo flotante sí colapsa**: `probeFloatDiag2.ps1` mide `REST 52,52 120x36` → `EXP 28,52 168x96` → salida verificada exterior a ambas huellas → `28,82 120x36` a **t=618 ms**, estable hasta t=4.573 ms. El resultado anterior era un **falso negativo** de la sonda: el punto de salida caía dentro de la huella desplegada y el puntero nunca abandonaba el área interactiva. | Director de Desarrollo |
| 2026-11-03 | E3 | Nuevo hallazgo H-E3-07 (deriva de la barra flotante) | Hallazgo de **geometría**, no de interacción: la barra flotante arranca el despliegue en una posición distinta de la del reposo y converge en el mismo ciclo (`drift=0`). Causa: `DockPlacementCalculator.GetFloatingBounds` recalcula `GetNearestEdge(current, workArea)` sobre el rectángulo **ya expandido**, de modo que el crecimiento puede cambiar de lado entre estados. Es una decisión de ingeniería de ADR-003 §6.3 y §12 #5 (ancla por borde más cercano), **no un criterio de aceptación de E3**, y no afecta a la visibilidad ni al colapso. Queda registrado como H-E3-07 con el tratamiento pendiente de decisión del usuario. | Director de Desarrollo |
| 2026-11-03 | E3 | Verificación de `qa`: veredicto **APROBADA CON OBSERVACIONES** | Builds Release y Debug con `--no-incremental`: **0 errores y 0 advertencias**. `dotnet test` **390/390** en verde, sin esperas reales. Los cinco `grep` de control de ADR-004 §9.4 en el resultado exigido. E3-CA1..E3-CA5: **4 Cumple · 1 Cumple parcialmente · 0 No cumple**; el parcial es `E3-CA1` en su mitad *visual* del micro-efecto (no medible por prueba automatizada, diferida a M-05). **H-E3-05 y H-E3-06 quedan verificados sobre el código real**. Gracia end-to-end: 300 ms → 507/507 ms, 400 ms → 619/613 ms, 500 ms → 704/684 ms. La etapa **puede cerrarse** con tres condiciones: registrar QA-E3-01 sin reabrir E3, resolver QA-E3-09 y corregir la causa de H-E3-07, y aceptar formalmente el diferimiento de M-05…M-11 y de la geometría del borde `Bottom` a E8. | QA |
| 2026-11-03 | E3 | Medición del RNF por `qa` en Release publicado | Barra colapsada, cursor fuera de la ventana (`1500,800`), 30 muestras a 1 s. **CPU 0,0521 % de un núcleo** (cumple < 1 %). **RAM privada comprometida 44,02 MB** (rango 43,93–44,17). Cumple el límite de 50 MB con 5,98 MB de margen, pero **excede en 2,02 MB el presupuesto interno de 42 MB** fijado en Q-08. *Working set* 94,32 MB (dato informativo, no umbral). La medición de E1 fue 36,5 MB: el crecimiento de 7,5 MB corresponde a E2+E3 (docking, interacción y animaciones declarativas). | QA |
| 2026-11-03 | E3 | Cierre de H-E3-05 y H-E3-06 | Ambos hallazgos pasan a **Cerrado** con la implementación de ADR-004 verificada por `qa`: `WindowRegionReporter`, `WindowRegionTracker`, `RegionReport`, `CursorProbe` y `GetCursorPos` ya no existen en el árbol; la región `Bar` la declara `ContentPanel` y `SetState` purga las regiones de contenido; los siete `Storyboard` usan `FillBehavior="Stop"` con valor de reposo por `Setter` y fundido de salida alineado con el asentamiento. Los cinco `grep` de ADR-004 §9.4 no encuentran la maquinaria derogada. | Director de Desarrollo |
| 2026-11-03 | E3 | H-E3-07: causa corregida (QA-E3-02) | La causa localizada por el Director (`DockPlacementCalculator.GetFloatingBounds`) es **incompleta**: `GetFloatingBounds` es determinista para un rectángulo y un tamaño dados. El origen real es `DockPlacementCoordinator.ResolveFloating` (`:250-276`), que **re-deriva el ancla en cada transición** sobre el rectángulo **pre-redimensionado**, de modo que el mismo `current` alimenta dos estados con tamaños distintos y el ancla puede cambiar entre ellos. Se corrige la descripción del hallazgo; el tratamiento sigue **pendiente de decisión del usuario** (Q-13). No es un criterio de aceptación de E3. | QA |
| 2026-11-03 | E3 | Observaciones de `qa` registradas | **QA-E3-01** (alta): 44,02 MB frente al presupuesto de 42 MB → margen a vigilar en E4, E5, E7 y E8, **sin reabrir E3** porque el RNF de 50 MB se cumple. **QA-E3-02** (alta): causa de H-E3-07 corregida a `ResolveFloating`. **QA-E3-03** (baja): `BeginDrag` durante `Arming`/`Collapsing` fuerza `Tab`, desviándose de la tabla de ADR-004 §6. **QA-E3-04** (baja): falta prueba de orden «purga antes de `StateChanged`» (= D-19). **QA-E3-05** (= D-18): la matriz de presencia y el `Visibility` del XAML son dos declaraciones del mismo hecho. **QA-E3-06** (= D-20): una sola constante gobierna tres duraciones. **QA-E3-07** (informativa): `Expand()` no arma la gracia, coincide con ADR-004 §14.2 y es alcance de E6. **QA-E3-08** (informativa): ADR-004 §9.5 describe siete aserciones por ciclo y la sonda ejecuta seis más una final (19 en total). **QA-E3-09** (alta): contradicción documental sobre quién asume el presupuesto de 42 MB → se registra como **Q-12**. **QA-E3-10** (baja): la sonda no ejercita la migración v1→v2; queda como M-08. | QA |
| 2026-11-03 | E3 | Diferimiento formal a E8 | Se acepta el diferimiento a E8 de las verificaciones manuales **M-05 a M-11** y de la **geometría del borde `Bottom`** —no verificable en este equipo porque la barra de tareas ocupa el borde inferior y `ChooseDockEdge` repliega a `Top`; la corrida «Bottom» mide E2-CA2/E2-CA4, no la geometría inferior—. Ninguna de ellas es un criterio de aceptación automatizable de E3: E3-CA1..CA5 quedan cubiertos por la suite y por la sonda. Quedan anotadas en el informe de entrega de E8, donde ya está prevista la comprobación en Windows 10 y Windows 11 x64. | Director de Desarrollo |
| 2026-11-03 | E3 | **Cierre de E3** | Revisión final del Director: `dotnet build -c Release` y `-c Debug` con 0 errores y 0 advertencias, `dotnet test` 390/390, los cinco `grep` de ADR-004 §9.4 limpios, sonda de 19/19 en `Top`, `Left`, `Right`, `Float` y repliegue de `Bottom`, y árbol de trabajo limpio tras el commit `98f3387` (`fix(interaction): derive the interactive region from painted pixels per ADR-004`, 17 archivos, +1833 / −796). E3 pasa a **Completada al 100 %**; el plan queda al **44 %** (4 de 9 etapas, 37 de 87 puntos). Quedan abiertas las observaciones QA-E3-03, QA-E3-04, QA-E3-05, QA-E3-06 y QA-E3-10, la deuda D-02, D-06, D-08, D-10 a D-15 y D-17 a D-20, las verificaciones manuales M-01 a M-11 y las preguntas Q-10, Q-11 y Q-13. | Director de Desarrollo |
| 2026-11-03 | E1, E3, E4, E5, E7, E8 | Resolución de Q-12 | Se recalibra **una sola vez** el presupuesto interno de memoria privada de peor caso, de 42 MB a **46 MB** (44,02 MB medidos en el cierre de E3 más ~2 MB de holgura). El **límite de 50 MB del RNF sigue siendo la puerta dura** y el criterio de aceptación de E8; E3 queda aceptada con su medición y **E4, E5, E7 y E8 verifican contra 46 MB**. Los ADR **no se reescriben**: `ADR-003 §10` y `ADR-004 §10` conservan 42 MB como texto histórico, y el valor vigente consta en **Q-08** (nota de vigencia), en **R-02** y en **Q-12**. Se actualizan QA-E3-01 y QA-E3-09, que quedan **resueltas**. | Usuario |
| 2026-11-03 | E3, E4 | Resolución de Q-13 | Se adopta la **opción 1**: H-E3-07 se **corrige al abrir E4 con un ADR-005 breve** que fija el ancla de la barra flotante en el momento de soltarla y la conserva mientras la barra siga flotante, en lugar de re-derivarla en cada transición (`DockPlacementCoordinator.ResolveFloating`, `:250-276`). La corrección forma parte del contrato de E4, que cierra el hallazgo cuando esté implementada y verificada por `qa`. Con **Q-12** y **Q-13** resueltas no queda ninguna pregunta que bloqueara el cierre de E3; siguen diferidas a E4 **Q-10** (¿el colapso cierra la Cinta abierta?) y **Q-11** (¿el micro-efecto se repite en cada activación?). | Usuario |
| 2026-11-03 | E4 | Resolución de Q-10 | Se adopta la **opción 1**: **todo colapso cierra la Cinta que estuviera abierta**, tanto el automático por gracia como el explícito de E6. El reposo es la pestaña sola y al redesplegar todas las Cintas aparecen cerradas. El cierre es **efecto del estado** —se resuelve donde se abandona `Expanded`, de modo que los dos caminos de colapso no puedan divergir—, la Cinta abierta **no se persiste** y la exclusividad de HU03 CA #2 se mantiene. Encaja con la matriz `DockRegionPresence` de ADR-004, que ya purga `Ribbon` al pasar a `Tab`; el cierre del estado de apertura es contenido de E4. | Usuario |
| 2026-11-03 | E4 | Resolución de Q-11 | Se adopta la **opción 1**: el micro-efecto de expansión se emite **en cada activación** por *hover*, no solo la primera vez de la sesión. Es la lectura literal de §4.3-02 y la que mantiene un *feedback* coherente; no añade código ni estado, porque el *storyboard* de `TabBorderStyle` en el `DataTrigger` de `Arming` ya usa `FillBehavior="Stop"` con `AutoReverse` (ADR-004 D4) y `MicroExpansion_StopsAndReturnsToItsBaseValue` es su garantía. **E4 no debe introducir ninguna bandera de sesión** en el disparador. Con esta resolución **no queda ninguna pregunta abierta**: Q-10, Q-11, Q-12 y Q-13 están resueltas y E4 arranca sin bloqueos. | Usuario |
| 2026-11-14 | E4 | Implementación de ADR-005 y E4 entregada | El `implementador` ejecuta `ADR-005` (`_anchor` por instancia en `DockPlacementCoordinator`, `SetFloatingAnchor`, derivación única y limpieza al acoplar) y E4 (modelo plano `Ribbon`/`RibbonItem`, migración v2→v3 `AddRibbonsMigration`, CRUD en `DockBarViewModel`/`RibbonViewModel` con `IRibbonDeletionConfirmer`, acordeón exclusivo, cierre de Cinta al abandonar `Expanded`, cálculo dinámico en `DockBarMetrics.GetDynamicSize` y `RibbonPanel` perpendicular en `MainWindow.xaml`). Suite: **422/422** (+32 pruebas). Sin commit. | Implementador |
| 2026-11-14 | E4 | Verificación de `qa`: veredicto **APROBADA CON OBSERVACIONES** | Builds Release y Debug con `--no-incremental`: **0 errores y 0 advertencias**. `dotnet test`: **422/422** en verde. Greps de ADR-005 §6.2 y ADR-004 §9.4 limpios. 12/12 criterios en **Cumple** (ADR-005, ADR-004, E4-CA1..E4-CA7, Q-10, Q-11). Medición RNF en Release (30 muestras a 1 s): RAM privada comprometida **40,38 MB** promedio (máx 40,76 MB, < 46 MB Q-12 y < 50 MB RNF); CPU en reposo **0,0065 %** promedio (< 1 %). Registra 3 observaciones menores de UX (`OBS-01`..`OBS-03`) y verificaciones manuales visuales (`M-12`..`M-14`). | QA |
| 2026-11-14 | E4 | **Cierre de H-E3-07 y E4** | Se cierra **H-E3-07** con ADR-005 verificado por `qa`. E4 pasa a **Completada al 100 %**; el plan avanza al **56 %** (5 de 9 etapas, 45 de 87 puntos). Quedan registradas `OBS-01`..`OBS-03` y `M-12`..`M-14` para seguimiento en E5/E8. | Director de Desarrollo |
| 2026-11-14 | E5 | Implementación de E5 entregada | El `implementador` implementa `ShellLinkReader` (`IShellLinkW`/`IPersistFile` + extracción PNG y `DestroyIcon`/`FinalReleaseComObject` en `finally` en `StickyDock.Interop`), `ShortcutLauncher` (lectura fiel de destino/argumentos/workingDirectory, Q-06), `IUserAlertService`, migración v3→v4 (`AddBarItemsMigration`), `ShortcutDragDropBehavior` (drop externo `.lnk` estricto per Q-01 y reordenamiento interno), menú contextual sin edición y conexión de `BarItemCount`. Suite: **442/442** (+20 pruebas). Sin commit. | Implementador |
| 2026-11-14 | E5 | Verificación de `qa`: veredicto **APROBADA** | Builds Release y Debug con `--no-incremental`: **0 errores y 0 advertencias**. `dotnet test`: **442/442** en verde. 6/6 criterios (`E5-CA1..E5-CA6`) en **Cumple**. Interop aislado en `StickyDock.Interop`, `MainWindow.xaml.cs` limpio, greps ADR-004 §9.4 y ADR-005 §6.2 en verde. Medición RNF en Release (30 muestras a 1 s): RAM privada comprometida **40,79 MB** promedio (máx 40,93 MB, < 46 MB Q-12 y < 50 MB RNF); CPU en reposo **0,032 %** (< 1 %). | QA |
| 2026-11-14 | E5 | **Cierre de E5** | E5 pasa a **Completada al 100 %**; R-03 pasa a **Mitigado**; el plan avanza al **67 %** (6 de 9 etapas, 58 de 87 puntos). Quedan registradas las verificaciones manuales visuales `M-15`..`M-17`. | Director de Desarrollo |
| 2026-11-14 | E6 | Implementación inicial de E6 y rechazo por RNF-01 | El `implementador` entrega `WinFormsTrayIconService` (`NotifyIcon`), `WindowsAutoStartService`, `SettingsViewModel`, `SettingsWindow` y `ShutdownMode="OnExplicitShutdown"` (464/464 pruebas). `qa` verifica los 5 criterios (`E6-CA1..E6-CA5`) pero **rechaza** E6 por RNF-01: `System.Windows.Forms.dll` eleva la memoria privada comprometida a **61,20 MB** promedio (máx. 62,76 MB > 50 MB). | Implementador / QA |
| 2026-11-14 | E6 | `ADR-006` + `Win32TrayIconService` y ajuste RNF-01 | Se registra `ADR-006` (`Shell_NotifyIconW` + `CreatePopupMenu`/`TrackPopupMenuEx` nativo en `StickyDock.Interop` con `GetMessagePos()` y `HWND_MESSAGE`), `System.GC.ConserveMemory=5` / `RetainVMGarbageCollection=false` en `StickyDock.App.csproj`, y `RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly` en `App.xaml.cs` (elimina ~26 MB del driver Direct3D en una barra minimalista). Suite: **464/464** en verde. | Director / Implementador |
| 2026-11-14 | E6 | Verificación final de `qa`: veredicto **APROBADA** | Builds Release y Debug con `--no-incremental`: **0 errores y 0 advertencias**. `dotnet test`: **464/464** en verde. Greps ADR-004 §9.4 y ADR-005 §6.2 limpios. 5/5 criterios (`E6-CA1..E6-CA5`) en **Cumple**, 0 defectos. Medición RNF en Release (30 muestras a 1 s): `PrivateMemorySize64` **33,71 MB** promedio (rango 33,61–33,79 MB, `PrivateBytes` 33,70 MB, < 46 MB Q-12 y < 50 MB RNF-01); CPU en reposo **0,000 %** (< 1 %). | QA |
| 2026-11-14 | E6 | **Cierre de E6** | E6 pasa a **Completada al 100 %**; el plan avanza al **78 %** (7 de 9 etapas, 66 de 87 puntos). Quedan registradas las verificaciones manuales visuales `M-18`..`M-20` y `ADR-006` en estado *Propuesta* para ratificación del usuario. | Director de Desarrollo |
| 2026-11-15 | E7 | Implementación de E7 entregada | El `implementador` entrega `Win32MonitorCatalog` (`EnumDisplayMonitors`/`GetMonitorInfoExW`/`GetDpiForMonitor` sin WinForms), `TaskbarMonitor.GetTaskbarPlacementForMonitor`, `AddMonitorReplicaMigration` (esquema v4→v5), selección de monitores secundarios en `SettingsViewModel`/`SettingsWindow`, sincronización espejada vía `IConfigurationDocument.Changed` en `DockBarViewModel` (`IDisposable`), y `MultiMonitorCoordinator`/`WpfBarHostFactory` para desconexión y reconexión. Suite: **477/477** (+13 pruebas). Sin commit. | Implementador |
| 2026-11-15 | E7 | Verificación de `qa`: veredicto **APROBADA CON OBSERVACIONES** | Builds Release y Debug con `--no-incremental`: **0 errores y 0 advertencias**. `dotnet test`: **477/477** en verde. 6/6 criterios (`E7-CA1..E7-CA5`, `Q-04`) en **Cumple**; greps ADR-004 §9.4 y ADR-005 §6.2 limpios; `WinFormsModulesLoaded: 0`. RNF en Release (30 muestras a 1 s): CPU **0,0064 %** (< 1 %); RAM privada `PrivateMemorySize64` **47,02 MB** promedio (máx. 47,14 MB — cumple RNF-01 `< 50 MB`, supera en ~1,02 MB el presupuesto interno Q-12 `< 46 MB`, asignado a perfilado de E8). | QA |
| 2026-11-15 | E7 | **Cierre de E7** | E7 pasa a **Completada al 100 %**; R-04 pasa a **Mitigado**; el plan avanza al **89 %** (8 de 9 etapas, 74 de 87 puntos). Quedan registradas `OBS-E7-01` (presupuesto Q-12 para E8) y `M-21` (verificación física multimonitor). | Director de Desarrollo |
| 2026-11-17 | E5 (fix) | **Corrección de Drag & Drop desde Explorador** | El usuario reporta imposibilidad de agregar accesos directos por Drag & Drop. Causa raíz: `TabBorder` carecía de `AllowDrop="True"` / `ExternalDropCommand` y `PointerRegionBehavior` solo escuchaba `MouseEnter`/`MouseLeave` (suprimidos durante `OLE DoDragDrop`). Se añaden `DragEnter`/`DragLeave` (`FileDrop`) en `PointerRegionBehavior`, `ExternalDropCommand` en `TabBorder` y fallback `UseShellExecute` en `ShortcutLauncher`. Validada la lectura + icono PNG de los 3 `.lnk` de prueba (`Docker Desktop`, `Microsoft Edge-VALKHYRIA`, `Ubuntu` → `wsl.exe -d Ubuntu --cd ~`). Builds Release/Debug limpios; suite **480/480** en verde. | Director / Implementador |
| 2026-11-17 | E4/E5 (UI) | **Ajuste visual de barra y Cintas** | Se alinea `MainWindow.xaml` con el diagrama de usuario y el diseño del *toolbox*: cápsula translúcida redondeada (`CornerRadius="12"`), agarre `⋮`, cuadros de Cintas (`34×34`) con flecha vectorial direccional (`↓`/`↑`/`→`/`←`) y numeración sutil (`DisplayNumber`), botón `+` cálido destacado, icono de aplicación embebido (`icons/sticky-24x24.png`) en el extremo derecho/inferior, y `RibbonPanel` perpendicular con `#DisplayNumber` al inicio, accesos directos en el cuerpo y botón de edición `✎` (`ToggleActionsCommand` inline) al final. `DockBarMetrics` ajustado. `dotnet build -c Debug` limpio; suite **483/483** en verde (`Release` pendiente de cerrar instancia previa de `StickyDock.exe` abierta por el usuario). | Director / Implementador |
| 2026-11-17 | E4/E5 (UI) | **Refinamiento visual Win7 Glass y Cintas angostas** | Según retroalimentación visual del usuario: `TabBorder` acoplada muestra cápsula delgada Win7 glass sin texto `"Sticky"` y chevron direccional sutil (`⌄`/`⌃`/`›`/`‹`), y en modo flotante muestra `sticky-64x64.png` + `"Sticky"`; el icono en `ContentPanel` pasa al inicio (izquierda/arriba); `ContentPanel` fija su grosor (`52`) con `DockPanel.Dock` para no estirarse al abrir Cintas; `RibbonPanel` se convierte en cinta angosta (`54`) alineada con su botón indicador (`OpenRibbonOffset` + `RibbonOffsetToMarginConverter`); estética Win7 Aero Glass translúcida (sin verde) y flechas solo cabeza sin relleno. Builds Debug/Release limpios; suite **487/487** en verde. | Director / Implementador |

---

## Riesgos abiertos

| ID | Riesgo | Etapa | Probabilidad | Impacto | Mitigación | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| R-01 | El runtime de Windows App SDK complica el empaquetado en Windows 10 | E0, E8 | — | — | Cerrado por Q-02: con WPF no hay dependencia de runtime externo | **Cerrado** |
| R-02 | `AllowsTransparency` encarece el coste en reposo y pone en riesgo el RNF de RAM | E1, E3 | Media | Medio | Medición contrastada (Q-08): 36,5 MB de memoria privada frente a 50 MB. Medición de cierre de E3 en Release publicado: **44,02 MB** (43,93–44,17), con **5,98 MB** de margen frente al límite. El crecimiento de 7,5 MB respecto de E1 corresponde a E2+E3 (docking, interacción y animaciones declarativas). El riesgo sigue **mitigado pero con menos holgura**: la medición cumple el presupuesto interno vigente de **46 MB** (recalibrado en **Q-12**) con **1,98 MB** de margen. Se vigila en E4, E5, E7 y E8 | **Mitigado** (2026-11-03; margen de 5,98 MB frente al RNF de 50 MB y de 1,98 MB frente al presupuesto de 46 MB) |
| R-03 | Interop COM de `IShellLink` con *marshaling* de iconos | E5 | Media | Alto | Etapa aislada y con prueba dedicada de un `.lnk` con argumentos | **Mitigado** (2026-11-14; `ShellLinkReader` aislado en Interop con `DestroyIcon`/`FinalReleaseComObject` en `finally` y prueba real end-to-end) |
| R-04 | Sincronización de estado entre las réplicas multimonitor | E7 | Media | Medio | Un único origen de verdad en el proyecto de núcleo | **Mitigado** (2026-11-15; `IConfigurationDocument` único + `DockBarViewModel.Dispose` + `MultiMonitorCoordinator`) |
| R-05 | *Work area* y DPI mixto con varias escalas de pantalla | E2, E7 | Alta | Medio | ADR-002: geometría en píxeles de dispositivo (`GetMonitorInfoW` + `GetDpiForWindow`), `PerMonitorV2` (AMP-03) y pruebas de matriz de `GetDockedBounds`; verificación manual con dos escalas | **Mitigado** (2026-10-28; queda la verificación manual M-03 con dos escalas) |

---

## Hallazgos de auditoría (abiertos)

Hallazgos detectados por el Director al revisar el código real, no los informes de los subagentes. Una etapa con un hallazgo abierto no se cierra.

| ID | Etapa | Hallazgo | Evidencia | Estado |
| --- | --- | --- | --- | --- |
| H-E2-01 | E2 | El acoplamiento no tiene efecto: el borde calculado al soltar el arrastre se descarta y la ventana nunca se posiciona en el borde. La detección de barra de tareas y *work area* del coordinador son valores fijos. El cambio de área de trabajo solo se registra en el log. | Corregido en el commit `2aa510a`: `DockPlacementCoordinator` decide en píxeles de dispositivo y `WpfDockWindowHost.ApplyBounds` mueve la ventana con `SetWindowPos`; `Win32WindowGeometrySource` lee `GetMonitorInfoW`, `GetDpiForWindow` y la barra de tareas real. Arrastre real verificado: arriba `900,0`, derecha `1800,506`, izquierda `0,506`, centro flotante `640,382`, con `dockEdge` 0/2/1/null respectivamente. | **Cerrado** (ADR-002, 2026-10-28) |
| H-E2-02 | E2 | Ninguna prueba automatizada cubre E2: la suite sigue en 60 pruebas, la misma cifra que al cerrar E0. | Corregido en el commit `2aa510a`: 138 pruebas nuevas de E2 (calculador, coordinador con dobles, `AppBarEdgeMapping`, `DockingMessageClassifier`, `WinEventDockingEventSource`, persistencia del borde). La suite pasa de 60 a **199** pruebas, todas en verde en Release y Debug. | **Cerrado** (ADR-002, 2026-10-28) |
| H-E2-03 | E2 | Lógica de interop y arrastre en el code-behind de la vista (`DwmSetWindowAttribute` por P/Invoke directo, `DragMove`, coordenadas de pantalla), en contra de §5.2-01 y E0-CA6. | Corregido en el commit `2aa510a`: `MainWindow.xaml.cs` queda con `InitializeComponent` y los dos manejadores de *hover* de E1. Grep de control sin `DragMove`, `DllImport`, `nint`/`IntPtr`, `HwndSource` ni `catch {}` en `src\StickyDock.App\Views` ni en `src\StickyDock.Core`. | **Cerrado** (ADR-002, 2026-10-28) |
| H-E2-04 | E2 | El hook global de `EVENT_OBJECT_LOCATIONCHANGE` se dispara con cualquier cambio de posición de cualquier ventana del sistema y no filtra por barra de tareas: riesgo de coste de CPU. | Corregido en el commit `2aa510a`: `WinEventDockingEventSource` se instala para el proceso de `explorer.exe` y descarta en el callback toda ventana que no sea `Shell_TrayWnd`, y solo eleva el evento si la colocación de la barra de tareas cambió. Medición: 78 movimientos de una ventana ajena en 5,04 s → **0,000 %** de CPU y log sin líneas nuevas (400 movimientos en 86 ms, mismo resultado). | **Cerrado** (ADR-002, 2026-10-28) |
| H-E1-01 | E1 | El criterio de RAM de E1 se cerró sin cumplirse según la métrica de A5 (*working set* 89,4 MB > 50 MB). Con la métrica adoptada en Q-08 (memoria privada comprometida, 36,5 MB) **sí se cumple**, con 13,4 MB de margen. | `docs\plan\mediciones\RNF-01-02-reposo-2026-10-26.md` | **Cerrado** (Q-08, 2026-10-26) |
| H-E3-05 | E3 | **Estado pegado**: un `Enter(Bar)` derivado del rectángulo de la ventana revierte `Collapsing → Expanded` y la región no se libera nunca; la barra deja de colapsar y de reaccionar al *hover*. La prueba de regresión a nivel de controlador no reproduce el fallo real de la capa WPF. | **Reabierto** el 2026-11-01. Medición sobre el binario Release: en el ciclo 2, y tras reentrar en el rectángulo durante el colapso, la ventana permanece en `96x168` más de 2.400 ms sin colapsar y el *hover* sobre la pestaña ya no produce cambio. En el log, la secuencia por ciclo es `Leave Tab` → 400 ms → `Collapsing` → **131–135 ms** → `Enter Bar → Expanded`, y nunca aparece `Pointer left region Bar`. Los 131–135 ms coinciden con el final del fundido de salida (120 ms): al desaparecer el último píxel pintado, el sistema deja de entregar el puntero y `WindowRegionReporter.Reconcile` (l. 282-317), que decide con `WindowPlacement.GetBounds`, lo vuelve a declarar «dentro». **Corregido el 2026-11-03** con ADR-004: se borran `WindowRegionReporter`, `WindowRegionTracker`, `CursorProbe` y `GetCursorPos`; la región `Bar` la declara `ContentPanel` —el elemento que se pinta— con `PointerRegionBehavior`; la matriz `DockRegionPresence` descarta la entrada de una región que no existe en el estado vigente y `SetState` purga las regiones de contenido **antes** de elevar `StateChanged`; `Enter(Tab)` durante `Collapsing` ya no revierte. Sonda en los cinco bordes: 19 PASS / 0 FAIL y ningún `Enter Bar` espurio. | **Cerrado** (ADR-004, verificado por `qa` el 2026-11-03) |
| H-E3-06 | E3 | **Panel desplegado e invisible a partir del segundo ciclo**: la ventana crece a la huella desplegada pero el contenido no se pinta. | **Abierto** el 2026-11-01. Medición con color de píxel en `(1840,500)`: ciclo 1 `195,195,195` (visible); ciclo 2 `15,15,15` —el fondo— durante los 1.400 ms del despliegue, con la ventana ya en `96x168` y `WindowFromPoint` devolviendo la ventana de Chrome (*click-through*). Causa: `ContentPanelStyle` (`MainWindow.xaml` l. 47-166) anima `Opacity` desde `EnterActions` con `FillBehavior="HoldEnd"` y sin `ExitActions`; el reloj del colapso queda retenido en 0 y prevalece sobre el del despliegue. **Corregido el 2026-11-03** con ADR-004 D4: `FillBehavior="Stop"` en los siete `Storyboard` del archivo, valor de reposo declarado con `Setter` (`Opacity = 1` en el estilo, `Opacity = 0` en el `DataTrigger` de `Collapsing`), fundido de entrada sin `From` y fundido de salida alineado con el asentamiento (`0:0:0.16`). Medición del píxel del panel en los tres ciclos de la sonda, en los cinco bordes: `197,197,197` los tres ciclos (antes `15,15,15` en el ciclo 2). | **Cerrado** (ADR-004, verificado por `qa` el 2026-11-03) |
| H-E3-07 | E3 | **Deriva de la barra flotante**: al desplegarse, la barra flotante arranca en una posición distinta de la del reposo y converge a la misma huella dentro del mismo ciclo. Causa real (corregida en QA-E3-02): `DockPlacementCoordinator.ResolveFloating` (`:250-276`) **re-deriva el ancla en cada transición** sobre el rectángulo **pre-redimensionado**, de modo que el mismo `current` alimenta dos estados con tamaños distintos y el ancla puede cambiar entre ellos. `DockPlacementCalculator.GetFloatingBounds` es determinista para un rectángulo y un tamaño dados y **no** es la causa. | **Corregido el 2026-11-14** con `ADR-005` (D1–D8 en `DockPlacementCoordinator.cs`): `_anchor` por instancia fijado al soltar/desacoplar, derivado una sola vez al arrancar flotante, borrado al acoplar y conservado en `ResolveFloating` salvo recorte por `ClampIntoWorkArea`. Verificado con las 5 pruebas de `DockPlacementCoordinatorTests.cs` y los 3 greps de `ADR-005 §6.2`. | **Cerrado** (ADR-005, verificado por `qa` el 2026-11-14) |

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

### Q-08 — Métrica del RNF de memoria

- **Estado**: **Resuelta**
- **Decisión**: se adopta la **memoria privada comprometida** (`PrivateBytes`) como métrica del RNF de RAM. Medido en reposo: **36,5 MB** frente al límite de 50 MB. El *working set* queda como dato informativo, no como umbral. Se fija un **presupuesto interno de 42 MB** para el peor caso, verificado en E4, E5, E7 y E8. *Nota de vigencia*: **Q-12** (2026-11-03) recalibra ese presupuesto a **46 MB**; el **límite de 50 MB del RNF sigue siendo la puerta dura** y el criterio de aceptación de E8. El valor vigente para E4, E5, E7 y E8 es **46 MB**.
- **Fecha**: 2026-10-26 · **Decidido por**: usuario
- **Bloquea**: E1 (desbloqueada; H-E1-01 cerrado) y el criterio de RAM de E8 (fijado)
- **Hueco detectado**: el RNF dice «RAM < 50 MB en estado de ejecución normal» sin fijar la métrica. La interpretación A5 de este documento eligió *working set*. La medición del Director en Release, con la barra colapsada y durante 30 s, da **89,6 MB de *working set*** y **36,8 MB de memoria privada**.
- **Consecuencia**: con *working set*, E1-CA5 no se cumple y R-02 queda materializado (ADR-001 tendría que revisarse). Con memoria privada, se cumple con margen y R-02 se cierra.
- **Medición contrastada** (`docs/plan/mediciones/RNF-01-02-reposo-2026-10-26.md`, 40 muestras a 1 s):

  | Métrica | Valor medio | ¿Cumple < 50 MB? |
  | --- | ---: | :---: |
  | *Working set* total | 89,4 MB | No |
  | *Working set* privado | 23,1 MB | Sí |
  | Memoria privada comprometida | 36,5 MB | Sí |
  | CPU en reposo | 0,16 % de un núcleo | Sí (< 1 %) |

  Del *working set*, **66,3 MB (74,1 %) es compartido** con DLL de WPF, .NET y el controlador gráfico. Tras `EmptyWorkingSet`, el conjunto de trabajo cae a 5,1 MB en 15 s sin tocar la memoria comprometida: son páginas reclamables, no demanda real.
- **Recomendación del Director** (aceptada por el usuario): adoptar **memoria privada comprometida** (36,5 MB) como métrica del RNF, con el *working set* como dato informativo.
- **Opciones**:
  1. **Mantener *working set* como métrica** y abrir una corrección de rendimiento: implica optimizar el renderizado de la transparencia (posible renuncia a `AllowsTransparency`) o revisar ADR-001. — Descartada.
  2. **Adoptar memoria privada comprometida como métrica** (36,5 MB): se declara la interpretación, se actualiza A5 y E1-CA5 pasa a verde. El *working set* queda como dato informativo, no como umbral. — **ELEGIDA**.
  3. ~~Medir antes de decidir con una herramienta dedicada~~ — **ejecutada**: es la medición contrastada de arriba.

### Q-09 — ¿Quién expone la configuración del temporizador de gracia?

- **Estado**: **Resuelta**
- **Decisión**: **E3 entrega el valor, la validación 300–500 ms, la migración de esquema a la versión 2, la persistencia y la aplicación inmediata del cambio.** La ventana de configuración general —abierta desde la bandeja del sistema— es alcance de **E6** (§4.6-01, HU05 CA #1). E3-CA5 se verifica editando `settings.json` y con pruebas de migración, normalización y persistencia, no sobre un control de interfaz.
- **Fecha**: 2026-10-28 · **Decidido por**: usuario
- **Bloquea**: E3 (desbloqueada; ADR-003 aceptada)
- **Hueco detectado**: E3-CA5 exige que la gracia sea «configurable por el usuario dentro del rango 300–500 ms» y que persista, pero ningún documento asigna a E3 una interfaz de usuario; §4.6-01 la sitúa en la bandeja, que es de E6.
- **Opciones consideradas**:
  1. **Diseño completo, interfaz en E6** — **ELEGIDA**. E3 aporta el comportamiento completo; E6 aporta el control que lo hace accesible. La lectura literal de «configurable» se satisface en el producto, y el criterio de la etapa se comprueba sobre la preferencia y su efecto.
  2. Adelantar a E3 una ventana mínima con un solo control — descartada: duplica trabajo de E6 y adelanta alcance ya planificado.
  3. Dejar la gracia fija en 400 ms hasta E6 — descartada: incumple E3-CA5 tal como está escrito.
- **Efecto en la implementación**:
  - `StickyDockConfiguration.GracePeriodMilliseconds` (400 por defecto), `GracePeriod.Normalize` para el rango 300–500 y `AddGracePeriodMigration` (1 → 2) entran en E3.
  - El controlador de interacción lee el valor del documento **en el instante de armar el temporizador**, así que un cambio en `settings.json` surte efecto sin reiniciar.
  - **Nota para `qa`**: E3-CA5 se valida (a) editando `settings.json` con 300, 400 y 500 ms y observando el tiempo de colapso, y (b) con las pruebas de migración, normalización y persistencia. No se exige un control de interfaz en esta etapa.

### Q-10 — Al colapsar la barra, ¿se cierra la Cinta que estuviera abierta?

- **Estado**: **Resuelta** (opción 1: el colapso cierra la Cinta abierta)
- **Decisión**: **todo colapso cierra la Cinta que estuviera abierta**, tanto el automático por temporizador de gracia como el explícito de E6. El estado de reposo es la pestaña sola: al redesplegarse, todas las Cintas aparecen cerradas. La Cinta abierta forma parte del área interactiva de la barra (A4) y no puede sobrevivir a la desaparición de su ancla.
- **Fecha**: 2026-11-03 · **Decidido por**: usuario
- **Origen**: §4.4 no lo dice. HU03 fija la exclusividad entre Cintas, no qué ocurre con una Cinta cuando la barra entera se oculta.
- **Opciones consideradas**:
  1. **El colapso cierra la Cinta abierta — ELEGIDA**. Coherente con A4 y con que una Cinta sin barra visible no tiene ancla; §4.2 describe el reposo como «una pequeña pestaña minimalista».
  2. La Cinta se conserva y reaparece abierta en el siguiente despliegue — descartada: deja una superficie visible sin la barra que la contiene, en contra del propósito de RF02 de no obstaculizar la pantalla durante el reposo.
- **Efecto en la implementación**:
  - El cierre de la Cinta es un efecto del **estado**, no de cada camino de colapso: se resuelve donde el estado abandona `Expanded`, de modo que el colapso automático y el explícito no puedan divergir.
  - La Cinta abierta **no se persiste** (coherente con la decisión de ADR-003 de no persistir el estado de la barra): tras reiniciar, todas arrancan cerradas.
  - La exclusividad de HU03 CA #2 (abrir una Cinta cierra la anterior) es independiente y se mantiene: cerrar la Cinta abierta al colapsar no altera la que estuviera cerrada.
- **Trazabilidad**: RF02, RF03, HU03 (CA #2), §4.2, §4.4-02, A4. ADR-003 deja la costura preparada (`DockRegion.Ribbon` y `StateChanged`) y ADR-004 la matriz `DockRegionPresence`, que purga `Ribbon` al pasar a `Tab`; la corrección no toca la máquina de estados.

### Q-11 — ¿El micro-efecto de expansión se emite en cada activación o solo la primera vez?

- **Estado**: **Resuelta** (opción 1: en cada activación)
- **Decisión**: el micro-efecto de expansión se emite **en cada activación** por *hover*, no solo la primera vez de la sesión. Cada vez que el temporizador de activación de 200 ms vence sobre la pestaña, la pestaña emite el micro-efecto y a continuación se despliega la barra. No se introduce ninguna bandera de sesión ni estado nuevo.
- **Fecha**: 2026-11-03 · **Decidido por**: usuario
- **Origen**: §4.3-02 lo describe «durante el *hover* sostenido (~200 ms) sobre la pestaña en reposo», sin distinguir la primera activación de la sesión de las siguientes. Ni HU02 CA #2 ni §4.3-01 introducen esa distinción.
- **Opciones consideradas**:
  1. **En cada activación — ELEGIDA**. Es la lectura literal de §4.3-02 y la que mantiene un *feedback* coherente: la pestaña confirma siempre que el *hover* está siendo reconocido. Además es lo que ya ocurre con el diseño vigente de **ADR-004 D4**: el micro-efecto usa `FillBehavior="Stop"` con `AutoReverse`, de modo que el reloj se libera al terminar y el efecto se reproduce en cada `Arming` sin ningún estado adicional. La opción no añade código; la alternativa exigiría añadirlo.
  2. Solo la primera vez por sesión — descartada: añade estado sin respaldo en la especificación y crea una diferencia perceptible entre el primer despliegue y los siguientes, con la pestaña respondiendo distinto a un gesto idéntico.
- **Efecto en la implementación**:
  - Ninguno sobre el código: el *storyboard* de `TabBorderStyle` en el `DataTrigger` de `Arming` ya usa `Stop` con `AutoReverse` (ADR-004 D4) y se dispara en cada entrada a `Arming`, que es el estado que se alcanza en cada activación por *hover*.
  - La comprobación existente `MainWindowStoryboardContractTests.MicroExpansion_StopsAndReturnsToItsBaseValue` **es** la garantía de esta decisión: verifica que las dos animaciones de escala van a `1.06` con `AutoReverse` y `Stop` en el `DataTrigger` de `Arming`. No hace falta ninguna prueba nueva, y E4 no debe introducir ninguna bandera de sesión en el disparador.
  - M-05 y M-09 cubren la comprobación visual de que el efecto se percibe en cada ciclo, no solo en el primero.
- **Trazabilidad**: HU02 CA #2, §4.3-02, §4.3-01, ADR-004 D4, E3-CA1, M-05, M-09.

### Q-12 — ¿Quién asume el presupuesto interno de 42 MB de memoria privada?

- **Estado**: **Resuelta** (opción 1: recalibrar el presupuesto interno a **46 MB**)
- **Decisión**: se recalibra el presupuesto interno de peor caso, **una sola vez**, de 42 MB a **46 MB** (44,02 MB medidos en el cierre de E3 más ~2 MB de holgura). El **límite de 50 MB del RNF sigue siendo la puerta dura** y el criterio de aceptación de E8. E3 queda aceptada con su medición y **E4, E5, E7 y E8 verifican contra 46 MB**. Los ADR no se reescriben: `ADR-003 §10` y `ADR-004 §10` siguen citando 42 MB como texto histórico, y el valor vigente consta en **Q-08**, en **R-02** y en este apartado.
- **Fecha**: 2026-11-03 · **Decidido por**: usuario
- **Origen**: contradicción documental detectada por `qa` (**QA-E3-09**). **Q-08** fija un presupuesto interno de **42 MB** de peor caso y lo asigna a E4, E5, E7 y E8; **`ADR-003 §10`** y **`ADR-004 §10`** lo exigen además como medición de cierre de **E3**. La medición de cierre de E3 es **44,02 MB** (43,93–44,17): cumple el RNF de 50 MB con 5,98 MB de margen y **excede en 2,02 MB** el presupuesto interno.
- **Hecho que complica la lectura literal**: si E3 se cierra en 44,02 MB y E4 debe medir contra 42 MB, E4 nace incumpliendo. El presupuesto no puede quedarse donde está sin reabrir E3 o sin declarar la etapa nueva incumplida de entrada.
- **Opciones**:
  1. **Recalibrar el presupuesto una sola vez a 46 MB** (44,02 medidos más ~2 MB de holgura), manteniendo el RNF de **50 MB** como puerta dura y como criterio de aceptación de E8. E3 queda aceptada con su medición y E4/E5/E7 verifican contra 46 MB. — *recomendada*: conserva la señal temprana del presupuesto, reconoce el coste real de E2+E3 (7,5 MB) y no falsea el criterio de la etapa.
  2. **Mantener 42 MB y asignar la reducción a E8**: E3 se acepta contra el RNF, el exceso de 2,02 MB se registra como deuda de rendimiento con dueño en E8, y E4/E5/E7 tienen prohibido crecer. — Coherente con Q-08, pero convierte el presupuesto en una línea base congelada en 44,02 MB y deja a E5 (iconos extraídos por COM) sin margen real.
  3. **Retirar el presupuesto interno** y medir solo contra el RNF de 50 MB en todas las etapas. — Elimina la contradicción borrando la señal: el margen se descubre al incumplir el RNF, en E8.
- **Trazabilidad**: RNF-01, Q-08, A5, R-02, `ADR-003 §10`, `ADR-004 §10`, QA-E3-01, QA-E3-09.
- **Efecto en la documentación**: la opción elegida se aplica en Q-08 y en la tabla de riesgos **R-02**. Las opciones 1 y 2 exigen dejar constancia del valor vigente en `ESTADO.md`; los ADR no se reescriben.

### Q-13 — Tratamiento de la deriva de la barra flotante (H-E3-07)

- **Estado**: **Resuelta** (opción 1: corregir al abrir E4 con un ADR-005 breve)
- **Decisión**: se **corrige en E4 con un ADR-005 breve**. El ancla de la barra flotante se fija en el momento de soltarla y se conserva mientras la barra siga flotante, en lugar de re-derivarse en cada transición. H-E3-07 pasa a ser parte del contrato de E4, que lo cierra cuando la corrección esté implementada y verificada por `qa`.
- **Fecha**: 2026-11-03 · **Decidido por**: usuario
- **Origen**: `docs/plan/ESTADO.md`, hallazgo H-E3-07. La barra flotante arranca el despliegue en una posición distinta de la del reposo y converge a la misma huella dentro del mismo ciclo (`drift=0`). Causa localizada por `qa` (**QA-E3-02**): `DockPlacementCoordinator.ResolveFloating` (`:250-276`) re-deriva el ancla con `GetNearestEdge` en cada transición sobre el rectángulo **pre-redimensionado**, de modo que el mismo rectángulo alimenta dos estados con tamaños distintos y el ancla puede cambiar entre ellos. `DockPlacementCalculator.GetFloatingBounds` es determinista y no es la causa.
- **Naturaleza**: es un defecto de **geometría** de una decisión de ingeniería de ADR-003 §6.3 y §12 #5 (ancla por borde más cercano), **no un criterio de aceptación de E3**, y no afecta a la visibilidad, al colapso ni al resto de criterios de la etapa.
- **Opciones**:
  1. **Corregirlo al abrir E4 con un ADR-005 breve**: fijar el ancla de la barra flotante en el momento de soltarla y conservarla mientras siga flotante, en lugar de re-derivarla en cada transición. — *recomendada*: E4 es la primera etapa que vuelve a cambiar el largo de la barra (`NotifyBarSizeChanged`), así que multiplica esta misma clase de defecto; corregirlo después cuesta más y con más geometría encima.
  2. **Diferirlo a E7**, que es la etapa que reutiliza `ResolveFloating` con una barra por monitor, y mientras tanto documentarlo como comportamiento conocido. — No reabre nada hoy, pero deja tres etapas con una deriva visible para el usuario de la barra flotante.
  3. **Aceptarlo como comportamiento definitivo** de la barra flotante y cerrar el hallazgo sin cambio de código. — Requiere justificar que la barra «salte» al desplegarse; el desplazamiento medido es de 24 px en X con ancla `Top`, perceptible.
- **Trazabilidad**: RF01, RF02, ADR-003 §6.3/§12 #5, H-E3-07, QA-E3-02.
- **Nota**: cualquiera de las tres opciones deja `ESTADO.md` coherente; la que se elija fija la etapa que asume el hallazgo en su contrato.

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
| A5 | Métrica de RAM y de CPU | **RAM: memoria privada comprometida** (`PrivateBytes` de `Win32_PerfFormattedData_PerfProc_Process`), con la barra colapsada en E1 y en el peor caso (desplegada, todas las Cintas) en E8. **CPU:** delta de `TotalProcessorTime` sobre un núcleo durante 30 s. El *working set* queda como dato informativo. Decidido en Q-08 | RNF-01 y RNF-02 no fijan métrica; el *working set* incluye DLL compartidas (74,1 % medido) y no mide el coste propio |
| A6 | «Detección de entrada de puntero **global**» (§5.1-04) | Detección por el **área de la ventana**, sin hook global de ratón, para no comprometer RNF-02 | El comportamiento observable de RF02 se satisface igual; el arquitecto lo confirma en E3 |
| A7 | «Longitud mínima» de la barra sin valor | Igual a la **pestaña en reposo más la zona de indicadores**; con cero elementos y cero Cintas, exactamente la pestaña | RF03 exige un mínimo y un crecimiento dinámico, sin fijar valor |

## Ampliaciones de alcance registradas (Fase B)

Cambios del plan que **exceden** lo que la especificación enumera. Se registran para que la desviación sea visible y no pase por requisito.

| ID | Ampliación | Motivo | Estado |
| --- | --- | --- | --- |
| AMP-01 | Persistir el **borde acoplado** y el **temporizador de gracia** | §5.2-02 enumera solo orden, Cintas y preferencias multimonitor. Sin persistir el borde, la barra vuelve a su posición inicial en cada arranque (E2-CA5, E3-CA5) | Aceptada como preferencia de usuario |
| AMP-02 | **SQLite no se implementa**: solo JSON versionado | §5.2-02 ofrece «JSON / SQLite liviano» como alternativa. Se elige JSON por simplicidad y menor huella de memoria (RNF-01) | Aceptada (ADR-001 ya lo asume) |
| AMP-03 | `app.manifest` con `dpiAwareness = PerMonitorV2` | Ningún RF lo pide; lo exige R-05 (DPI mixto): sin manifiesto la aplicación es *system DPI aware*, `GetDpiForWindow` es ambiguo y `SetWindowPos` descuadra en monitores con otra escala. Afecta al escalado de toda la aplicación, incluido lo ya cerrado en E1 | Aceptada (ADR-002, 2026-10-28) |
| AMP-04 | El esquema de persistencia sube a la **versión 2** con migración `AddGracePeriodMigration` | AMP-01 solo pide «persistir la gracia»; el cambio de esquema lo exige el hecho de que los `settings.json` de E0–E2 están en la versión 1 y E3-CA5 habla de persistencia entre ejecuciones. Sin migración, las instalaciones existentes arrancarían con valores por defecto | Aceptada (ADR-003, 2026-10-28) |

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

## Deuda técnica y verificaciones manuales abiertas (E2)

Registrada al cerrar E2. **Ninguno de estos puntos incumple un criterio de aceptación**: los seis CA de la etapa están verificados. Se anotan para que no se pierdan y para que las etapas siguientes los hereden con conocimiento.

### Defectos menores del informe de QA

| ID | Descripción | Etapa que debe abordarlo |
| --- | --- | --- |
| D-01 | `WpfDockWindowHost.GetBarSize()` usa `ActualWidth`/`ActualHeight`, así que un redimensionado impuesto desde fuera por `SetWindowPos` deja la barra con el tamaño intruso tras el `WM_EXITSIZEMOVE`. No se manifiesta en operación normal porque `ResizeBorderThickness = 0` impide al usuario redimensionarla | **En curso** — ADR-003 retira `IDockWindowHost.GetBarSize()` y declara el tamaño en `DockBarMetrics` + `IDockBarSizeSource`. Se cierra al verificar E3 |
| D-02 | El espejo de la posición a WPF cubre `Left`/`Top` pero no `Width`/`Height`, al contrario que la lista de verificación de ADR-002. Se acepta porque `ApplyBounds` fija el tamaño real por `SetWindowPos`; el espejo existe solo para que WPF no reescriba la posición | Documentado aquí; sin acción prevista |
| D-04 | `DockPlacementCoordinator.PersistEdge` escribe en *fire-and-forget* (`_ = SaveAsync(...)`). Acoplar y cerrar el proceso en el mismo instante podría perder la preferencia. No reproducible en las pruebas (el reinicio restauró siempre el borde) | **En curso** — ADR-003 introduce `IConfigurationDocument` con escritura serializada, `FlushAsync` y `Dispose` acotado a 2 s. Se cierra al verificar E3 |
| D-05 | **Corregido** el 2026-10-28: `HandleDragCompleted` trata un `PixelRect` vacío como liberación fuera de banda y persiste `null`, con la prueba `DragEnd_WithAnEmptyRect_DoesNotDock_AndPersistsNull` | — |
| D-06 | `WinEventDockingEventSource` resuelve `Shell_TrayWnd` una sola vez: si `explorer.exe` se reinicia, el hook queda inerte hasta el siguiente arranque. La ruta de mensajes sigue funcionando. Deuda ya declarada en ADR-002 | E8 |
| D-03 | **Corregido** el 2026-10-28: el comentario XML de `IDockWindowHost.BeginDrag` describe el mecanismo real (`WM_SYSCOMMAND` con `SC_MOVE`) y por qué difiere del texto de ADR-002 | — |

### Deudas detectadas por el Director al revisar el código

| ID | Descripción | Etapa que debe abordarlo |
| --- | --- | --- |
| D-07 | **Dos copias del documento de configuración**: `Bootstrapper` registra un `StickyDockConfiguration` como singleton, pero `DockPlacementCoordinator` recibe su propia instancia y persiste sobre ella. Cuando E3 o E6 guarden otras preferencias con la copia del contenedor, el `dockEdge` puede revertirse a un valor obsoleto. Debe quedar un único documento mutable compartido antes de que exista un segundo escritor | **En curso** — ADR-003 declara `IConfigurationDocument` como único escritor y único llamador de `IConfigurationStore.SaveAsync`. Se cierra al verificar E3 |
| D-08 | Un `settings.json` guardado con BOM UTF-8 rompe `JsonConfigurationStore` (`0xEF is an invalid start of a value`) y la aplicación arranca con valores por defecto. El archivo lo genera la propia aplicación sin BOM, así que solo ocurre si un editor externo lo añade; aun así, el lector debería tolerar el BOM | E8 (manejo de configuración corrupta) |
| D-09 | El coordinador solo recalcula la colocación al arrancar y ante eventos del entorno. Si la barra cambia de tamaño por contenido (crece con elementos o Cintas, E4) sin que llegue `WM_EXITSIZEMOVE` ni un cambio de área de trabajo, no se re-resuelve sola. Hará falta un `NotifyBarSizeChanged()` explícito en el contrato o en el host | **En curso** — ADR-003 añade `DockPlacementCoordinator.NotifyBarSizeChanged()` y lo cablea desde `IDockStateService.StateChanged` en la raíz de composición. Se cierra al verificar E3 |
| D-10 | `Undock()` conserva la misma cola que ahora vive en `ReleaseToFloating` (limpiar borde, persistir, elevar `PlacementChanged`). Unificarlas es un refactor sin cambio de comportamiento | Repaso de E2 |

### Verificaciones manuales pendientes (no reproducibles en este equipo)

El equipo de desarrollo es Windows 11 build 26300 con un solo monitor a 100 % de escala y la barra de tareas fija abajo. Estas comprobaciones quedan para el informe de entrega de E8, donde ya está previsto probar en Windows 10 y Windows 11 x64.

| ID | Verificación | CA o riesgo que cubre |
| --- | --- | --- |
| M-01 | Mover la barra de tareas a los bordes superior, izquierdo y derecho y comprobar que la barra de la utilidad migra al borde opuesto sin solaparse (requiere Windows 10 o una build que permita moverla) | E2-CA2, HU01 CA #2 |
| M-02 | Cambiar la resolución a 1280×720 y 800×600 con las Cintas abiertas (en E4 en adelante) y comprobar que la barra permanece completa dentro del área de trabajo y conserva el borde | E2-CA3, RF01 |
| M-03 | Conectar un segundo monitor a 150 % y arrastrar la barra hasta él: acople al borde libre de **ese** monitor, tamaño en píxeles acorde a su escala y convergencia sin oscilación | R-05, E7 |
| M-04 | Revisar el aspecto de la pestaña de E1 al 125 % y 150 %, y que el micro-efecto de expansión no se recorte tras adoptar `PerMonitorV2` | AMP-03, deuda de E1 |

---

## Deuda y verificaciones abiertas de E3 (ADR-003 y ADR-004)

Registrada al abrir E3 y ampliada con la corrección de H-E3-05 y H-E3-06. **Ninguno de estos puntos bloquea la etapa**: son consecuencias asumidas del diseño aprobado, no incumplimientos.

### Deuda declarada por ADR-003

| ID | Descripción | Etapa que debe abordarlo |
| --- | --- | --- |
| D-11 | Los *storyboards* de despliegue y del micro-efecto no tienen prueba automatizada; se verifican por inspección y con medición | E8 (documentación de arquitectura) |
| D-12 | `DockBarMetrics.Default` (120×36 plegada, 168×96 desplegada) debe mantenerse coherente con el contenido real de la vista; hoy no hay comprobación automática de esa coherencia | E4 (cuando el contenido cambie el tamaño) |
| D-13 | Si en E4 alguna Cinta se implementa como ventana emergente, su región no llegará por `PointerRegionBehavior` y habrá que registrarla explícitamente | E4 |
| D-14 | Los literales del XAML (±60 de desplazamiento y 0,16 s de duración) duplican `DockBarMetrics` e `InteractionTimings`. Es el precio de que `Timeline.From` y `Timeline.Duration` no admitan *binding*. **ADR-004 §4.5 lo convierte en comprobación de máquina**: `MainWindowStoryboardContractTests` deriva los literales desde Core, de modo que un desajuste rompe `dotnet test`. La deuda no se cierra —los literales siguen en el XAML—, pero deja de ser silenciosa | E8, o la etapa que cambie `DockBarMetrics` |
| D-15 | **La ventana no tiene *hit-test* selectivo**: `MainWindow` usa `Background="Transparent"` (hit-testeable en WPF) y no hay `WM_NCHITTEST` ni `HwndSource` filtrando. E1-CA4 se cumple porque la ventana colapsada mide exactamente la pestaña (120×36). **Cualquier ampliación futura de la huella plegada —margen, asa mayor, sombra, `Padding` en la raíz— hará que esa superficie intercepte clics y debe verificarse contra E1-CA4** | E8, y obligatoria para cualquier etapa que toque la huella plegada |
| D-17 | **Coordenadas negativas en la colocación** (no reproducido en este equipo; a verificar): el log de una sesión del 2026-10-31 registra `'Right' at (-96, 441, 96x168)`, `'Top' at (-1044, 1, 168x96)` y una barra flotante en `(-349, 478)` y `(-732, 523)`. Si el cálculo produjo esas coordenadas en un escritorio de un solo monitor, la barra quedó fuera de pantalla, en contra de E2-CA3. Las tres ocurrencias aparecen tras eventos de entorno, no en el arranque | E7 (multimonitor), con la verificación manual M-03; a confirmar antes con una sonda de colocación en el equipo de desarrollo |

### Deuda declarada por ADR-004 (corrección de H-E3-05 y H-E3-06)

| ID | Descripción | Etapa que debe abordarlo |
| --- | --- | --- |
| D-18 | **La matriz de `DockRegionPresence` y el `Visibility` del XAML son dos declaraciones del mismo hecho.** Hoy coinciden por construcción, pero la prueba de contrato no verifica la equivalencia completa «estado ↔ visibilidad»: comprueba que `Bar` está en `ContentPanel` y que existen los valores de reposo. Un elemento nuevo con una visibilidad que no siga la matriz solo se detecta midiendo | E4 (al añadir Cintas) |
| D-19 | **El orden «purga antes de `StateChanged`» no tiene prueba de orden explícita.** Se protege por inspección y por una aserción posterior; endurecerla exige afirmar `ActiveRegions` desde dentro del manejador del evento | Implementación de E3 |
| D-20 | **Una sola constante (`InteractionTimings.SlideMilliseconds`) gobierna tres duraciones**: el *slide* del despliegue, el asentamiento del controlador y el fundido de salida. Es lo que garantiza que el panel se apague justo cuando la ventana encoge, pero impide cambiar una sin las otras; separarlas exigiría una constante propia para el fundido y ajustar `FadeOutDuration_EqualsTheSettleDuration` | E8, o la etapa que ajuste las animaciones |

### Observaciones de `qa` en el veredicto de cierre de E3 (QA-E3-01 a QA-E3-10)

Registradas al aceptar el veredicto **APROBADA CON OBSERVACIONES** del 2026-11-03. **Ninguna reabre E3**: las de severidad alta se resuelven por decisión o por registro, y las de severidad baja quedan asignadas a una etapa. Las que exigen una decisión del usuario se elevan como pregunta.

| ID | Severidad | Observación | Tratamiento |
| --- | --- | --- | --- |
| QA-E3-01 | Alta | La medición de cierre de E3 da **44,02 MB** de memoria privada comprometida frente al **presupuesto interno de 42 MB** fijado en Q-08 (el límite del RNF de 50 MB sí se cumple, con 5,98 MB de margen) | **Aceptada sin reabrir E3**: el criterio de aceptación y el RNF se cumplen. El presupuesto interno queda **recalibrado a 46 MB** por **Q-12** (opción 1), de modo que la medición cumple con **1,98 MB** de margen. Se vigila en E4, E5, E7 y E8 contra ese valor |
| QA-E3-02 | Alta | La causa de H-E3-07 no es `DockPlacementCalculator.GetFloatingBounds`, sino `DockPlacementCoordinator.ResolveFloating` (`:250-276`), que re-deriva el ancla en cada transición sobre el rectángulo pre-redimensionado | **Aplicada**: H-E3-07 y el resumen recogen la causa corregida. `GetFloatingBounds` es determinista para un rectángulo y un tamaño dados |
| QA-E3-03 | Baja | `BeginDrag` durante `Arming` o `Collapsing` fuerza `Tab`, desviándose de la tabla de ADR-004 §6 | **Diferida a E4**: el arrastre es de E2 y no hay criterio de E3 afectado; se corrige cuando E4 revise la máquina con Cintas |
| QA-E3-04 | Baja | Falta una prueba de orden explícita de «purga antes de `StateChanged`» (= D-19) | **Diferida**: se refunde con D-19; el endurecimiento exige afirmar `ActiveRegions` desde dentro del manejador |
| QA-E3-05 | Baja | La matriz de `DockRegionPresence` y el `Visibility` del XAML son dos declaraciones del mismo hecho (= D-18) | **Diferida a E4**: es la etapa que añade elementos visibles y la única que puede desalinearlas |
| QA-E3-06 | Baja | Una sola constante (`InteractionTimings.SlideMilliseconds`) gobierna tres duraciones (= D-20) | **Diferida a E8**, o a la etapa que ajuste las animaciones |
| QA-E3-07 | Informativa | `Expand()` no arma la gracia; coincide con ADR-004 §14.2 y es alcance de E6 | **Confirmada**: la decisión de qué debe hacer el «expandir» de la bandeja se toma en E6 |
| QA-E3-08 | Informativa | ADR-004 §9.5 describe siete aserciones por ciclo y la sonda ejecuta seis más una final (19 en total) | **Aceptada**: la diferencia es de método de recuento, no de cobertura; la sonda verifica más, no menos |
| QA-E3-09 | Alta | Contradicción documental sobre quién asume el presupuesto de 42 MB: Q-08 lo asigna a E4/E5/E7/E8, mientras `ADR-003 §10` y `ADR-004 §10` lo exigen como medición de cierre de E3 | **Resuelta** como **Q-12** (opción 1: presupuesto recalibrado a 46 MB, RNF de 50 MB intacto como puerta dura). E4/E5/E7/E8 verifican contra 46 MB |
| QA-E3-10 | Baja | La sonda no ejercita la migración de esquema v1 → v2 | **Diferida como M-08** (verificación manual del informe de entrega de E8) |

### Observación de E6 detectada al redactar ADR-004 (§14.2)

`DockInteractionController.Expand()` cancela el temporizador pendiente y pasa a `Expanded`, pero **no arma la gracia**. Hoy no tiene consecuencias porque solo lo llaman las pruebas; en E6, el «expandir» del menú de la bandeja dejaría la barra desplegada **indefinidamente**, hasta que el puntero entrara y saliera de la barra al menos una vez. No es un defecto de E3: qué debe hacer ese comando se decide en E6 con el contrato delante.

### Verificaciones manuales que añade E3

| ID | Verificación | CA o deuda que cubre |
| --- | --- | --- |
| M-05 | *Hover* sostenido y retirada: el micro-efecto se percibe antes de que la barra esté completamente desplegada, **no se recorta contra la ventana** en los cuatro bordes y el *slide* entra desde el borde correcto. Incluye comprobar que los literales del XAML (60 y 0,16 s) coinciden con `DockBarMetrics` e `InteractionTimings` | E3-CA1, D-14 |
| M-06 | La pestaña conserva su posición en pantalla al desplegar y al colapsar, en los cuatro bordes (I3) | E3-CA1, E3-CA2 |
| M-07 | Aspecto de la pestaña y del contenido al 125 % y 150 % tras `PerMonitorV2` (amplía M-04) | AMP-03 |
| M-08 | Un `settings.json` real de E2 (versión 1) se migra a la versión 2 conservando `dockEdge` y añadiendo `gracePeriodMs` | E3-CA5, AMP-04 |
| M-09 | **Repetición**: cinco ciclos seguidos de *hover* y retirada. En cada uno la barra se despliega **visible**, colapsa y vuelve a desplegarse con el mismo aspecto | HU02 CA #2/#3; H-E3-05, H-E3-06 (ADR-004) |
| M-10 | **Reversión**: volver al panel durante el colapso lo reabre sin parpadeo; volver a la pestaña durante el colapso muestra la pestaña y, a los 200 ms, la barra se despliega de nuevo | ADR-004 D2, E3-CA3 |
| M-11 | **El *hit-test* de la ventana no cambia**: con la barra colapsada la ventana sigue midiendo 120×36 al 100 %, un clic dentro del rectángulo lo recibe la aplicación y un clic fuera llega a la ventana de debajo. Vigila que quitar el `Background` de `RootPanel` no altera lo que el sistema entrega | E1-CA4 como no-regresión; D-15 sigue abierta |

### Avisos resueltos sin cambio de código (N-01)

| ID | Aviso | Resolución |
| --- | --- | --- |
| N-01 | ADR-002 dejó anotado que la zona de arrastre (`WindowChrome.CaptionHeight` sobre la franja de la pestaña) debía reevaluarse cuando la barra se despliegue | **Resuelto como no-cambio documentado** (ADR-003 §3.3, opción a): la pestaña es el único asa en ambos estados. Ampliar la zona a la franja vacía no sobrevive a E5, y hacer asa de toda la barra impediría ejecutar los accesos directos por el `PreviewMouseLeftButtonDown` del contenedor. El arrastre además cancela los temporizadores y bloquea las transiciones automáticas mientras dura |

---

## Deuda y verificaciones abiertas de E4 (ADR-005 y Cintas)

Registradas al cerrar E4 con veredicto **APROBADA CON OBSERVACIONES** del 2026-11-14. **Ninguna incumple un criterio de aceptación**.

| ID | Severidad | Descripción | Tratamiento |
| --- | --- | --- | --- |
| OBS-01 | Menor (UX) | Renombrar desde `ContextMenu` en una Cinta cerrada activa `IsRenaming` sin mostrar el `TextBox` (que vive en `RibbonPanel`), y el `Popup` del `ContextMenu` dispara `LeaveRegion(Bar)` si el usuario tarda > 400 ms | Atender en E5/E8 (abrir la Cinta al iniciar `Rename` o mantener `Bar` activa mientras el menú está abierto) |
| OBS-02 | Menor (Visual) | En `BarSlotStyle` (`MainWindow.xaml`), cuando `HasOpenRibbon=False`, `ContentPanel` usa `Width/Height="Auto"` y se ajusta al contenido interno | Revisar en E5/E8 al ajustar el estilo visual con accesos directos |
| OBS-03 | Menor (UX) | `WpfRibbonDeletionConfirmer` llama a `MessageBox.Show` sin ventana propietaria mientras `MainWindow` tiene `Topmost="True"` | Pasar `Application.Current.MainWindow` como `owner` en E5/E8 |

| ID | Verificación manual | CA o ADR que cubre |
| --- | --- | --- |
| M-12 | En Release, arrastrar la barra a modo flotante (`104,104 120x36`), desplegar por *hover* y colapsar: comprobar que regresa exactamente al mismo rectángulo sin deriva | ADR-005, H-E3-07 |
| M-13 | En los cuatro bordes y en modo flotante, abrir una Cinta y verificar visualmente que la animación *slide* avanza perpendicularmente hacia el interior de la pantalla | E4-CA1 |
| M-14 | Crear 2 Cintas (`+`), alternar apertura exclusiva (acordeón), renombrar, reordenar, reiniciar y verificar persistencia en `settings.json` (v3) con todas las Cintas cerradas al arranque | E4-CA2, E4-CA4, Q-10 |
| M-15 | Arrastrar desde el Explorador un `.lnk` real (ej. `wsl.exe` o `cmd.exe` con argumentos) sobre la barra y sobre una Cinta; comprobar icono, ejecución con argumentos y rechazo de `.exe`/carpeta con alerta | E5-CA1, E5-CA2, E5-CA3 |
| M-16 | Reordenar accesos directos por drag & drop interno en barra y Cinta y verificar persistencia tras reiniciar | E5-CA5, E5-CA6 |
| M-17 | Abrir menú contextual de un acceso directo y comprobar visualmente `Move to Main Bar`, `Move to Ribbon` y `Remove` (sin edición) | E5-CA4, Q-06 |
| M-18 | Hacer clic izquierdo en el icono de bandeja y comprobar apertura/activación de `SettingsWindow`; clic derecho y probar `Settings` y `Exit` verificando que el icono desaparece sin quedar fantasma | E6-CA1, E6-CA2, ADR-006 |
| M-19 | Cambiar el temporizador de gracia en `SettingsWindow` (300–500 ms), comprobar efecto inmediato y persistencia tras reiniciar; cerrar `SettingsWindow` y verificar ejecución en segundo plano y fluidez visual con `RenderMode.SoftwareOnly` | E6-CA3, E6-CA5 |
| M-20 | Activar/desactivar `"Start automatically with Windows"` y verificar entrada `"StickyDock"` en `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | E6-CA4 |
| M-21 | En entorno físico con 2+ monitores (DPI mixto y barra secundaria en otro borde), activar réplica, verificar acople por monitor, sincronización espejada en vivo y desconexión/reconexión física | E7-CA1..E7-CA5, R-05 |

---

## Resumen para reporte

- **Etapas cerradas**: E0 (2026-10-22), E1 (2026-10-26), E2 (2026-10-28, `2aa510a`), E3 (2026-11-03, `98f3387`), E4 (2026-11-14, `5577dbc`), E5 (2026-11-14, `7aa22a4`), E6 (2026-11-14, `76a12e1`) y **E7 (2026-11-15)**.
- **Etapa cerrada — E7 (Multimonitor y réplica espejada) al 100 %**:
  - `Win32MonitorCatalog` (`EnumDisplayMonitors`/`GetMonitorInfoExW`/`GetDpiForMonitor` sin WinForms) y `TaskbarMonitor.GetTaskbarPlacementForMonitor`.
  - Migración v4→v5 (`AddMonitorReplicaMigration`) y selector en `SettingsViewModel`/`SettingsWindow` (solo réplica espejada, Q-04).
  - Sincronización espejada vía `IConfigurationDocument.Changed` en `DockBarViewModel` (`IDisposable`) y `MultiMonitorCoordinator` / `WpfBarHostFactory` para desconexión y reconexión.
- **Veredicto de `qa`: APROBADA CON OBSERVACIONES** — 6/6 criterios (`E7-CA1..E7-CA5`, `Q-04`) en **Cumple**, builds Release/Debug limpios, **477/477** pruebas en verde, `WinFormsModulesLoaded: 0`.
- **RNF medidos por `qa` en Release** (30 muestras a 1 s): CPU en reposo **0,0064 %** (< 1 %); memoria privada comprometida (`PrivateMemorySize64`) **47,02 MB** promedio (máx. 47,14 MB — cumple RNF-01 `< 50 MB`, supera en ~1,02 MB el presupuesto interno Q-12 `< 46 MB`, asignado a perfilado de E8).
- **Avance del plan**: **89 %** (8 de 9 etapas cerradas; 74 de 87 puntos estimados).
- **Próxima acción**: iniciar **E8 — Rendimiento, endurecimiento y entrega** (etapa final del plan).
