# ADR-003: Hover, despliegue, temporizadores y propiedad de la configuración

- **Estado**: **Aceptada**
- **Fecha**: 2026-10-28 (aceptada por el usuario el 2026-10-28, junto con la resolución de Q-09)
- **Etapa**: E3 (Hover, *slide-in* y temporizador de gracia)
- **Trazabilidad**: RF02 (§2, §4.2, §4.3), HU02 (CA #2, CA #3), §5.1-04 (detección de entrada de puntero), §5.2-01 (MVVM), RF03 (eje de la barra), RNF-01/RNF-02 (Q-08, A5), A4 (área interactiva), A6 (sin hook global de ratón), A7 (longitud mínima), Q-03 (temporizadores), E3-CA1..E3-CA5
- **Resuelve deuda registrada**: D-01 (tamaño medido de la barra), D-04 (persistencia *fire-and-forget*), D-07 (dos copias del documento de configuración), D-09 (el tamaño de la barra no se re-resuelve) y el aviso de ADR-002 sobre la zona de arrastre de la barra desplegada
- **No modifica**: ADR-001 (stack). Se apoya en él.
- **Sustituye parcialmente a**: ADR-002 (docking y geometría). Retira `IDockWindowHost.GetBarSize()` del contrato y cambia el constructor de `DockPlacementCoordinator`; todo lo demás del ADR-002 sigue vigente. Motivo: **D-01** — el tamaño de la barra se medía en vez de declararse, y solo se cierra sacando la medición del contrato. El alcance exacto de la sustitución y su efecto sobre la lista de verificación de ADR-002 están en §5.5.1.

> **Resumen en una frase**: E3 se resuelve con **una máquina de estados explícita** (`DockInteractionController`) que posee los temporizadores detrás de un programador de un solo disparo, con **tamaños declarados** (`DockBarMetrics`) en lugar de medidos, con **un único documento de configuración mutable** (`IConfigurationDocument`) como único escritor, con la **pestaña como única zona de arrastre** en ambos estados y con la **animación en la vista** (storyboards declarativos disparados por el estado), de modo que el reposo sigue costando cero y ningún criterio de E3 deja de ser comprobable sin abrir una ventana.

---

## 1. Contexto

### 1.1 Lo que la etapa exige

`docs/plan/PLAN.md`, etapa E3, fija el objetivo y cinco criterios de aceptación; los cuatro primeros derivan de RF02 y HU02, el quinto de Q-03:

| CA | Exigencia literal | Origen |
| --- | --- | --- |
| E3-CA1 | Con el cursor sobre la pestaña durante ~200 ms, la barra emite un micro-efecto de expansión y se despliega con animación *slide-in* | HU02 CA #2, §4.3-01, §4.3-02 |
| E3-CA2 | Al salir el cursor del área interactiva, la barra colapsa tras el temporizador de gracia, 400 ms por defecto dentro del rango 300–500 ms | HU02 CA #3, §4.3-03, Q-03 |
| E3-CA3 | Moverse entre la barra desplegada y sus Cintas no provoca un colapso accidental (*grace period* cálido) | §4.3-03, A4 |
| E3-CA4 | El temporizador de activación se cancela si el cursor se retira antes de los 200 ms | §4.3-02, HU02 CA #2 |
| E3-CA5 | El tiempo de gracia es configurable en 300–500 ms, 400 por defecto, y **persiste entre ejecuciones** | Q-03, AMP-01 |

Además, tres reglas del §4 no negociables: la pestaña invierte el tono según el tema (§4.2-02/03, ya cumplido en E1), el despliegue es una animación de deslizamiento desde el borde (§4.3-01) y el ocultamiento es *por temporizador*, no por política fija (§4.3-03).

Y dos restricciones transversales que E3 es el primero en poner a prueba: **RNF-02** (CPU < 1 % de un núcleo en reposo) y **RNF-01** según Q-08 (memoria privada comprometida, presupuesto interno de 42 MB). Una máquina de estados con temporizadores mal gestionados es la forma más barata de romper ambas.

### 1.2 Estado real del código (hechos verificados, no informes)

E3 no empieza sobre terreno neutro. Estos son los hechos que condicionan el diseño:

1. **El estado actual es de dos valores y no tiene dueño de política.** `DockState` solo declara `Tab` y `Expanded` (`src\StickyDock.Core\Services\DockState.cs:6-13`) y `DockStateService` los cambia a petición de quien llame (`src\StickyDock.Core\Services\DockStateService.cs:38-47`). No hay temporizadores, ni zona interactiva, ni noción de "armado".
2. **El *hover* está cableado en el code-behind y solo escala.** `MainWindow.xaml.cs:21-42` suscribe `MouseEnter`/`MouseLeave` de **toda la ventana** y escribe `viewModel.IsHovered`; el *setter* de `IsHovered` (`DockBarViewModel.cs:89-110`) fija `ScaleX/ScaleY = 1.08` inmediatamente, sin retardo, sin temporizador y sin desplegar nada.
3. **El tamaño de la barra se mide, no se declara.** `IDockWindowHost.GetBarSize()` (`IDockWindowHost.cs:16`) devuelve `ActualWidth/ActualHeight` con respaldo en `Width/Height` (`WpfDockWindowHost.cs:32-39`) y el coordinador lo consume en cada colocación (`DockPlacementCoordinator.cs:234`). Es el defecto D-01: un `SetWindowPos` externo deja el tamaño intruso en el siguiente cálculo.
4. **La configuración la escriben dos objetos distintos.** `Bootstrapper` registra el documento como singleton (`Bootstrapper.cs:62-63`) pero pasa a `DockPlacementCoordinator` **otra** referencia al mismo registro inmutable más el almacén (`Bootstrapper.cs:166-172`), y el coordinador guarda su propia copia y persiste sobre ella (`DockPlacementCoordinator.cs:325-334`). Es D-07: en cuanto E3 escriba la gracia y E6 otras preferencias, el último escritor pisa a los demás.
5. **La persistencia es *fire-and-forget*.** `PersistEdge` ejecuta `_ = SaveAsync(...)` (`DockPlacementCoordinator.cs:333`) sin esperar, sin orden garantizado y sin posibilidad de *flush* al cerrar. Es D-04.
6. **El esquema persistido es la versión 1 y no conoce la gracia.** `StickyDockConfiguration.CurrentSchemaVersion = 1` (`StickyDockConfiguration.cs:18`) y el propio comentario del tipo anuncia que E3 añade una sección con su migración (`:9-11`). El almacén ya soporta la cadena de migraciones (`JsonConfigurationStore.cs:199-222`), así que no hay que escribir maquinaria nueva, solo registrarla.
7. **El tamaño de la barra no se re-resuelve al cambiar por contenido.** El coordinador solo recalcula al arrancar y ante eventos del entorno (`DockPlacementCoordinator.cs:171-201`). Es D-09, y E3 es el primer caso real: la barra cambia de tamaño sin que llegue `WM_EXITSIZEMOVE` ni un cambio de área de trabajo.
8. **La ventana entera es área de cliente, con `CaptionHeight = 0`** (`MainWindow.xaml:22`), y el arrastre cuelga de un `PreviewMouseLeftButtonDown` que marca el evento como manejado (`PressDragBehavior.cs:70-86`, `:84`) sobre el `Border` de la pestaña (`MainWindow.xaml:44-47`). ADR-002 dejó anotado que esa zona debe reevaluarse cuando la barra se despliegue; el encargo del Director lo etiqueta como **N-01**. *Nota de exactitud*: N-01 **no consta** en `docs/plan/ESTADO.md`; se cita como aviso del encargo y queda pendiente de registro por el Director.

### 1.3 Los cinco problemas que E3 debe resolver antes de escribir una línea

1. **¿Quién posee el documento de configuración?** (D-07). Sin una respuesta, E3 introduce el segundo escritor y la preferencia del borde puede revertirse sola.
2. **¿Cómo crece la ventana al desplegarse, con qué fórmulas, quién avisa al coordinador y cómo se evita el bucle de realimentación?** (D-09, D-01, A7).
3. **¿Cuál es la zona de arrastre de la barra desplegada?** (aviso de ADR-002, N-01) y qué ocurre con el `e.Handled = true` existente.
4. **¿Cuál es la máquina de estados y la política exacta de los temporizadores**, incluidos reentradas, cancelaciones y preparación para E4?
5. **¿Cómo se reparte la animación entre ViewModel y vista** sin violar §5.2-01 ni encarecer el reposo?

## 2. Drivers

Ordenados por peso. Los tres primeros son requisito; los tres últimos son ingeniería y se marcan como tales donde corresponde.

1. **Observabilidad de los cinco CA sin abrir una ventana.** HU02 CA #2 y CA #3 son criterios de comportamiento temporal; si la política vive en el code-behind o en un `DispatcherTimer` disperso, no hay forma de escribirlos como aserciones. Es el mismo criterio que decidió ADR-002 y la razón por la que E2 se reabrió.
2. **RNF-02 y RNF-01/Q-08.** En reposo no debe existir **ningún** temporizador vivo, ningún *storyboard* corriendo y ningún *hook* de ratón. Un `DispatcherTimer` permanente o un `SetWindowsHookEx(WH_MOUSE_LL)` incumplen el RNF por diseño, no por despiste.
3. **§5.2-01 (MVVM) y E0-CA6.** Lógica de estado, temporizadores y animaciones en ViewModels y servicios; el code-behind no la contiene. La animación es la parte que más fácilmente se escapa a la vista con lógica dentro; este ADR fija la frontera.
4. **A6: la detección de puntero es por área de ventana, no global.** §5.1-04 sugiere "detección de entrada de puntero global"; A6 ya la interpretó como detección por área para no comprometer RNF-02 y encarga al arquitecto confirmarlo en E3. Este ADR lo confirma (§4.4).
5. **Un solo escritor por preferencia (D-07, D-04).** El estado de E3 añade una segunda preferencia persistida; sin un dueño único, el diseño queda estructuralmente roto para E6 y E7.
6. **Que E4 y E6 se apoyen sin reescribir.** E4 añade Cintas (una cuarta región interactiva y un despliegue perpendicular); E6 añade la ventana de configuración de la gracia y el arranque automático. La máquina de estados y el documento deben admitirlos como extensión, no como cirugía.
7. **(Ingeniería)** Estabilidad visual: crecer y encogerse sin que la barra "salte" ni se recorte, y que la pestaña —único ancla visual y único asa de arrastre— no se mueva al desplegar.

---

## 3. Opciones consideradas

### 3.1 P1 — Propiedad del documento de configuración (D-07)

#### Opción A — Mantener el registro inmutable y añadir un servicio de escritura

- **Descripción**: `StickyDockConfiguration` sigue siendo el documento que circula; un `IConfigurationWriter` con métodos `SaveDockEdgeAsync`, `SaveGracePeriodAsync`, … persiste cada preferencia por separado.
- **Ventajas**: cambio mínimo; los consumidores actuales no se tocan; no hace falta migrar firmas.
- **Desventajas / riesgos**: el registro es inmutable, así que cada escritor debe releer el documento para no pisar las demás preferencias; `IConfigurationStore.SaveAsync` sigue siendo alcanzable desde cualquier sitio, que es exactamente la causa de D-07; y una API con un método por preferencia crece sin control (E4, E5, E6, E7 aportan al menos seis más).
- **Coste de adopción**: bajo, con deuda estructural.

#### Opción B — Un documento mutable compartido (`IConfigurationDocument`) como único escritor (ELEGIDA)

- **Descripción**: Core expone `IConfigurationDocument` con `Current`, `Changed`, `Update(Func<StickyDockConfiguration, StickyDockConfiguration>)` y `FlushAsync()`. Todos los interesados (coordinador de docking, controlador de interacción, preferencias de E6) reciben **la misma instancia** y solo pueden cambiar el documento pidiendo una transformación. El almacén (`IConfigurationStore`) queda como puerto de E/S puro cuyo único llamador es el documento.
- **Ventajas**:
  - Un solo escritor, por construcción: `SaveAsync` deja de ser alcanzable desde la lógica de negocio.
  - El documento mantiene la forma de registro inmutable por dentro (`with`), así que la serialización, la comparación por valor y los `init` no cambian.
  - Obliga a resolver D-04 en el mismo movimiento: el documento serializa sus escrituras y ofrece `FlushAsync` para el cierre ordenado.
  - E4/E5/E6/E7 no añaden plomería: piden `Update(...)` y reciben `Changed`.
- **Desventajas / riesgos**: `Update` es un punto único que hay que mantener disciplinado (nada de `Current` mutable por referencia); hay que decidir la política de *coalescing* para no escribir un archivo por cada cambio; y hay que fijar la afinidad de hilo (se decide: el documento se usa desde el hilo de UI; `FlushAsync` puede esperarse desde cualquier hilo).
- **Coste de adopción**: medio (3 archivos nuevos en Core, 2 firmas de constructor tocadas).

#### Opción C — Servicio de configuración con estado propio, sin registro

- **Descripción**: sustituir el registro por una clase mutable con propiedades y eventos por propiedad (`GracePeriodChanged`, `DockEdgeChanged`, …).
- **Ventajas**: notificaciones finas; lectura directa.
- **Desventajas / riesgos**: **no es viable tal como está planteada**, porque `StickyDockConfiguration` es un `record` inmutable que se serializa por valor y con el que ya trabajan E0, E1 y E2; convertirlo en clase mutable obliga a reescribir la comparación de cambios, el volcado de configuración y las pruebas de persistencia existentes. Además multiplica los eventos y con ellos las oportunidades de realimentación.
- **Coste de adopción**: alto y con rotura de lo ya cerrado.

**Decisión: Opción B.** El argumento que decide la balanza es que D-07 no es un defecto de sincronización, es un defecto de **propiedad**: mientras `SaveAsync` sea público y el documento circule por valor, cada etapa nueva añade un escritor y el diseño empeora. La opción B lo cierra en un solo punto y de paso resuelve D-04.

### 3.2 P2 — Tamaño de la barra al desplegarse (D-09, D-01, A7)

#### Opción A — Seguir midiendo y avisar al coordinador tras el layout

- **Descripción**: mantener `GetBarSize()`, exponer un método `NotifyBarSizeChanged()` que la vista llama desde `SizeChanged`, y dejar que el coordinador lea `ActualWidth/ActualHeight`.
- **Ventajas**: no hay que declarar tamaños; el layout real manda.
- **Desventajas / riesgos**: **D-01 no se cierra, se agrava**: `SizeChanged` se dispara también cuando el tamaño lo impone `SetWindowPos`, así que el aviso llega por el camino de la propia escritura y se realimenta; y existe un salto visible de un fotograma entre el cambio de estado y el layout. Además deja una decisión de geometría dependiente de WPF, en contra de ADR-002.
- **Coste de adopción**: bajo, con el bucle de realimentación como regalo.

#### Opción B — Tamaños declarados por estado y orientación (ELEGIDA)

- **Descripción**: `DockBarMetrics` declara cuatro números —largo y grosor en estado plegado y desplegado— en unidades independientes del dispositivo. `IDockBarSizeSource.GetCurrentSize(anchor)` devuelve el `DockBarSize` que corresponde al estado vigente y a la orientación del borde. El coordinador deja de leer el tamaño medido: `IDockWindowHost.GetBarSize()` **se elimina**.
- **Ventajas**:
  - Cierra D-01 de raíz: no hay nada que medir, así que un `SetWindowPos` externo no puede contaminar el cálculo.
  - La geometría vuelve a ser pura y comprobable: `DockBarMetrics.GetSize(...)` es una función sin estado.
  - El tamaño deja de depender de WPF, así que E7 puede instanciar una barra por monitor sin más.
  - El crecimiento es una decisión única (`DockBarMetrics`), ajustable en un punto.
- **Desventajas / riesgos**: los números hay que mantenerlos coherentes con el layout de la vista (si el contenido pide más, se recorta). Se mitiga con un recorte explícito (`Min` contra el área de trabajo, ya existente en `GetDockedBounds`) y con una verificación manual de aspecto.
- **Coste de adopción**: medio (2 archivos nuevos en Core, 1 borrado en la App, ajuste del *fixture* de pruebas).

#### Opción C — Autosize del contenido y desplazamiento del borde

- **Descripción**: dejar que WPF mida y el coordinador acepte el tamaño resultante tras cada transición, con una bandera de reentrada.
- **Ventajas**: el tamaño siempre cabe.
- **Desventajas / riesgos**: el resultado depende del orden de los eventos y del *layout* (que es asíncrono respecto de la transición de estado), lo que hace el comportamiento no determinista ni probable. Descartada.
- **Coste de adopción**: bajo en apariencia, alto en estabilidad.

**Decisión: Opción B**, con las fórmulas del §6 y la regla de idempotencia R1–R5 del §6.6.

#### Sub-decisión P2.b — ¿Quién avisa al coordinador?

Tres candidatos: (i) el controlador de interacción conoce el docking —descartado, dos políticas en un objeto—; (ii) el ViewModel avisa —descartado, mete docking en la capa de presentación y lo duplica en E7—; (iii) **la raíz de composición** suscribe `IDockStateService.StateChanged` y llama a `DockPlacementCoordinator.NotifyBarSizeChanged()` — **elegida**. Es un cableado de una línea en `Bootstrapper`, donde ya vive el grafo de docking, y ninguno de los dos objetos conoce al otro.

### 3.3 P3 — Zona de arrastre de la barra desplegada (aviso de ADR-002, N-01)

#### Opción (a) — La pestaña es el único asa, en ambos estados (ELEGIDA)

- **Descripción**: `PressDragBehavior` sigue colgado del `Border` de la pestaña y de ningún otro elemento. La pestaña permanece visible y quieta en el borde cuando la barra se despliega, así que el asa nunca desaparece. `e.Handled = true` se mantiene.
- **Ventajas**: cero código nuevo; no roba gestos a los accesos directos de E5 (clic para ejecutar, clic derecho para el menú, arrastre interno para reordenar); el asa es estable y predecible; el estado no cambia la zona.
- **Desventajas / riesgos**: el asa es pequeña (la pestaña); con la barra muy desplegada hay que apuntar al borde. Aceptable: es el gesto que el usuario ya aprendió en E2 y el que HU01 describe ("arrastrar la barra").
- **Coste de adopción**: nulo. **Resuelve el aviso de ADR-002 y N-01 como no-cambio documentado.**

#### Opción (b) — Zona de arrastre ampliada a la franja vacía de la barra desplegada

- **Descripción**: añadir un `Border` de "asa" en la franja de la barra que no ocupa ningún elemento.
- **Ventajas**: más superficie para agarrar.
- **Desventajas / riesgos**: con E5 cargada de accesos directos, esa franja casi no existe; y una zona de arrastre que cambia de tamaño según el contenido es una fuente de gestos perdidos. Reevaluable si el usuario se queja de la diana.
- **Coste de adopción**: bajo.

#### Opción (c) — Toda la barra es asa cuando está desplegada

- **Descripción**: `PressDragBehavior` sobre el contenedor raíz en estado `Expanded`.
- **Ventajas**: máxima superficie.
- **Desventajas / riesgos**: **incompatible con E5**: el `PreviewMouseLeftButtonDown` del contenedor se ejecuta antes de que el elemento hijo reciba el clic y marca el evento como manejado, de modo que hacer clic en un acceso directo arrastraría la barra en lugar de ejecutarlo. Descartada.
- **Coste de adopción**: nulo, y bloquearía E5.

**Decisión: Opción (a).** Además, el arrastre **suprime el despliegue**: `BeginDrag` cancela los temporizadores y bloquea las transiciones automáticas, y `EndDrag` reevalúa regiones. Sin esto, mantener pulsado sobre la pestaña más de 200 ms dispararía el despliegue en mitad del movimiento nativo.

### 3.4 P4 — Máquina de estados y temporizadores

#### Opción A — Un `DispatcherTimer` por temporizador dentro del ViewModel

- **Descripción**: el `DockBarViewModel` crea dos o tres `DispatcherTimer` y contiene la política.
- **Ventajas**: pocos tipos; todo visible en un archivo.
- **Desventajas / riesgos**: el ViewModel queda atado a WPF (deja de poder probarse sin *dispatcher*), la política se mezcla con la proyección de estado que consumen los *bindings*, y cada temporizador añade una fuente de fugas si no se dispone. Es justo el patrón que E0-CA6 prohíbe en la práctica.
- **Coste de adopción**: bajo, con deuda.

#### Opción B — Controlador único en Core + programador de un solo disparo (ELEGIDA)

- **Descripción**: `DockInteractionController` en `StickyDock.Core.Interaction` posee el estado, las regiones activas y **una** ranura de temporizador detrás de `ISingleShotScheduler` (`Schedule`/`Cancel`/`IsPending`). La implementación real es un `DispatcherTimer` en la App; en pruebas, un `ManualScheduler` que avanza a mano. El controlador implementa `IDockStateService`, así que el ViewModel no cambia de dependencia.
- **Ventajas**:
  - Los cinco CA se prueban con el programador manual: "entra el puntero, avanzan 199 ms, no pasa nada; avanzan 2 ms más, se despliega" es una aserción, no una espera.
  - Una sola ranura de temporizador hace **imposible** el solapamiento de temporizadores, que es la fuente clásica de estados inconsistentes.
  - El coste en reposo es cero: si no hay temporizador pendiente, no hay nada armado.
  - E4 añade una región (`Ribbon`) al mismo enum y no toca la política.
- **Desventajas / riesgos**: hay que documentar la afinidad de hilo (el programador se crea en el hilo de UI y los `callback` llegan ahí; el controlador no toma cerrojos porque es de un solo hilo, y eso hay que declararlo, no suponerlo).
- **Coste de adopción**: medio (5 archivos nuevos en Core, 2 en la App).

#### Opción C — `async`/`await` con `Task.Delay` y `CancellationTokenSource`

- **Descripción**: la política se escribe como métodos asíncronos con cancelación.
- **Ventajas**: sintaxis directa.
- **Desventajas / riesgos**: reintroduce el problema de la reentrada (varias continuaciones vivas compitiendo), exige sincronizar al hilo de UI con `Dispatcher` en cada continuación, y en pruebas obliga a esperar tiempo real o a inyectar relojes asíncronos. La ranura única del programador da la misma garantía con menos superficie.
- **Coste de adopción**: medio.

**Decisión: Opción B.**

#### Sub-decisión P4.b — Cómo llegan las regiones al controlador (H-1)

`EnterRegion` y `LeaveRegion` no existen en `IDockStateService`: el contrato vigente declara exactamente cuatro miembros (`State`, `StateChanged`, `CollapseToTab`, `Expand`; `src\StickyDock.Core\Services\IDockStateService.cs:8-21`). Sin resolver esto, `EnterRegionCommand` y `LeaveRegionCommand` del ViewModel no se pueden implementar sin un *cast* al tipo concreto, que es justo lo que este ADR prohíbe.

##### Opción (a) — Ampliar `IDockStateService` con las dos operaciones

- **Descripción**: `IDockStateService` gana `EnterRegion(DockRegion)` y `LeaveRegion(DockRegion)`.
- **Ventajas**: una sola dependencia en el ViewModel y una sola abstracción en el contenedor.
- **Desventajas / riesgos**: ensancha el contrato de **estado** con **entrada de puntero**; obliga a implementar dos métodos que no le corresponden a todo consumidor del estado (el menú de la bandeja de E6, `DockBarSizeSource` y cualquier doble de prueba) y rompe una interfaz que ya usan E1 y E2 sin necesidad. Un doble de `IDockStateService` dejaría de ser el doble de un estado.
- **Coste de adopción**: bajo, con deuda de contrato.

##### Opción (b) — Un puerto propio para las regiones (ELEGIDA)

- **Descripción**: `IDockRegionSink` en `StickyDock.Core.Interaction` declara `EnterRegion`/`LeaveRegion`; `DockInteractionController` lo implementa **junto con** `IDockStateService`; `DockBarViewModel` recibe ambos.
- **Ventajas**:
  - Cada contrato dice una sola cosa: el estado se **consulta**, la entrada de puntero se **notifica**. Es la misma separación que D1 hizo entre documento y almacén.
  - `IDockStateService` conserva sus cuatro miembros: E1, E2 y E6 no se tocan, y los dobles de estado siguen implementando cuatro miembros.
  - El controlador sigue siendo el único dueño de la política; el puerto no tiene implementación propia, son dos líneas que reenvían.
- **Desventajas / riesgos**: una dependencia más en el constructor del ViewModel y dos líneas más en la composición. Se aceptan: son explícitas y no ocultan un *cast*.
- **Coste de adopción**: bajo (una interfaz de dos métodos, sin implementación nueva).

##### Opción (c) — `ICommand` publicados por el controlador

- **Descripción**: el controlador expone `ICommand EnterRegionCommand` (con el `DockRegion` como parámetro) y el ViewModel los reenvía.
- **Ventajas**: el ViewModel casi no cambia.
- **Desventajas / riesgos**: mete `System.Windows.Input.ICommand` en el contrato de dominio de Core y ata la interfaz pública de un servicio a la clase de comandos de WPF. E0 evitó deliberadamente esa dependencia poniendo `ICommand` solo en los ViewModels. Descartada.
- **Coste de adopción**: bajo, con acoplamiento.

**Decisión: Opción (b).** El criterio es el de siempre en este ADR: una interfaz, una responsabilidad. Entrar en una región no es un estado de la barra, es un hecho del puntero; fundirlos obligaría a cambiar el contrato de estado cada vez que E4 añada una región.

`BeginDrag`, `EndDrag`, `IsDragging` y `ActiveRegions` **no** entran en ninguna interfaz: su único llamador es la raíz de composición, que construye el tipo concreto para cablear `DragStarted`/`DragCompleted` del coordinador (§5.3, §10). Solo se publican como puerto las dos operaciones que la vista necesita.

### 3.5 P5 — Reparto de la animación (§5.2-01, RNF-02)

#### Opción A — Animación declarativa en la vista, disparada por el estado (ELEGIDA)

- **Descripción**: el ViewModel expone `DockState`; la vista tiene *storyboards* declarativos disparados por `DataTrigger`/`MultiDataTrigger` sobre un `TranslateTransform` del panel de contenido, con `FillBehavior` explícito y el recorte en un contenedor dedicado. La pestaña queda **estática** y por encima. Los valores concretos (desplazamiento y duración) se fijan en la sub-decisión P5.b.
- **Ventajas**: no hay código por fotograma en ninguna capa; la animación es propiedad del renderizador de WPF (barata, acelerada por composición cuando puede); sin *storyboard* activo no hay coste; la dirección del deslizamiento es una tabla de cuatro entradas declarada en §8.3, sin cálculo ni *binding* en tiempo de ejecución.
- **Desventajas / riesgos**: el mecanismo (storyboard, propiedades de animación) no es comprobable por prueba unitaria; se verifica por inspección y con la medición de reposo, igual que el resto de la capa de vista desde E0.
- **Coste de adopción**: bajo.

#### Opción B — El ViewModel escribe `TranslateTransform.X/Y` fotograma a fotograma

- **Descripción**: un temporizador de ~16 ms actualiza las propiedades enlazadas.
- **Ventajas**: control total de la curva.
- **Desventajas / riesgos**: introduce un temporizador de 60 Hz en el ViewModel durante cada transición (RNF-02 en el peor momento), obliga a notificar propiedades 20 veces por animación y hace que la política dependa del reloj. Descartada.
- **Coste de adopción**: alto en CPU y en ruido de notificaciones.

#### Opción C — Animación en el code-behind

- **Descripción**: `MainWindow.xaml.cs` construye y arranca las animaciones.
- **Ventajas**: ninguna frente a A.
- **Desventajas / riesgos**: viola §5.2-01 y E0-CA6, y es exactamente el tipo de código que H-E2-03 tuvo que borrar.
- **Coste de adopción**: nulo, y ya se pagó una vez.

**Decisión: Opción A.**

#### Sub-decisión P5.b — Cómo se expresan los valores de la animación (H-4)

##### Opción (a) — `Storyboard` en el `Style` con `From`/`Duration` por `Binding`

- **Descripción**: lo que decía el borrador anterior de este ADR (`From="{Binding SlideFromX}"`, `Duration="{Binding SlideMilliseconds}"`).
- **Ventajas**: una sola fuente de verdad para el vector y la duración, calculada en Core.
- **Desventajas / riesgos**: **no es viable, y no por el sellado del `Style`**. `Timeline.From` y `Timeline.Duration` **no son propiedades de dependencia**, así que no admiten `Binding` en ninguna circunstancia, ni dentro ni fuera de un estilo; además el árbol de animaciones de un `Style` se congela al sellarse, de modo que cualquier *binding* que sobreviviera lanzaría una excepción en tiempo de ejecución que ni el compilador ni las pruebas verían. Es exactamente el defecto que se corrige.
- **Coste de adopción**: nulo en apariencia, y una etapa bloqueada en la práctica.

##### Opción (b) — Valores literales por borde en el XAML (ELEGIDA)

- **Descripción**: cada borde tiene su bloque de animación con `From`/`To` y `Duration` literales; el disparo es un `MultiDataTrigger` con dos condiciones (`DockState` y `DockEdge`), y el elemento animado es el propio elemento estilizado, de modo que no hace falta `Storyboard.TargetName` ni un espacio de nombres concreto.
- **Ventajas**:
  - Ejecutable tal cual, sin *selector* ni código: `MultiDataTrigger` admite la conjunción que un `DataTrigger` simple no puede expresar.
  - Cero *binding* en propiedades de animación, que es la única forma en que WPF las acepta.
  - El elemento animado es el estilizado, así que desaparece el problema de resolución de `TargetName` entre el espacio de nombres del `Style` y el de la ventana.
  - Sin código por fotograma y sin *storyboard* permanente: terminada la animación, el *clock* se detiene.
- **Desventajas / riesgos**: los literales duplican dos números de Core —el desplazamiento (60 = `ExpandedThickness − CollapsedThickness`) y la duración (0.16 s = `InteractionTimings.SlideMilliseconds`)— y ningún compilador comprueba la coherencia. Se declara como deuda D-14 y se verifica con la inspección de §10 y con la comprobación manual M-05.
- **Coste de adopción**: bajo (cinco bloques de animación y una tabla de cuatro literales).

##### Opción (c) — Construir el `Storyboard` en un *behavior* adjunto de la App

- **Descripción**: un comportamiento lee `SlideFromX/Y` del ViewModel y arranca un `Storyboard` construido en código.
- **Ventajas**: mantiene una sola fuente de verdad y permite valores calculados.
- **Desventajas / riesgos**: traslada la animación a código (decenas de líneas de configuración de *timelines*) y la deja fuera del alcance de una prueba unitaria igual que el XAML, pero con más superficie donde equivocarse; y obliga a que el *behavior* observe el estado, duplicando lo que un `DataTrigger` hace sin código. Se descarta: no compra nada verificable.
- **Coste de adopción**: medio.

**Decisión: Opción (b).** La razón es de una frase: `Timeline.From` y `Timeline.Duration` no son propiedades de dependencia, así que la animación **no puede** recibir sus valores por *binding* y forzosamente han de ser literales; el único diseño que sigue siendo declarativo con literales es el disparo por `MultiDataTrigger` sobre el elemento estilizado. La consecuencia se asume explícitamente: el ViewModel deja de publicar el vector de deslizamiento y su duración, y esa geometría pasa a ser una tabla de cuatro filas documentada en §8.3 en lugar de una función de Core.

---

## 4. Decisión

Se adoptan las opciones marcadas como ELEGIDAS en §3, que se concretan en siete decisiones y una migración de esquema.

### D1 — Un único documento de configuración mutable y un solo escritor

`StickyDock.Core.Configuration.IConfigurationDocument` es la **única** vía de cambio de preferencias. El coordinador de docking deja de recibir el registro y el almacén por separado y recibe el documento. `IConfigurationStore` no se modifica: sigue siendo el puerto de E/S y su único llamador pasa a ser `ConfigurationDocument`.

Motivo: cierra D-07 y D-04 en un punto, y es la forma que E6 y E7 necesitan para no añadir un tercer y un cuarto escritor.

### D2 — Tamaños declarados, no medidos

`DockBarMetrics` declara `(CollapsedLength, CollapsedThickness, ExpandedLength, ExpandedThickness)`; `IDockBarSizeSource.GetCurrentSize(anchor)` devuelve el tamaño del estado vigente en la orientación del borde. `IDockWindowHost.GetBarSize()` **se elimina** y con él D-01. El coordinador gana `NotifyBarSizeChanged()`, que la raíz de composición invoca al cambiar `IDockStateService.StateChanged`.

Motivo: D-09 exige que el tamaño se re-resuelva cuando cambia por contenido, y D-01 exige que ese tamaño no se mida. Declararlo resuelve ambos y deja la geometría pura. La huella plegada es **exactamente la de la ventana de E1** (120×36): E1-CA4 se cumple porque la ventana mide lo que la pestaña, no porque exista una franja inerte que absorba el sobrante, y el producto no tiene mecanismo de *hit-test* selectivo (deuda D-15).

### D3 — La pestaña es el único asa de arrastre, en ambos estados

`PressDragBehavior` no se mueve ni se replica. El arrastre cancela los temporizadores y bloquea las transiciones automáticas mientras dura; al terminar, se reevalúan las regiones. N-01 y el aviso de ADR-002 se resuelven como **no-cambio documentado**, con la condición de revisión anotada en §14.

Motivo: es la única opción compatible con los gestos que E5 necesita sobre los accesos directos.

### D4 — Una máquina de estados explícita con una sola ranura de temporizador

`DockState` pasa a cuatro valores (`Tab`, `Arming`, `Expanded`, `Collapsing`), todos en `StickyDock.Core.Services` (el enum no cambia de espacio de nombres para no romper consumidores). El periodo de gracia **no** es un estado: ocurre dentro de `Expanded` con un temporizador pendiente, de modo que `Collapsing` significa siempre "el fundido de salida está corriendo". `DockInteractionController` implementa `IDockStateService` e `IDockRegionSink` (D7) y sustituye a `DockStateService`.

Motivo: menos estados que mantener, y cada estado se corresponde con algo visible (una pestaña, un armado, una barra desplegada, una barra deslizándose hacia fuera), lo que hace la tabla de transiciones verificable de un vistazo.

### D5 — La animación es declarativa y vive entera en la vista

El ViewModel publica solo `DockState` (más las dos operaciones de región que se le reenvían, D7). La vista traduce ese estado en animaciones declarativas: `MultiDataTrigger` sobre `DockState` y `DockEdge` con valores **literales** por borde, porque `Timeline.From` y `Timeline.Duration` no son propiedades de dependencia y no admiten *binding* (P5.b). El recorte del contenido que se desliza vive en un contenedor dedicado y la pestaña queda **fuera** de él (§8.2).

Motivo: §5.2-01 pide que la lógica no esté en la vista y RNF-02 pide que no haya trabajo periódico; ambas cosas se cumplen con animaciones declarativas, sin cálculo por fotograma y sin un solo *binding* dentro de una animación. El precio —que la dirección del deslizamiento deja de ser una función de Core y pasa a ser una tabla de cuatro filas en el XAML— se declara como deuda D-14 y se verifica con M-05.

### D6 — El esquema de persistencia sube a la versión 2

`StickyDockConfiguration.CurrentSchemaVersion = 2`, con `gracePeriodMs` (entero, por defecto 400) y una migración `AddGracePeriodMigration` (1 → 2) que añade la propiedad al árbol JSON. Se añade `StickyDockSettings.CreateStore(...)` como fábrica única del almacén con sus migraciones registradas, para que la lista no se duplique entre la aplicación y las pruebas.

Motivo: E3-CA5 exige persistencia del valor; el almacén ya sabe migrar (`JsonConfigurationStore.cs:199-222`) y no admitir la migración dejaría ilegibles los `settings.json` de E0–E2, que están en la versión 1.

### D7 — Las regiones entran por un puerto propio y no ensanchan el contrato de estado

`IDockRegionSink` (`StickyDock.Core.Interaction`) declara `EnterRegion(DockRegion)` y `LeaveRegion(DockRegion)`. `DockInteractionController` implementa **las dos** interfaces (`IDockStateService` e `IDockRegionSink`) y el `DockBarViewModel` recibe ambas dependencias. `IDockStateService` **no cambia**: sigue con sus cuatro miembros.

Motivo: el estado se consulta y la entrada de puntero se notifica; son dos responsabilidades y merecen dos contratos. Es lo que permite que `DockBarSizeSource`, el menú de E6 y los dobles de prueba del estado sigan implementando cuatro miembros, y lo que evita el *cast* al tipo concreto que el borrador anterior exigía sin decirlo (H-1).

### Interpretación registrada sobre el instante del micro-efecto

HU02 CA #2 dice "emite un micro-efecto de expansión **y se despliega**", y §4.3-02 dice que el micro-efecto ocurre "durante el *hover* sostenido (~200 ms) … **antes de desplegar completamente** la barra". Este ADR lo interpreta así: a los 200 ms **arrancan a la vez** el micro-efecto (escala de la pestaña, ~120 ms) y el *slide-in*; la barra no está *completamente* desplegada cuando el micro-efecto se emite, que es lo que el texto exige. La lectura estricta —micro-efecto, luego espera, luego despliegue— añadiría ~120 ms muertos al gesto y se aparta de §4.3-01 ("animación fluida"). Si el usuario prefiere la lectura estricta, es un cambio de una línea en la tabla de transiciones (retrasar `Expanded` un tiempo de micro-efecto con el mismo programador) y no afecta a ningún contrato.

---

## 5. Contratos concretos

Nombres definitivos, en inglés. Los contratos de ADR-002 que no se citan aquí **no cambian**.

### 5.1 Documento de configuración (D1)

```csharp
namespace StickyDock.Core.Configuration;

/// <summary>Arguments of <see cref="IConfigurationDocument.Changed"/>.</summary>
/// <param name="Document">Document as it is after the change.</param>
public sealed record ConfigurationChangedEventArgs(StickyDockConfiguration Document);

/// <summary>
/// The one mutable configuration of the process (D-07). Every consumer holds the same instance, and the only way to
/// change it is to ask for a transformation, so that no writer can overwrite the preferences of another one.
/// </summary>
/// <remarks>
/// The document keeps the immutable record shape: <see cref="Update"/> produces a new value and replaces the current
/// one. Use it from the UI thread; <see cref="FlushAsync"/> may be awaited from any thread.
/// </remarks>
public interface IConfigurationDocument
{
    /// <summary>Gets the current document. The instance is replaced on every change.</summary>
    StickyDockConfiguration Current { get; }

    /// <summary>Raised after a change, with the document that is current at that moment.</summary>
    event EventHandler<ConfigurationChangedEventArgs>? Changed;

    /// <summary>
    /// Replaces the document with the result of <paramref name="change"/> and schedules a write.
    /// </summary>
    /// <returns><see langword="false"/> when the transformation produced an equal document (no write, no event).</returns>
    bool Update(Func<StickyDockConfiguration, StickyDockConfiguration> change);

    /// <summary>Awaits the write in flight and writes the document when it is still dirty.</summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation. It is the only caller of <see cref="IConfigurationStore{TDocument}.SaveAsync"/>, keeps at
/// most one write in flight and coalesces the changes that arrive meanwhile (D-04).
/// </summary>
public sealed class ConfigurationDocument : IConfigurationDocument, IDisposable
{
    public ConfigurationDocument(
        IConfigurationStore<StickyDockConfiguration> store,
        StickyDockConfiguration initial,
        ILogger logger);

    /// <summary>Blocks the shutdown until the pending write is on disk, with a bounded wait.</summary>
    public void Dispose();
}
```

Reglas del escritor, para que se implemente sin ambigüedad:

- **Un solo escritor.** `IConfigurationStore` no se modifica; ningún tipo de lógica de negocio lo recibe. El coordinador de docking, el controlador de interacción y las preferencias de E6 reciben `IConfigurationDocument`.
- **Una escritura en vuelo como máximo.** `Update` reemplaza `Current`, eleva `Changed` y marca el documento como sucio; si no hay escritura en curso, arranca una; si la hay, no arranca otra (la que está en curso volverá a escribir cuando termine si el documento sigue sucio). El resultado es *latest wins*: nunca se escribe un estado intermedio.
- **Sin bloqueo del hilo de UI.** `Update` no espera E/S.
- **`FlushAsync` y `Dispose`.** `FlushAsync` espera la escritura en curso y escribe si quedó sucio. `Dispose` lo hace de forma síncrona con una espera acotada (2 s) y registra un error si expira: la aplicación se cierra igual, la preferencia puede perderse, el cierre nunca se cuelga. Esto cierra D-04 sin meter `async void` en el apagado de WPF.
- **Un fallo de escritura no rompe la sesión**: se registra y se reintenta en el siguiente cambio.

```csharp
namespace StickyDock.Core.Configuration;

/// <summary>Moves a stored document from schema version 1 to 2 by adding the grace period (D6).</summary>
public sealed class AddGracePeriodMigration : IConfigurationMigration
{
    /// <inheritdoc />
    public int FromVersion => 1;

    /// <inheritdoc />
    public void Apply(JsonObject document);   // writes "gracePeriodMs" = 400 when the property is missing
}

/// <summary>Single place where the configuration store of the product is built, with its migrations registered.</summary>
public static class StickyDockSettings
{
    public static IConfigurationStore<StickyDockConfiguration> CreateStore(string filePath, ILogger? logger = null);
}
```

```csharp
// src\StickyDock.Core\Configuration\StickyDockConfiguration.cs
public const int CurrentSchemaVersion = 2;

/// <summary>
/// Gets the grace period of the collapse timer, in milliseconds (E3, Q-03). Values outside 300..500 are normalized on
/// read by <c>GracePeriod.Normalize</c>; the persisted value is whatever the user chose.
/// </summary>
public int GracePeriodMilliseconds { get; init; } = 400;
```

### 5.2 Estado y tamaño de la barra (D2, D4)

```csharp
// src\StickyDock.Core\Services\DockState.cs — same namespace, two new members (E3, D4)
public enum DockState
{
    /// <summary>Collapsed to the idle tab. No timer is armed.</summary>
    Tab = 0,

    /// <summary>Deployed. Its footprint is the expanded one.</summary>
    Expanded = 1,

    /// <summary>Pointer over the tab, activation timer pending. The footprint is still the collapsed one.</summary>
    Arming = 2,

    /// <summary>Slide-out running. The footprint is still the expanded one until the settle timer fires.</summary>
    Collapsing = 3,
}
```

Los valores **numéricos no se persisten**: el estado nunca se guarda y la aplicación arranca siempre en `Tab` (§4.2: la barra "permanece en estado de reposo" al aparecer). Se fijan por claridad y para que un futuro registro sea estable.

```csharp
namespace StickyDock.Core.Docking;

/// <summary>Declared footprint of the bar, in device-independent units (D2). Single adjustment point of the sizes.</summary>
/// <param name="CollapsedLength">Length along the edge while collapsed. Equal to the window of E1: the tab is the whole footprint.</param>
/// <param name="CollapsedThickness">Thickness away from the edge while collapsed.</param>
/// <param name="ExpandedLength">Length along the edge when deployed.</param>
/// <param name="ExpandedThickness">Thickness away from the edge when deployed.</param>
public readonly record struct DockBarMetrics(
    double CollapsedLength,
    double CollapsedThickness,
    double ExpandedLength,
    double ExpandedThickness)
{
    /// <summary>
    /// Design values: a collapsed footprint of 120×36 —the window of E1, unchanged— and a deployed panel of 168×96.
    /// </summary>
    public static DockBarMetrics Default { get; } = new(120d, 36d, 168d, 96d);

    /// <summary>
    /// Footprint of a state and an orientation. Without an edge (floating) the bar is laid out horizontally.
    /// </summary>
    public DockBarSize GetSize(bool expanded, DockEdge? anchor);
}

/// <summary>
/// Declares the size the bar occupies for the state it is in. Implemented in Core over the dock state, so that the
/// size depends only on the state and never on the size the window happens to have (D-01, rule R1).
/// </summary>
public interface IDockBarSizeSource
{
    /// <summary>Size of the bar for the current state, laid out against <paramref name="anchor"/>.</summary>
    DockBarSize GetCurrentSize(DockEdge? anchor);
}
```

```csharp
namespace StickyDock.Core.Interaction;

/// <summary>Default size source: the dock state decides which of the two footprints applies.</summary>
public sealed class DockBarSizeSource : IDockBarSizeSource
{
    public DockBarSizeSource(IDockStateService states, DockBarMetrics metrics);
    public DockBarSize GetCurrentSize(DockEdge? anchor);
}
```

`DockBarSizeSource` vive en Core y no en la App a propósito: así el tamaño que se aplica es comprobable con un `IDockStateService` de dobles y la App no aporta ninguna decisión.

### 5.3 Interacción: regiones, tiempos y temporizadores (D4)

```csharp
namespace StickyDock.Core.Interaction;

/// <summary>Part of the interactive area the pointer is in (A4).</summary>
public enum DockRegion
{
    None = 0,

    /// <summary>The idle tab. Present in every state.</summary>
    Tab = 1,

    /// <summary>The deployed bar.</summary>
    Bar = 2,

    /// <summary>A deployed ribbon (E4). Declared here so that E3 does not have to be reopened.</summary>
    Ribbon = 3,
}

/// <summary>
/// Port the view model uses to report that the pointer entered or left an interactive region (D7). It is deliberately
/// separate from <see cref="StickyDock.Core.Services.IDockStateService"/>: the state is consulted, the pointer input is
/// notified, and E6 needs the state contract with its four members only.
/// </summary>
/// <remarks>Single-threaded: the implementation must be called on the UI thread.</remarks>
public interface IDockRegionSink
{
    /// <summary>Records that the pointer entered <paramref name="region"/>.</summary>
    void EnterRegion(DockRegion region);

    /// <summary>Records that the pointer left <paramref name="region"/>.</summary>
    void LeaveRegion(DockRegion region);
}

/// <summary>Bounds of the configurable grace period (Q-03, §4.3-03).</summary>
public static class GracePeriod
{
    public const int MinimumMilliseconds = 300;
    public const int MaximumMilliseconds = 500;
    public const int DefaultMilliseconds = 400;

    /// <summary>Clamps the stored value into the allowed range, falling back to the default when it is not a value.</summary>
    public static int Normalize(int milliseconds);
}

/// <summary>Durations of the interaction. Activation and slide are fixed by RF02 and by this ADR; the grace is not.</summary>
public readonly record struct InteractionTimings(TimeSpan Activation, TimeSpan Grace, TimeSpan Slide)
{
    /// <summary>200 ms, fixed by RF02 (§4.3-02) and by Q-03: it is not configurable.</summary>
    public const int ActivationMilliseconds = 200;

    /// <summary>Duration of the slide and of the micro-expansion. Engineering decision (see §12).</summary>
    public const int SlideMilliseconds = 160;

    /// <summary>Builds the timings from the stored grace period, normalized to 300..500 ms.</summary>
    public static InteractionTimings FromGrace(int graceMilliseconds);
}
```

```csharp
namespace StickyDock.Core.Timing;

/// <summary>
/// One-shot timer with a single slot: scheduling cancels the previous timer. Disposing nowhere is needed because the
/// slot belongs to its owner, which is the only one that can arm it (D4).
/// </summary>
/// <remarks>Implemented by a <c>DispatcherTimer</c> in the application and by a manual clock in the tests.</remarks>
public interface ISingleShotScheduler
{
    /// <summary>Gets a value indicating whether a timer is armed.</summary>
    bool IsPending { get; }

    /// <summary>Arms the slot, replacing any pending timer.</summary>
    void Schedule(TimeSpan delay, Action callback);

    /// <summary>Disarms the slot. Idempotent.</summary>
    void Cancel();
}
```

```csharp
namespace StickyDock.Core.Interaction;

/// <summary>
/// Owns the interaction state of one bar: regions, timers and transitions (E3, D4, D7). It is the single implementation
/// of <see cref="IDockStateService"/> and of <see cref="IDockRegionSink"/>, so the state contract does not grow with the
/// pointer input and the view model only holds interfaces.
/// </summary>
/// <remarks>
/// Single-threaded: every member must be called on the UI thread and the scheduler delivers its callbacks there. The
/// class takes no locks and must not be shared between threads.
/// </remarks>
public sealed class DockInteractionController : IDockStateService, IDockRegionSink, IDisposable
{
    public DockInteractionController(
        ISingleShotScheduler scheduler,
        IConfigurationDocument configuration,
        ILogger logger);

    /// <summary>Gets the current state.</summary>
    public DockState State { get; }

    /// <summary>Gets a value indicating whether a drag of the bar is in progress (P3).</summary>
    public bool IsDragging { get; }

    /// <summary>Gets the regions the pointer is in. Diagnostic and test surface.</summary>
    public IReadOnlyCollection<DockRegion> ActiveRegions { get; }

    /// <summary>Raised on every state change, with the new state (contract of <see cref="IDockStateService"/>).</summary>
    public event EventHandler<DockStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public void EnterRegion(DockRegion region);

    /// <inheritdoc />
    public void LeaveRegion(DockRegion region);

    /// <summary>Suppresses the automatic transitions until <see cref="EndDrag"/>.</summary>
    public void BeginDrag();

    /// <summary>Reenables the automatic transitions and reevaluates with the regions as they are now.</summary>
    public void EndDrag();

    /// <summary>Deploys the bar, cancelling any pending timer (explicit override, used by E6 and by tests).</summary>
    void Expand();

    /// <summary>Collapses the bar immediately, cancelling any pending timer (explicit override, used by E6).</summary>
    void CollapseToTab();

    /// <summary>Cancels the pending timer. Idempotent.</summary>
    public void Dispose();
}
```

`Expand()` y `CollapseToTab()` se conservan porque ya forman parte de `IDockStateService` y E6 los necesita para el menú de la bandeja; la diferencia con el camino normal es que **cancelan** el temporizador en lugar de armarlo. `DockStateService` se elimina: su bloqueo interno sobra en un objeto de un solo hilo y su política es la que se sustituye.

`EnterRegion` y `LeaveRegion` son la implementación de `IDockRegionSink` (D7), no del contrato de estado. `BeginDrag`, `EndDrag`, `IsDragging` y `ActiveRegions` **no** forman parte de ninguna interfaz: su único llamador es la raíz de composición, que recibe el tipo concreto (§10). Publicar solo lo que la vista necesita es lo que permite que un doble de `IDockStateService` siga siendo trivial.

### 5.4 Animación: no hay contrato nuevo en Core (D5, P5.b)

El borrador anterior de este ADR preveía `SlideVector` y `ExpansionAnimation.GetSlideOffset(edge, metrics)` en Core, con el ViewModel publicando el vector y la duración. **Se retira**: `Timeline.From` y `Timeline.Duration` no son propiedades de dependencia, así que ninguna animación puede recibir esos valores por *binding* (P5.b) y las dos propiedades habrían quedado sin consumidor. La geometría del deslizamiento pasa a ser una tabla de cuatro filas en el XAML (§8.3), derivada de `DockBarMetrics`:

| Borde | Eje | `From` del despliegue | `To` del despliegue |
| --- | --- | --- | --- |
| `Top` | Y | `−(ExpandedThickness − CollapsedThickness)` = −60 | 0 |
| `Bottom` | Y | `+(ExpandedThickness − CollapsedThickness)` = +60 | 0 |
| `Left` | X | `−(ExpandedThickness − CollapsedThickness)` = −60 | 0 |
| `Right` | X | `+(ExpandedThickness − CollapsedThickness)` = +60 | 0 |
| flotante | — | sin desplazamiento: solo fundido | — |

- La magnitud es el **grosor ganado** al desplegarse, que es lo que el contenido debe recorrer para "salir del borde" (§4.3-01); con los valores por defecto son 60 unidades en los cuatro bordes, porque el grosor ganado no depende del eje del borde.
- El signo es **hacia fuera** de la pantalla: el contenido entra desde más allá del borde hacia su posición final.
- La duración es `InteractionTimings.SlideMilliseconds` (160 ms), también literal en el XAML.
- Que la tabla viva en la vista y no en Core es la consecuencia asumida de P5.b: la coherencia con `DockBarMetrics` e `InteractionTimings` se protege con la inspección de §10 y con la comprobación manual M-05, y queda declarada como deuda D-14.

### 5.5 Cambios en contratos existentes

```csharp
// src\StickyDock.Core\Docking\IDockWindowHost.cs — GetBarSize() is REMOVED (D-01). The rest is unchanged.
public interface IDockWindowHost
{
    PixelRect GetBounds();
    void ApplyBounds(PixelRect bounds);
    void BeginDrag();
}
```

```csharp
// src\StickyDock.Core\Docking\DockPlacementCalculator.cs — two new pure methods, nothing else changes
public static class DockPlacementCalculator
{
    /// <summary>Nearest edge of the work area. Ties are broken in the order Top, Left, Right, Bottom.</summary>
    public static DockEdge GetNearestEdge(PixelRect bounds, PixelRect workArea);

    /// <summary>
    /// Rectangle of a floating bar resized to <paramref name="size"/>, keeping the side nearest to
    /// <paramref name="anchor"/> fixed so that the bar grows inward. Returns <paramref name="current"/> untouched when
    /// the size already matches, which is what keeps the recalculation idempotent (rule R4).
    /// </summary>
    public static PixelRect GetFloatingBounds(PixelRect current, DockEdge anchor, DockBarSize size);
}
```

```csharp
// src\StickyDock.Core\Docking\DockPlacementCoordinator.cs — constructor and two members change (D1, D2)
public sealed class DockPlacementCoordinator : IDisposable
{
    public DockPlacementCoordinator(
        IDockingEventSource events,
        IWindowGeometrySource geometry,
        IDockWindowHost host,
        IDockBarSizeSource barSize,
        IConfigurationDocument configurationDocument,
        ILogger logger);

    /// <summary>Raised when the drag of the bar begins. The interaction controller suppresses expansion meanwhile.</summary>
    public event EventHandler? DragStarted;

    /// <summary>Raised after the drag completed and the snap was decided. The interaction controller reevaluates.</summary>
    public event EventHandler? DragCompleted;

    /// <summary>
    /// Recomputes the placement with the size the bar occupies for the state it is in now (D-09). Called by the
    /// composition root when the dock state changes. Never resets the last applied rectangle.
    /// </summary>
    public void NotifyBarSizeChanged();
}
```

El coordinador deja de escribir la preferencia con `_ = SaveAsync(...)` y pasa a `configurationDocument.Update(document => document with { DockEdge = edge })`. `_configurationStore` desaparece del tipo.

```csharp
// src\StickyDock.Core\ViewModels\DockBarViewModel.cs — hover properties replaced by state projection (D4, D5, D7)
public sealed class DockBarViewModel : ViewModelBase
{
    public DockBarViewModel(
        ILogger logger,
        IDockStateService dockStates,
        IDockRegionSink regions,
        IThemeService? themeService);

    /// <summary>Gets the current dock state, mirrored from the controller. Drives every visual trigger.</summary>
    public DockState DockState { get; }

    /// <summary>Gets the command a region element executes when the pointer enters it; it forwards to the region sink.</summary>
    public ICommand EnterRegionCommand { get; }

    /// <summary>Gets the command a region element executes when the pointer leaves it; it forwards to the region sink.</summary>
    public ICommand LeaveRegionCommand { get; }

    // IsHovered, TabOpacity, ScaleX, ScaleY and SlideFromX/SlideFromY/SlideMilliseconds are REMOVED: the
    // micro-expansion, the opacity and the slide are storyboards with literal values triggered by DockState (D5, P5.b).
}
```

`IsLightTheme`, `TabBackground`, `TabForeground`, `DockEdge`, `ApplyPlacement`, `DragCommand` y `DragRequested` **no cambian**: E1 y E2 siguen funcionando con el mismo contrato. `DockBarMetrics` sale del constructor del ViewModel porque solo lo necesitaba para calcular el vector de deslizamiento, que ahora es una tabla en el XAML (P5.b).

### 5.5.1 Efecto sobre ADR-002 (sustitución parcial)

Lo que cambia del ADR-002, y nada más:

| Elemento de ADR-002 | Cómo queda | Motivo |
| --- | --- | --- |
| `IDockWindowHost.GetBarSize()` | **Retirado**. La interfaz conserva `GetBounds`, `ApplyBounds` y `BeginDrag` | D-01: medir con `ActualWidth`/`ActualHeight` permitía que un `SetWindowPos` ajeno contaminara el cálculo siguiente. El tamaño pasa a declararse en `DockBarMetrics` y a leerse por `IDockBarSizeSource` |
| Constructor de `DockPlacementCoordinator` | Recibe `IDockBarSizeSource` e `IConfigurationDocument` en lugar del almacén y del registro inmutable | D2 (tamaño declarado) y D1 (documento único: D-07 y D-04) |
| Persistencia del borde | `configurationDocument.Update(...)` en lugar de `_ = SaveAsync(...)` | D1, D-04 |
| `DockBarViewModel` | Pierde las propiedades de *hover* y los comandos de escala; gana `DockState` y los comandos de región | D4, D5, D7 |

Consecuencias sobre la **lista de verificación** de `ADR-002-docking-y-observacion-del-area-de-trabajo.md`:

- El punto «Crear — `StickyDock.App`: `WpfDockWindowHost.cs` — `GetBarSize` … `GetBounds`, `ApplyBounds`» se reduce a `GetBounds`, `ApplyBounds` y el `BeginDrag` que la implementación ya añadió.
- El punto «Crear — tests: `Docking\FakeDockWindowHost.cs`» pierde `BarSize`/`GetBarSize`; el tamaño del doble pasa a `FakeBarSizeSource`.
- El tercer filtro de ADR-002 (idempotencia del coordinador: solo aplica cuando el rectángulo difiere) **se conserva intacto**; este ADR lo reutiliza como regla R3.
- Ni la observación del entorno (`IDockingEventSource`, `WinEventDockingEventSource`, `DockingMessageClassifier`, los `WM_*`) ni la geometría (`PixelRect`, `MonitorGeometry`, `TaskbarPlacement`, `GetDockedBounds`, `ClampIntoWorkArea`) se tocan.
- El mapa E2-CA → prueba de ADR-002 **no se ve afectado**: ningún criterio de E2 dependía de `GetBarSize()`; las pruebas que lo usaban cambian de doble, no de aserción.

Fuera de esto, ADR-002 sigue vigente y no requiere revisión.

### 5.6 Contratos del lado de la App

```csharp
namespace StickyDock.App.Timing;

/// <summary>Adapter of <see cref="ISingleShotScheduler"/> over one <c>DispatcherTimer</c> created on the UI thread.</summary>
internal sealed class DispatcherTimerScheduler : ISingleShotScheduler
{
    public DispatcherTimerScheduler();
    public bool IsPending { get; }
    public void Schedule(TimeSpan delay, Action callback);
    public void Cancel();
}
```

```csharp
namespace StickyDock.App.Input;

/// <summary>
/// Adapter that turns the pointer events of one element into the region commands of the view model. It holds no policy:
/// it forwards and nothing else (§5.2-01).
/// </summary>
internal static class PointerRegionBehavior
{
    public static readonly DependencyProperty RegionProperty;     // DockRegion, attached
    public static readonly DependencyProperty EnterCommandProperty; // ICommand, attached
    public static readonly DependencyProperty LeaveCommandProperty; // ICommand, attached
}
```

Uso en la vista, sin una línea de código detrás:

```xml
<Border docking:PressDragBehavior.Command="{Binding DragCommand}"
        input:PointerRegionBehavior.Region="{x:Static interaction:DockRegion.Tab}"
        input:PointerRegionBehavior.EnterCommand="{Binding EnterRegionCommand}"
        input:PointerRegionBehavior.LeaveCommand="{Binding LeaveRegionCommand}" />
```

---

## 6. Geometría del despliegue

### 6.1 De dónde sale el tamaño

```
expanded   = state ∈ { Expanded, Collapsing }        (footprint already deployed)
             state ∈ { Tab, Arming }                → collapsed
vertical   = anchor ∈ { Left, Right }                 (the bar runs along a vertical edge)
size       = expanded
             ? (vertical ? (ExpandedThickness, ExpandedLength) : (ExpandedLength, ExpandedThickness))
             : (vertical ? (CollapsedThickness, CollapsedLength) : (CollapsedLength, CollapsedThickness))
```

- **R1 — el tamaño no se mide.** `DockBarSizeSource` consulta el estado y devuelve un valor declarado. Ninguna ruta del cálculo lee `ActualWidth`, `ActualHeight`, `Window.Width` ni el rectángulo de la ventana para deducir el tamaño. Esto es lo que cierra D-01: un `SetWindowPos` ajeno (o un `WM_EXITSIZEMOVE` tras un redimensionado intruso) no puede contaminar el siguiente cálculo.
- Los valores por defecto son `(120, 36, 168, 96)`: la **huella** plegada coincide con la ventana de E1 (`MainWindow.xaml:6-7`) y la desplegada es 168×96. La huella plegada **no** crece para hacer sitio al micro-efecto: la pestaña es un `Border` de tamaño de contenido (~58×27 con el `Padding` y el `TextBlock` actuales), no un elemento que llene la ventana, así que el 6 % de escala lo resuelve el propio `layout` sin acercarse al borde. Cualquier ampliación futura de la huella plegada haría que esa superficie capturara clics y rompería E1-CA4 (deuda D-15).
- El tamaño se interpreta **siempre en unidades independientes del dispositivo** y se convierte con el DPI del monitor, igual que en ADR-002. Así la barra mide lo mismo a 100 % y a 150 %.

### 6.2 Barra acoplada: se reutiliza `GetDockedBounds` sin tocar

```
scale  = monitor.Dpi / 96
w      = Min(Round(size.Width  * scale, AwayFromZero), workArea.Width)
h      = Min(Round(size.Height * scale, AwayFromZero), workArea.Height)
centredX = workArea.X + (workArea.Width  − w) / 2        (división entera, trunca)
centredY = workArea.Y + (workArea.Height − h) / 2
```

| Borde | X | Y | Ancho | Alto |
| --- | --- | --- | --- | --- |
| `Top` | `centredX` | `workArea.Y` | `w` | `h` |
| `Bottom` | `centredX` | `workArea.Bottom − h` | `w` | `h` |
| `Left` | `workArea.X` | `centredY` | `w` | `h` |
| `Right` | `workArea.Right − w` | `centredY` | `w` | `h` |

**No hay fórmulas nuevas para el caso acoplado.** El rectángulo acoplado está anclado al borde exterior por construcción y centrado a lo largo del borde, así que crecerlo mantiene el anclaje y el centro: el crecimiento va **hacia dentro de la pantalla**, que es exactamente lo que §4.3-01 pide para el *slide-in* y lo que evita que la barra tape la barra de tareas. `GetDockedBounds` (`DockPlacementCalculator.cs:84-109`) se usa tal cual.

### 6.3 Barra flotante: ancla por borde más cercano

Una barra flotante no tiene borde que la ancle, así que hay que decidir hacia dónde crece:

```
anchor = GetNearestEdge(current, workArea)
gap(Top)    = max(0, current.Y − workArea.Y)
gap(Left)   = max(0, current.X − workArea.X)
gap(Right)  = max(0, workArea.Right  − current.Right)
gap(Bottom) = max(0, workArea.Bottom − current.Bottom)
   → menor distancia; empates en el orden Top, Left, Right, Bottom

cx = current.X + current.Width / 2
cy = current.Y + current.Height / 2
```

| Ancla | X | Y |
| --- | --- | --- |
| `Top` | `cx − w/2` | `current.Y` |
| `Bottom` | `cx − w/2` | `current.Bottom − h` |
| `Left` | `current.X` | `cy − h/2` |
| `Right` | `current.Right − w` | `cy − h/2` |

y después `ClampIntoWorkArea(result, workArea)`, ya existente (`DockPlacementCalculator.cs:176-190`). El ancla se calcula **antes** de cambiar el tamaño, de modo que el crecimiento va siempre hacia el interior.

### 6.4 Invariantes (verificables)

- **I1 — El rectángulo plegado está contenido en el desplegado** siempre que ambos quepan en el área de trabajo. Acoplado: mismo anclaje en el grosor y mismo centro en el largo. Flotante: `x_e = x_c + ⌊w_c/2⌋ − ⌊w_e/2⌋ ≤ x_c` y `x_e + w_e ≥ x_c + w_c` porque `w_e ≥ w_c`; análogo en Y.
- **I2 — El borde exterior no se mueve al desplegar.** Acoplado: el borde de anclaje se conserva por construcción. Flotante: el lado del ancla se conserva por la tabla de §6.3. Es lo que permite que la pestaña —único asa y ancla visual— no salte.
- **I3 — La pestaña conserva su posición en pantalla.** Alineada al borde exterior y centrada a lo largo del borde, la pestaña ocupa el mismo rectángulo en `Tab`, `Arming`, `Expanded` y `Collapsing`, porque el centro y el anclaje del contenedor no cambian (I2) y la pestaña no tiene transformación de desplazamiento. La pestaña es un `Border` de **tamaño de contenido** (~58×27), no un elemento que llene la ventana: por eso el micro-efecto de 1,06 cabe dentro de la huella sin reservarle margen. *Consecuencia para la vista*: el `Border` de la pestaña debe alinearse según `DockEdge` (`Top` → arriba-centro, `Left` → izquierda-centro, etc.), lo que hoy no hace (`MainWindow.xaml:44-46` fija arriba-centro). Es un defecto visual heredado de E2 que E3 corrige porque la pestaña deja de ser decorativa.
- **I4 — Todo rectángulo aplicado está dentro del área de trabajo**, salvo que la barra no quepa, en cuyo caso se recorta (`Min`) y se ancla con `Clamp`, igual que en E2.
- **I5 — El eje no cambia durante la vida de una barra**: la orientación se deduce del `DockEdge` vigente, y el `DockEdge` solo cambia por arrastre o por evento del entorno.

### 6.5 Ejemplo numérico (verificable en la suite)

Monitor `1920×1040` en `(0,0)` con DPI 120 (125 %): huella plegada `150×45` —la de la pestaña, sin holgura— y desplegada `210×120`.

| Borde | Plegado | Desplegado |
| --- | --- | --- |
| `Top` | `(885, 0, 150, 45)` | `(855, 0, 210, 120)` |
| `Bottom` | `(885, 995, 150, 45)` | `(855, 920, 210, 120)` |
| `Left` | `(0, 445, 45, 150)` | `(0, 415, 120, 210)` |
| `Right` | `(1875, 445, 45, 150)` | `(1800, 415, 120, 210)` |

En los bordes verticales el ancho es el **grosor** y el alto es el **largo**: `45 = 36 × 1,25` y `150 = 120 × 1,25` plegados, `120 = 96 × 1,25` y `210 = 168 × 1,25` desplegados. Por eso el centrado vertical usa el largo y no el grosor: `445 = (1040 − 150)/2` y `415 = (1040 − 210)/2`, y la coordenada del borde exterior descuenta el **ancho**: `1875 = 1920 − 45` y `1800 = 1920 − 120`.

Flotante en `(700, 400, 150, 45)`: ancla `Top` (hueco 400 frente a 595 de `Bottom`, 700 de `Left` y 1070 de `Right`) → desplegado `(670, 400, 210, 120)`, que ya cabe en el área de trabajo.

### 6.6 Idempotencia y ausencia de bucle de realimentación

El riesgo real de esta etapa es un ciclo `estado → tamaño → colocación → mensaje → colocación`. Se corta con cinco reglas, no con banderas ad hoc:

- **R1** — la fuente de tamaño depende **solo del estado**, nunca del tamaño real de la ventana. Un ciclo necesitaría realimentación; no la hay.
- **R2** — `_lastApplied` se escribe **antes** de llamar a `ApplyBounds`, de modo que cualquier evento que llegue durante la aplicación ya ve el rectángulo como aplicado.
- **R3** — el coordinador solo mueve la ventana cuando el rectángulo calculado **difiere** de `_lastApplied` (comportamiento ya vigente, `DockPlacementCoordinator.cs:239-244`), y `ApplyBounds` es el único punto de escritura.
- **R4** — `NotifyBarSizeChanged()` no reinicia `_lastApplied`: si el tamaño no cambió (p. ej. `Arming → Tab`, ambos plegados), el rectángulo calculado coincide y no hay movimiento. Además `GetFloatingBounds` devuelve el rectángulo actual sin tocar cuando el tamaño ya coincide, así que recalcular dos veces seguidas es un no-op exacto.
- **R5** — `PlacementChanged` solo se eleva cuando hay cambio real de borde o de rectángulo, así que el ViewModel no re-notifica por ruido.

Consecuencia medible: el despliegue produce **una** aplicación de `SetWindowPos` (crecer) y el colapso **una** (encoger), sin importar cuántos mensajes del entorno lleguen en medio.

### 6.7 Barra más grande que el área de trabajo

Si el área de trabajo es menor que la barra desplegada (800×600 con la barra de 168×96 escalada, o una barra de tareas que deja una franja mínima), `GetDockedBounds` recorta con `Min` y `ClampIntoWorkArea` recoloca. La barra queda pegada al borde, completa dentro de la pantalla y con el contenido potencialmente recortado por `ClipToBounds`. Es el mismo compromiso que E2 aceptó para el plegado y se declara aquí: **el contenido puede recortarse, la barra nunca sale de pantalla** (E2-CA3 se conserva).

---

## 7. Máquina de estados y temporizadores

### 7.1 Las cuatro fases y qué significa cada una en pantalla

| Estado | Huella (tamaño) | Qué se ve | Temporizador |
| --- | --- | --- | --- |
| `Tab` | plegada | la pestaña sola, translúcida de tono invertido | ninguno |
| `Arming` | plegada | la pestaña con el micro-efecto de expansión | activación, 200 ms |
| `Expanded` | desplegada | la barra; el *slide-in* ya terminó o está corriendo | ninguno, o **gracia** pendiente |
| `Collapsing` | desplegada | el fundido de salida corriendo; la ventana encoge al asentarse | asentamiento, 160 ms |

Dos decisiones de semántica que conviene no perder:

1. **La gracia no es un estado.** Ocurre dentro de `Expanded` con un temporizador pendiente. Así `Collapsing` significa siempre "hay una animación de salida en curso" y el ViewModel no necesita propiedades derivadas para disparar el *storyboard*: `DockState == Collapsing` basta.
2. **`Arming` sí es un estado** porque se ve: es donde vive el micro-efecto (HU02 CA #2) y donde el temporizador puede cancelarse (E3-CA4).

### 7.2 Tabla de transiciones

| Estado | Evento | Estado siguiente | Acción |
| --- | --- | --- | --- |
| `Tab` | `Enter(Tab)` | `Arming` | `Schedule(Activation)`; el *storyboard* del micro-efecto arranca por `DataTrigger` |
| `Arming` | `Timer(Activation)` | `Expanded` | eleva `StateChanged` → la App crece la ventana → arranca el *slide-in* |
| `Arming` | `Leave(Tab)` con regiones vacías | `Tab` | `Cancel()` (E3-CA4) |
| `Arming` | `Leave(Tab)` con otra región activa | `Expanded` | `Cancel()` y despliega: el puntero ya está dentro del área interactiva |
| `Expanded` | regiones vacías | `Expanded` | `Schedule(Grace)`; **no** cambia de estado (E3-CA2) |
| `Expanded` | `Enter(cualquiera)` con gracia pendiente | `Expanded` | `Cancel()`: la gracia se rearma la próxima vez (E3-CA3) |
| `Expanded` | `Timer(Grace)` | `Collapsing` | arranca el fundido de salida (§8.3); `Schedule(Settle)` |
| `Collapsing` | `Enter(cualquiera)` | `Expanded` | `Cancel()`; el despliegue relanza su animación desde el desplazamiento inicial |
| `Collapsing` | `Timer(Settle)` | `Tab` | la App encoge la ventana hasta la huella plegada, que es exactamente la pestaña |
| cualquiera | `BeginDrag` | sin cambio | `Cancel()`; `IsDragging = true`; las regiones se siguen registrando |
| cualquiera | `EndDrag` | según regiones | `IsDragging = false`; `Reevaluate()` |
| `Tab`/`Arming`/`Expanded`/`Collapsing` | `Expand()` | `Expanded` | `Cancel()`; salto directo (E6) |
| `Tab`/`Arming`/`Expanded`/`Collapsing` | `CollapseToTab()` | `Tab` | `Cancel()`; salto directo (E6) |

### 7.3 Rutina única de reevaluación

Toda entrada (región, temporizador, arrastre, mandato explícito) pasa por la misma función. No hay dos caminos que puedan discrepar:

```
Reevaluate():
    if IsDragging:                       return          // el arrastre manda
    if State == Collapsing:              return          // lo resuelve el asentamiento o una entrada
    if State == Expanded:
        if ActiveRegions is empty:       Schedule(Grace)  // si ya hay gracia pendiente, no se rearma
        else:                            Cancel()
        return
    if State == Arming:
        if ActiveRegions contains Tab:   return          // la activación sigue pendiente
        if ActiveRegions is empty:       Cancel(); To(Tab)
        else:                            Cancel(); To(Expanded)   // ya está dentro
        return
    // State == Tab
    if ActiveRegions contains Tab:       Schedule(Activation)
    else:                                Cancel()
```

Reglas que la tabla y la rutina implican y que se prueban una a una:

- **Una sola ranura.** `Schedule` cancela lo pendiente; es imposible tener activación y gracia a la vez. El controlador ni siquiera necesita comprobar `IsPending`/`Cancel` antes de `Schedule`.
- **`Schedule` es idempotente en intención, no en tiempo**: llamar `Reevaluate` dos veces seguidas estando en `Expanded` con regiones vacías **no** rearma la gracia (se comprueba el tipo de temporizador pendiente), de lo contrario un `MouseLeave` duplicado alargaría el colapso indefinidamente.
- **Reentradas**: los manejadores de temporizador se ejecutan una sola vez y desarman la ranura antes de actuar, así que una transición provocada por un temporizador no puede volver a entrar en él. La ranura única hace innecesario un cerrojo de reentrada.
- **`Dispose`** cancela la ranura: cerrar la aplicación con un temporizador pendiente no deja un `callback` que despierte al proceso moribundo.

### 7.4 Temporizadores: valores y quién los decide

| Temporizador | Valor | Origen | Configurable |
| --- | --- | --- | --- |
| Activación (*hover*) | 200 ms | RF02, §4.3-02, Q-03 | **No** |
| Gracia | 400 ms por defecto, 300–500 ms | §4.3-03, Q-03 | Sí, en E6 |
| Asentamiento (fundido de salida → encoger) | 160 ms | este ADR (§12) | No |

El controlador **lee la gracia del documento en el momento de armar el temporizador** (`_configuration.Current.GracePeriodMilliseconds` normalizado por `GracePeriod.Normalize`). No la cachea en el constructor. Esto es lo que permitirá que E6-CA3 («los cambios de preferencias surten efecto de inmediato») se cumpla sin tocar E3, y evita el defecto clásico de la copia obsoleta que ya costó D-07.

### 7.5 Confirmación de A6: no hay *hook* global de ratón

§5.1-04 menciona "detección de entrada de puntero global". A6 ya la interpretó como detección por área de ventana y encargó al arquitecto confirmarlo en E3. **Se confirma, por tres razones**:

1. El comportamiento observable de RF02 se satisface igual: la barra solo reacciona al puntero cuando el puntero está sobre ella, que es la única situación en que puede desplegarse.
2. Un `WH_MOUSE_LL` se ejecuta en el hilo que lo instala y **retrasa la entrada de todo el sistema** mientras dure el *callback*; es incompatible con RNF-02 por diseño y con el RNF de "no interrumpir la experiencia del usuario" (§4.2-04).
3. Un *hook* de ratón global añade una superficie de fallo (se pierde si el sistema lo desaloja tras un *timeout*) que no aporta ningún criterio de aceptación.

La detección real son dos eventos por elemento (`MouseEnter`/`MouseLeave` en la pestaña, en el panel de la barra y, en E4, en cada Cinta), reenviados por `PointerRegionBehavior` a los comandos del ViewModel. Los eventos de ratón de WPF no consumen CPU cuando no llegan.

### 7.6 Preparación para E4 (Cintas)

- `DockRegion.Ribbon` ya existe y el controlador lo trata como cualquier otra región: la Cinta abierta mantiene la gracia cancelada, que es lo que E3-CA3 exige («moverse entre la barra desplegada y sus cintas no provoca un colapso accidental»).
- La exclusividad de apertura (§4.4-02) **no** entra en el controlador: es estado de Cintas y pertenecerá a `RibbonHostViewModel` en E4. Mezclarla aquí convertiría la máquina de interacción en una máquina de contenido.
- El crecimiento por contenido de E4 («la barra se extiende al añadir elementos o Cintas», RF03, A7) usa el mismo `NotifyBarSizeChanged()` que E3 introduce: E4 solo tiene que actualizar `DockBarMetrics` con su largo real o llamar al aviso, sin tocar el coordinador ni la política.

### 7.7 Orden de las notificaciones al desplegar

El crecimiento de la ventana y el arranque de la animación se disparan del **mismo** `StateChanged`, y el coordinador se suscribe en `SourceInitialized` mientras el ViewModel lo hizo en su construcción. Es decir: el ViewModel se enterará **primero**. No es un problema, y no hay que forzar el orden por dos razones:

1. Ambos manejadores se ejecutan de forma síncrona dentro de la misma operación del despachador; WPF no renderiza entre ellos. Cuando llega el primer fotograma, la ventana ya tiene el tamaño desplegado y el contenido ya está dentro de su `ClipToBounds`.
2. El *storyboard* arranca en el desplazamiento inicial declarado para el borde (§5.4), que saca el contenido fuera del recorte; aunque el tamaño llegara un fotograma después, el efecto no sería un contenido recortado, sino un deslizamiento desde el borde exacto.

Si en la verificación manual (M-05) apareciera un parpadeo, la corrección es de la vista y no del contrato: diferir el cambio de `DockState` a una operación con prioridad `Render`. Se anota para no confundirlo con un problema de diseño.

---

## 8. Reparto de la animación (§5.2-01, RNF-02)

### 8.1 Frontera

| Responsabilidad | Dónde vive | Por qué |
| --- | --- | --- |
| Decidir **cuándo** hay animación | `DockInteractionController` (estado) | es política, se prueba con el programador manual |
| Decidir **hacia dónde** y **cuánto** se desplaza | Tabla de §5.4, escrita como literales en el XAML de §8.3 | es geometría declarada: la animación no admite *binding* (P5.b) |
| Exponer el estado a los *bindings* | `DockBarViewModel` | es proyección: publica `DockState` y los comandos de región, nada más |
| **Ejecutar** la animación | `MultiDataTrigger` y *storyboards* en `MainWindow.xaml` | es renderizado, y no lleva ninguna decisión |

La frontera no transporta ningún valor de animación: el ViewModel publica `DockState` y la vista lo traduce con literales, porque `From` y `Duration` no pueden llegar por *binding*. Es la consecuencia de P5.b y no una omisión.

### 8.2 Estructura de la vista

```
Window (WindowStyle=None, AllowsTransparency, Topmost)      ← tamaño y posición: los decide el coordinador
└── Grid  x:Name="RootPanel"                               ← NO recorta nada: aloja también la pestaña
    ├── Grid  x:Name="ClipPanel"  ClipToBounds="True"      ← ÚNICO recorte; ocupa la huella declarada
    │   └── Border  x:Name="ContentPanel"                  ← superficie desplegada y panel que se desliza
    │          RenderTransform: TranslateTransform(X=0, Y=0) ← animado por los storyboards
    │          Opacity                                       ← animada por los storyboards
    │          (E4) panel de Cintas                         ← se aloja aquí, sin cambiar la estructura
    └── Border  x:Name="TabBorder"  Panel.ZIndex="1"       ← la pestaña: estática, por encima, asa de arrastre
           tamaño de contenido (~58×27 con Padding y TextBlock), centrada según DockEdge (I3)
           NO llena la huella: la huella plegada es la ventana de E1, 120×36
```

Cuatro detalles que no son cosméticos:

- **El recorte va en `ClipPanel`, no en la raíz.** `RootPanel` no recorta: si lo hiciera, el micro-efecto de la pestaña —que la agranda un 6 %— quedaría cortado contra el borde de la ventana, que es exactamente el defecto H-2. El recorte es una responsabilidad del contenedor del contenido, y solo de él.
- **La pestaña no necesita margen porque no ocupa la ventana.** Es un `Border` de tamaño de contenido (~58×27 con `Padding="12,6"` y un `TextBlock` de `FontSize="11"`), alineado centro-arriba: el factor 1,06 la lleva a ~61×29, que sigue muy lejos de los 120×36 de la huella. El margen que se consideró en una revisión anterior se retira: no hacía falta y **rompía E1-CA4** al ampliar la superficie que captura clics en reposo (deuda D-15).
- **La pestaña va por encima y sin transformación**: es el ancla visual (I3) y el asa de arrastre (D3). Si se desplazara con el contenido, el usuario perdería la referencia y el `PressDragBehavior` se movería bajo el cursor.
- **El tamaño de la ventana no se anima.** Se aplica de golpe con `SetWindowPos` y lo que se anima es el contenido dentro. Animar `Width`/`Height` de una ventana WPF con `AllowsTransparency` obligaría a un *layout* por fotograma y dispararía `SizeChanged` en bucle; es exactamente lo que RNF-02 no admite.

#### Qué se ve en `Expanded` durante E3 (H-3)

E3 no aporta contenido: aporta el **despliegue**. Lo que se ve al desplegarse es, literalmente, esto:

| Elemento | Qué es en E3 | Qué añade E4 |
| --- | --- | --- |
| `ContentPanel` | Un `Border` translúcido con el fondo y el borde de la barra (los recursos de tema de E1), `CornerRadius` y la huella declarada 168×96 | Las Cintas dentro del mismo panel |
| Su contenido | **Nada**: ningún texto, ningún icono, ningún *placeholder* | Los indicadores de Cinta y sus accesos directos |
| La pestaña | La misma de E1, ahora con el micro-efecto de activación | Sin cambios |

- El panel **no incorpora interactividad inventada**: sin botones, sin etiquetas y sin zonas activas. Lo único interactivo de E3 es la pestaña, que ya lo era, y por eso `DockRegion.Bar` se declara pero no se asocia a ningún elemento todavía.
- Que el panel esté vacío es deliberado y es lo mínimo demostrable: E3-CA1 y E3-CA2 se verifican sobre **su tamaño y su movimiento**, no sobre su contenido. Rellenarlo con algo sería adelantar E4.
- La superficie es translúcida y no opaca para que el despliegue se lea como una capa sobre el escritorio (§4.1) y para que la ausencia de contenido no parezca un fallo de carga.

### 8.3 Storyboards declarativos

```xml
<!-- Esquema, no código de producción: el detalle exacto pertenece al implementador.
     El elemento estilizado ES el panel animado, así que no hay Storyboard.TargetName ni espacio de nombres que resolver. -->
<Style TargetType="Border" x:Key="ContentPanelStyle">
    <Setter Property="Opacity" Value="1" />
    <Style.Triggers>

        <!-- Despliegue acoplado arriba: el contenido entra desde fuera de la pantalla. -->
        <MultiDataTrigger>
            <MultiDataTrigger.Conditions>
                <Condition Binding="{Binding DockState}" Value="{x:Static services:DockState.Expanded}" />
                <Condition Binding="{Binding DockEdge}" Value="{x:Static docking:DockEdge.Top}" />
            </MultiDataTrigger.Conditions>
            <MultiDataTrigger.EnterActions>
                <BeginStoryboard>
                    <Storyboard FillBehavior="Stop">
                        <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(TranslateTransform.Y)"
                                         From="-60" To="0" Duration="0:0:0.16" />
                        <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                         From="0.3" To="1" Duration="0:0:0.16" />
                    </Storyboard>
                </BeginStoryboard>
            </MultiDataTrigger.EnterActions>
        </MultiDataTrigger>

        <!-- Un bloque idéntico por borde: mismo patrón, eje y signo según la tabla de §5.4. -->

        <!-- Barra flotante: no hay borde del que entrar, así que solo hay fundido. -->
        <MultiDataTrigger>
            <MultiDataTrigger.Conditions>
                <Condition Binding="{Binding DockState}" Value="{x:Static services:DockState.Expanded}" />
                <Condition Binding="{Binding DockEdge}" Value="{x:Null}" />
            </MultiDataTrigger.Conditions>
            <MultiDataTrigger.EnterActions>
                <BeginStoryboard>
                    <Storyboard FillBehavior="Stop">
                        <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                         From="0.3" To="1" Duration="0:0:0.16" />
                    </Storyboard>
                </BeginStoryboard>
            </MultiDataTrigger.EnterActions>
        </MultiDataTrigger>

        <!-- Salida: fundido, sin condición de borde porque no hay desplazamiento que invertir. -->
        <DataTrigger Binding="{Binding DockState}" Value="{x:Static services:DockState.Collapsing}">
            <DataTrigger.EnterActions>
                <BeginStoryboard>
                    <Storyboard FillBehavior="HoldEnd">
                        <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                         From="1" To="0" Duration="0:0:0.12" />
                    </Storyboard>
                </BeginStoryboard>
            </DataTrigger.EnterActions>
        </DataTrigger>
    </Style.Triggers>
</Style>

<!-- Micro-efecto de HU02 CA #2: la pestaña crece y vuelve sin acercarse al borde de la ventana. -->
<Style TargetType="Border" x:Key="TabBorderStyle">
    <Setter Property="RenderTransform">
        <Setter.Value>
            <ScaleTransform ScaleX="1" ScaleY="1" />
        </Setter.Value>
    </Setter>
    <Setter Property="RenderTransformOrigin" Value="0.5,0.5" />
    <Style.Triggers>
        <DataTrigger Binding="{Binding DockState}" Value="{x:Static services:DockState.Arming}">
            <DataTrigger.EnterActions>
                <BeginStoryboard>
                    <Storyboard FillBehavior="Stop">
                        <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                                         To="1.06" Duration="0:0:0.06" AutoReverse="True" />
                        <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleY)"
                                         To="1.06" Duration="0:0:0.06" AutoReverse="True" />
                    </Storyboard>
                </BeginStoryboard>
            </DataTrigger.EnterActions>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

Por qué esta forma y no la del borrador anterior:

1. **`Timeline.From` y `Timeline.Duration` no son propiedades de dependencia.** Ninguna animación puede recibir el desplazamiento ni la duración por *binding*, ni dentro ni fuera de un `Style`. A eso se suma que el árbol de animaciones de un `Style` se congela al sellarse: un *binding* que sobreviviera lanzaría una excepción en tiempo de ejecución que ni el compilador ni las pruebas verían. Los valores son, por tanto, **literales**, y su coherencia con Core se protege con la inspección de §10 y con M-05 (deuda D-14).
2. **`MultiDataTrigger` y no `DataTrigger`**: la dirección depende de **dos** hechos a la vez (`DockState` y `DockEdge`) y un `DataTrigger` simple solo expresa una condición. Con `MultiDataTrigger` el disparo sigue siendo declarativo y sin código.
3. **El elemento animado es el estilizado**: al no usar `Storyboard.TargetName` desaparece el problema clásico de resolución de nombres entre el espacio de nombres del `Style` y el de la ventana.
4. **La salida es un fundido, no un desplazamiento invertido.** §4.3-01 fija la animación de **despliegue**; no pide una de repliegue. Un desplazamiento de salida exigiría cuatro bloques más con `To` literal para no ganar ningún criterio de aceptación, así que el contenido se desvanece en 120 ms y el asentamiento encoge la ventana a los 160 ms. El resultado es el mismo que persigue la etapa —una transición perceptible y barata— sin duplicar la tabla de literales.
5. **`FillBehavior="Stop"` en el despliegue y `HoldEnd` en la salida.** Con `Stop`, al terminar la animación el `TranslateTransform` vuelve a su valor base (0,0) —que es justo su posición final— y no queda ningún valor animado pegado. Con `HoldEnd` en la salida, el contenido permanece invisible hasta que el asentamiento encoge la ventana. Al volver a `Expanded`, el bloque de despliegue tiene `From` explícito, así que reanuda desde el borde aunque el fundido estuviera a medias.
6. **El micro-efecto es ida y vuelta en 120 ms** (`0:0:0.06` con `AutoReverse`) y centrado (`RenderTransformOrigin="0.5,0.5"`). Cabe porque la pestaña es de tamaño de contenido (~58×27 escalada a ~61×29) dentro de una huella de 120×36, no porque la huella le reserve margen: ampliarla más acercaría el borde animado al de la ventana, y la huella **no puede crecer** sin romper E1-CA4 (D-15).
7. **Se dispara por estado, no por comando**: el ViewModel no necesita saber que existe un *storyboard* y la vista no necesita saber cuándo empieza una transición.

### 8.4 Coste en reposo

- **Sin temporizador pendiente** no hay *callback* armado: en `Tab` y en `Expanded` sin gracia, el controlador no tiene nada que despertar.
- **Sin *storyboard* corriendo** WPF no programa fotogramas: terminada la animación de 160 ms, el `Clock` se detiene y `FillBehavior="Stop"` libera el valor animado.
- **Un `Clock` retenido no cuesta CPU**: el fundido de salida mantiene el valor con `HoldEnd` durante los 160 ms del asentamiento, pero un reloj en estado de retención no solicita fotogramas, y al pasar a `Tab` el contenido deja de pintarse.
- **Sin `TranslateTransform` animado** el contenido se renderiza como cualquier otro panel. La pestaña no tiene efectos permanentes: su `ScaleTransform` está en 1 y su opacidad es estática (la fija el tema de E1).
- **Nada mide ni sondea**: `DockBarSizeSource` devuelve un valor declarado y no hay `SizeChanged`, ni `CompositionTarget.Rendering`, ni `DispatcherTimer` permanente.

El reposo de E3 es, por tanto, el de E2 más cero. La medición de cierre de etapa lo comprueba (RNF-01 con el presupuesto de 42 MB y RNF-02 con 30 s de CPU).

---

## 9. Mapa E3-CA → pieza del diseño → prueba

Convención: las pruebas nuevas viven en `tests\StickyDock.Tests`; `ManualScheduler` avanza el reloj a mano, sin esperas reales.

| CA | Pieza que lo satisface | Cómo | Pruebas |
| --- | --- | --- | --- |
| **E3-CA1** — *hover* de 200 ms → micro-efecto y *slide-in* | `DockInteractionController` (`Enter(Tab)` → `Arming` → `Timer(Activation)` → `Expanded`), `InteractionTimings.ActivationMilliseconds`, `DockBarViewModel.DockState`, tabla de literales de §5.4 | La activación es un temporizador de un solo disparo; `Arming` es el estado que dispara el micro-efecto; la dirección del despliegue es la tabla de §8.3, derivada en §5.4 | `Interaction/DockInteractionControllerTests`: `EnterTab_ArmsActivation_AndEntersArming`, `ActivationTimerFires_DeploysBar`, `ActivationFires_ExactlyAt200ms_NotBefore`; `ViewModels/DockBarViewModelTests`: `Arming_IsProjectedToTheView`, `Expanded_IsProjectedToTheView` |
| **E3-CA2** — al salir, colapso tras 400 ms (300–500) | `GracePeriod`, `InteractionTimings.FromGrace`, `DockInteractionController` (`Expanded` + `Schedule(Grace)` → `Collapsing` → `Timer(Settle)` → `Tab`), `GracePeriodMilliseconds` persistido | La gracia se arma al quedarse sin regiones y se lee del documento en ese instante | `Interaction/DockInteractionControllerTests`: `LeaveRegion_SchedulesGrace_WithoutChangingState`, `GraceTimerFires_EntersCollapsing`, `SettleTimerFires_CollapsesToTab`; `Interaction/GracePeriodTests`: `Default_Is400`, `Normalize_ClampsBelow300`, `Normalize_ClampsAbove500`, `Normalize_KeepsValidValue`; `Configuration/GracePeriodPersistenceTests`: `Migration_Adds400ToASchemaOneDocument`, `StoredValue_SurvivesAReload` |
| **E3-CA3** — moverse entre la barra y sus Cintas no colapsa | `DockRegion` (`Tab`, `Bar`, `Ribbon`) + `Reevaluate` (solo arma la gracia con el conjunto de regiones **vacío**) | El área interactiva es la unión de las regiones activas (A4); la gracia absorbe el hueco transitorio entre regiones | `Interaction/DockInteractionControllerTests`: `EnteringBar_CancelsGrace`, `EnteringRibbon_CancelsGrace`, `TabBarRibbonSequence_NeverCollapses`, `LeaveAllRegions_ThenEnterRibbonWithinGrace_StaysExpanded` |
| **E3-CA4** — el temporizador se cancela si el cursor se retira antes de 200 ms | `Reevaluate` (rama `Arming`) + `ISingleShotScheduler.Cancel` | Al salir de la pestaña sin otra región activa se desarma la ranura y se vuelve a `Tab` | `Interaction/DockInteractionControllerTests`: `LeaveBefore200ms_CancelsActivation`, `LeaveBefore200ms_ThenWait_NeverExpands`, `LeaveAt199ms_DoesNotExpand` |
| **E3-CA5** — gracia configurable 300–500, 400 por defecto, persistida | `GracePeriod`, `StickyDockConfiguration.GracePeriodMilliseconds`, `AddGracePeriodMigration` (1 → 2), `IConfigurationDocument` | El valor se persiste en `settings.json`; se normaliza al leerlo; el controlador lo consulta al armar | `Configuration/GracePeriodPersistenceTests`: `SchemaTwo_IsWritten`, `SchemaOneDocument_IsMigratedAndKept`, `Grace_IsNormalizedWhenOutOfRange`; `Configuration/ConfigurationDocumentTests`: `Update_PersistsThroughTheStore`, `Update_RaisesChangedOnce`, `Update_WithAnEqualDocument_DoesNotWrite`, `FlushAsync_WritesPendingChange`, `ConcurrentUpdates_CoalesceIntoOneWrite`; `Interaction/DockInteractionControllerTests`: `Grace_IsReadFromTheDocumentWhenArmed` |

### Pruebas de soporte (no corresponden a un CA, protegen lo ya cerrado)

| Área | Pruebas |
| --- | --- |
| Tamaños declarados (D2, D-01) | `Docking/DockBarMetricsTests`: `Default_MatchesTheDesign`, `Horizontal_UsesLengthAsWidth`, `Vertical_UsesLengthAsHeight`; `Docking/DockBarSizeSourceTests`: `Arming_UsesCollapsedFootprint`, `Expanded_UsesExpandedFootprint`, `Collapsing_KeepsExpandedFootprintUnilSettled` |
| Huella plegada igual a la ventana de E1 (E1-CA4, D-15) | `Docking/DockBarMetricsTests`: `CollapsedFootprint_MatchesTheWindowOfE1` |
| Puertos de interacción (D7, H-1) | `ViewModels/DockBarViewModelTests`: `EnterRegionCommand_ForwardsTheRegionToTheSink`, `LeaveRegionCommand_ForwardsTheRegionToTheSink`, `DockState_IsMirroredFromTheStateService`; `Interaction/DockInteractionControllerTests`: `ImplementsTheStateServiceAndTheRegionSink`, `EnterRegion_ThroughTheSink_ArmsActivation` |
| Geometría del crecimiento (I1–I4) | `Docking/DockPlacementCalculatorTests`: `ExpandedBounds_ContainCollapsedBounds` (matriz 4 bordes × 3 geometrías × 2 DPI), `Growth_KeepsTheOuterBorderFixed`, `GetNearestEdge_TiesByDeclarationOrder`, `GetFloatingBounds_WithTheSameSize_ReturnsTheSameRect`, `GetFloatingBounds_GrowsAwayFromTheNearestEdge`, `ExpandedBounds_StayInsideWorkArea` |
| Coordinador con el aviso de tamaño (D-09) | `Docking/DockPlacementCoordinatorTests`: `NotifyBarSizeChanged_GrowsTheDockedBarInward`, `NotifyBarSizeChanged_WithTheSameSize_DoesNotMove`, `NotifyBarSizeChanged_OfAFloatingBar_KeepsTheAnchorSide`, `NotifyBarSizeChanged_AppliesOncePerTransition` |
| Arrastre (D3) | `Docking/DockPlacementCoordinatorTests`: `BeginDrag_RaisesDragStarted`, `DragEnd_RaisesDragCompleted`; `Interaction/DockInteractionControllerTests`: `BeginDrag_CancelsPendingTimers`, `Drag_SuppressesExpansion_EvenAfter200ms`, `EndDrag_ReevaluatesWithCurrentRegions` |
| Ciclo de vida | `Interaction/DockInteractionControllerTests`: `Dispose_CancelsThePendingTimer`, `Dispose_TwiceIsIdempotent`, `ExplicitExpand_CancelsTheGrace` |

### Cobertura de lo no comprobable por unidad

`DispatcherTimerScheduler`, `PointerRegionBehavior` y los *storyboards* no se prueban con pruebas unitarias: el primero necesita un `Dispatcher`, los otros renderizan. **No contienen decisiones**: la política está en el controlador (probada), la dirección del despliegue es la tabla de cuatro literales de §5.4 —verificada por inspección y con M-05— y el reenvío de eventos es una línea. Tampoco se prueba E1-CA4 en reposo: depende del *hit-test* real de WPF, así que se verifica midiendo la ventana colapsada (debe ser 120×36) y comprobando que un clic fuera de la pestaña no la activa. Se verifican por inspección, junto con E0-CA6, y con la medición de reposo de cierre de etapa.

---

## 10. Lista de verificación de implementación

### Crear — `StickyDock.Core`

- [ ] `src\StickyDock.Core\Configuration\IConfigurationDocument.cs` — `IConfigurationDocument`, `ConfigurationChangedEventArgs`.
- [ ] `src\StickyDock.Core\Configuration\ConfigurationDocument.cs` — escritor único, *latest wins*, `FlushAsync`, `Dispose` acotado.
- [ ] `src\StickyDock.Core\Configuration\AddGracePeriodMigration.cs` — 1 → 2, añade `gracePeriodMs = 400` si falta.
- [ ] `src\StickyDock.Core\Configuration\StickyDockSettings.cs` — `CreateStore`, migraciones registradas en un punto.
- [ ] `src\StickyDock.Core\Docking\DockBarMetrics.cs` — cuatro valores, `Default` = `(120, 36, 168, 96)`, `GetSize`.
- [ ] `src\StickyDock.Core\Docking\IDockBarSizeSource.cs`.
- [ ] `src\StickyDock.Core\Interaction\DockRegion.cs`.
- [ ] `src\StickyDock.Core\Interaction\IDockRegionSink.cs` — `EnterRegion`/`LeaveRegion` (D7).
- [ ] `src\StickyDock.Core\Interaction\GracePeriod.cs`.
- [ ] `src\StickyDock.Core\Interaction\InteractionTimings.cs`.
- [ ] `src\StickyDock.Core\Interaction\DockBarSizeSource.cs`.
- [ ] `src\StickyDock.Core\Interaction\DockInteractionController.cs` — tabla de §7.2, rutina de §7.3; implementa `IDockStateService` **e** `IDockRegionSink`.
- [ ] `src\StickyDock.Core\Timing\ISingleShotScheduler.cs`.
- [ ] `src\StickyDock.Core\Mvvm\RelayCommandOfT.cs` — comando con parámetro para las regiones.

### Crear — `StickyDock.App`

- [ ] `src\StickyDock.App\Timing\DispatcherTimerScheduler.cs` — un `DispatcherTimer`, creado en el hilo de UI.
- [ ] `src\StickyDock.App\Input\PointerRegionBehavior.cs` — propiedades adjuntas `Region`, `EnterCommand`, `LeaveCommand`.

### Crear — `tests\StickyDock.Tests`

- [ ] `TestInfrastructure\ManualScheduler.cs` — `Advance(TimeSpan)`, `IsPending`, cuenta de cancelaciones.
- [ ] `Docking\FakeBarSizeSource.cs`.
- [ ] `Docking\DockBarMetricsTests.cs`, `Docking\DockBarSizeSourceTests.cs`.
- [ ] `Interaction\DockInteractionControllerTests.cs`, `Interaction\GracePeriodTests.cs`.
- [ ] `Configuration\ConfigurationDocumentTests.cs`, `Configuration\GracePeriodPersistenceTests.cs`.
- [ ] `ViewModels\DockBarViewModelTests.cs`.

### Modificar

- [ ] `src\StickyDock.Core\Services\DockState.cs` — `Arming = 2`, `Collapsing = 3`.
- [ ] `src\StickyDock.Core\Configuration\StickyDockConfiguration.cs` — `CurrentSchemaVersion = 2`, `GracePeriodMilliseconds = 400`.
- [ ] `src\StickyDock.Core\Docking\IDockWindowHost.cs` — retirar `GetBarSize()`.
- [ ] `src\StickyDock.Core\Docking\DockPlacementCalculator.cs` — añadir `GetNearestEdge` y `GetFloatingBounds`; no tocar `GetDockedBounds` ni `ClampIntoWorkArea`.
- [ ] `src\StickyDock.Core\Docking\DockPlacementCoordinator.cs` — constructor con `IDockBarSizeSource` e `IConfigurationDocument`; `DragStarted`, `DragCompleted`, `NotifyBarSizeChanged`; persistencia vía `Update`; `ResolveFloating` usa `GetFloatingBounds`.
- [ ] `src\StickyDock.Core\ViewModels\DockBarViewModel.cs` — constructor con `IDockStateService` **e** `IDockRegionSink` (sin `DockBarMetrics`); `DockState` y los comandos de región que reenvían al puerto; **borrar** `IsHovered`, `TabOpacity`, `ScaleX`, `ScaleY` y `SlideFromX`/`SlideFromY`/`SlideMilliseconds`.
- [ ] `src\StickyDock.App\Docking\WpfDockWindowHost.cs` — borrar `GetBarSize()` y `UseActual`.
- [ ] `src\StickyDock.App\Bootstrapper.cs` — crear el documento con `StickyDockSettings.CreateStore` y registrarlo en lugar del registro inmutable; registrar `DockBarMetrics.Default` y `IDockBarSizeSource`; construir `DispatcherTimerScheduler` y `DockInteractionController` en lugar de `DockStateService`, y **registrar la misma instancia** bajo `IDockStateService` y bajo `IDockRegionSink` (el contenedor la dispondrá una vez por tipo registrado, de ahí que `Dispose` sea idempotente); al crear el coordinador en `SourceInitialized`, suscribir `IDockStateService.StateChanged → coordinator.NotifyBarSizeChanged()` (§7.7) y cablear `DragStarted`/`DragCompleted` al controlador; vaciar el documento en `Dispose`.
- [ ] `src\StickyDock.App\Views\MainWindow.xaml` — `RootPanel` **sin** `ClipToBounds`; `ClipPanel` con `ClipToBounds="True"` envolviendo solo `ContentPanel`; `ContentPanel` con `TranslateTransform`, la huella desplegada declarada y el fondo translúcido de E1; pestaña de tamaño de contenido **fuera** del recorte, sin `Margin`, `Panel.ZIndex="1"` y alineada según `DockEdge`; la ventana plegada **conserva** 120×36 (E1-CA4); *storyboards* de §8.3 (`MultiDataTrigger` por borde, fundido de salida, micro-efecto); `PointerRegionBehavior` en la pestaña y en el panel de la barra.
- [ ] `src\StickyDock.App\Views\MainWindow.xaml` — **borrar** de `TabBorderStyle` el `ScaleTransform` con `ScaleX`/`ScaleY` enlazados y cualquier `TabOpacity`: los sustituyen los *storyboards* de §8.3, no una propiedad del ViewModel.
- [ ] `src\StickyDock.App\Views\MainWindow.xaml.cs` — borrar los dos manejadores de *hover*; queda `InitializeComponent`, `MainViewModel` y el campo `DataContext`.
- [ ] `tests\StickyDock.Tests\Docking\FakeDockWindowHost.cs` — borrar `BarSize` y `GetBarSize`.
- [ ] `tests\StickyDock.Tests\Docking\DockPlacementCoordinatorTests.cs` — *fixture* con `FakeBarSizeSource` y documento real sobre `RecordingConfigurationStore`.
- [ ] `tests\StickyDock.Tests\Configuration\DockEdgePersistenceTests.cs` — el documento escrito pasa a `schemaVersion: 2` y el almacén de la prueba registra la migración.

### Borrar

- [ ] `src\StickyDock.Core\Services\DockStateService.cs` — sustituido por `DockInteractionController` (sin pruebas que lo referencien).

### Verificación final

- [ ] `dotnet build -c Release` y `-c Debug` sin errores **ni advertencias** (`TreatWarningsAsErrors`).
- [ ] `dotnet test` en verde con la suite ampliada; ninguna prueba con esperas reales de temporizador.
- [ ] `grep` de control en `src\StickyDock.Core`: ni `TabExpansionMargin`, ni `DllImport`, ni `nint`/`IntPtr`, ni `DispatcherTimer`, ni `ActualWidth`, ni `Storyboard`, ni `SlideFromX`/`SlideFromY`/`SlideMilliseconds`.
- [ ] `grep` de control en `src\StickyDock.App\Views`: ni `ScaleX`, ni `ScaleY`, ni `TabOpacity` (los sustituyen los *storyboards* por estado), ni temporizadores, ni lógica de estado.
- [ ] Verificación funcional manual: *hover* sostenido de ~200 ms → micro-efecto y despliegue; retirada → colapso tras ~400 ms; retirada antes de 200 ms → no ocurre nada; cambio de la gracia en el documento → efecto inmediato sin reiniciar.
- [ ] Verificación manual **M-05**: en los cuatro bordes, el despliegue entra desde el borde correcto, el micro-efecto **no** se recorta contra la ventana y la pestaña no se mueve; los literales del XAML coinciden con `DockBarMetrics` (60 = 96 − 36) e `InteractionTimings.SlideMilliseconds` (160).
- [ ] Verificación manual de **E1-CA4 en reposo** (H-7): con la barra colapsada la ventana mide **120×36**, un clic justo fuera de la pestaña no la activa y la pestaña no ha ganado superficie de captura.
- [ ] Medición de cierre: RAM privada comprometida < 42 MB y CPU < 1 % de un núcleo durante 30 s con la barra colapsada (RNF-01, RNF-02).
- [ ] `settings.json` de una instalación de E2 (versión 1) se abre, migra a la versión 2 y conserva el `dockEdge`.

---

## 11. Consecuencias

### Positivas

- **Los cinco CA de E3 se prueban sin abrir una ventana**: cuatro con el programador manual y el documento de configuración, y el quinto con la persistencia real sobre un almacén de prueba. Es la primera etapa del plan cuyos criterios temporales son aserciones.
- **D-01, D-04, D-07 y D-09 quedan cerrados por diseño, no por parche**: el tamaño no se mide (D-01), las escrituras se serializan y se pueden vaciar (D-04), hay un solo escritor (D-07) y el tamaño se re-resuelve cuando cambia por contenido (D-09).
- **Un solo escritor por preferencia** para el resto del plan: E6 no añadirá un tercer escritor, añadirá `Update` sobre el mismo documento.
- **Reloj y estado dejan de estar dispersos**: una ranura, cuatro estados, una rutina de reevaluación. La clase de defecto "dos temporizadores vivos a la vez" es inexpresable.
- **El code-behind vuelve a quedar vacío**: `MainWindow.xaml.cs` se reduce a `InitializeComponent` más las propiedades del *shell*, y por primera vez desde E1 desaparece el último manejador de eventos de la vista.
- **Coste en reposo cero y sin excepciones**: no hay sondeo, ni *hook* de ratón global, ni `SizeChanged`, ni animación permanente.
- **E4 y E7 se apoyan sin reescribir**: `DockRegion.Ribbon` y `NotifyBarSizeChanged()` son las dos costuras que E4 necesita, y el tamaño declarado es lo que E7 podrá instanciar por monitor.

### Negativas / deuda asumida

- **Los tamaños son declarados**: si el contenido de E4 o E5 pide más de 168×96, habrá que ajustar `DockBarMetrics` o recortar. El recorte no rompe ningún CA, pero sí puede ocultar contenido; se mitiga con la verificación manual de aspecto y con la matriz de `ExpandedBounds_StayInsideWorkArea`.
- **`ConfigurationDocument.Dispose` bloquea hasta 2 s**. Se prefiere un cierre determinista que vacía la preferencia a un `async void` en el apagado; si la escritura expira, se registra y la aplicación se cierra igual.
- **Los *storyboards* no tienen prueba unitaria y ahora contienen literales**. Se acepta desde E0 para toda la capa de vista. La política (cuándo) sigue en el controlador y el estado que dispara cada animación se decide fuera; lo que **no** puede decidirse fuera son el desplazamiento y la duración, porque `Timeline.From` y `Timeline.Duration` no admiten *binding* (P5.b). Esa duplicación de dos números se declara como D-14 y se verifica con la inspección de §10 y con M-05.
- **La ventana no tiene *hit-test* selectivo, y E1-CA4 depende de eso.** No hay `WM_NCHITTEST`, ni `HwndSource` filtrando, ni código equivalente: `MainWindow` tiene `Background="Transparent"` y WPF la considera *hit-testeable*, así que E1-CA4 se cumple **porque la ventana colapsada mide exactamente la pestaña** (120×36), no porque exista una franja inerte. Cualquier ampliación futura de la huella plegada —un margen, un asa más grande, una sombra, un `Padding` en la raíz— hará que esa superficie intercepte clics y **debe verificarse contra E1-CA4**. Se registra como D-15 y está en la lista de verificación de §10.
- **El micro-efecto cabe por construcción del `layout`, no por reserva de espacio.** La pestaña es un `Border` de tamaño de contenido (~58×27), no un elemento que llene la ventana; al 1,06 mide ~61×29 dentro de 120×36. La contrapartida es que el micro-efecto tiene poco recorrido visible: si en el futuro se quisiera un efecto mayor, no se puede resolver agrandando la huella sin romper E1-CA4 (D-15).
- **La sustitución parcial de ADR-002 es explícita**: `GetBarSize()` desaparece y el constructor del coordinador cambia, pero la observación del entorno, la geometría y el mapa de E2-CA quedan intactos (§5.5.1). Un ADR que reemplaza a otro sin decirlo es peor que uno que no lo reemplaza.
- **`PointerRegionBehavior` depende de los eventos de ratón de WPF sobre elementos**. Funciona porque las Cintas de E4 serán parte de la misma ventana; si alguna llegara a ser una ventana emergente propia, su región habría que registrarla aparte (anotado para E4).
- **El estado no se persiste**: la barra siempre arranca colapsada. Es lo que pide §4.2; se anota para que nadie lo interprete como un olvido.

### Impacto en otras áreas

- **E4 (Cintas)**: hereda `DockRegion.Ribbon`, `NotifyBarSizeChanged()` y la gracia cálida; debe añadir la región de cada Cinta al `PointerRegionBehavior`, la exclusividad de apertura (§4.4-02) en su propio ViewModel, y el largo real de la barra en `DockBarMetrics` o en el aviso. La pregunta Q-10 (§15) queda abierta para esa etapa.
- **E5 (`.lnk`)**: no se ve afectada; D3 garantiza que el asa de arrastre no roba los gestos de los accesos directos, que es la condición para que el clic ejecute y el arrastre interno reordene.
- **E6 (bandeja y preferencias)**: la ventana de configuración solo necesita leer y escribir `GracePeriodMilliseconds` a través de `IConfigurationDocument`; el controlador ya lo aplica al instante porque lo lee al armar. El menú de la bandeja puede usar `Expand`/`CollapseToTab`.
- **E7 (multimonitor)**: el documento es único y compartido, así que la réplica no necesitará sincronizar preferencias, solo contenido; el tamaño declarado permite instanciar una barra por monitor con su propio `DockBarSizeSource`.
- **E8 (rendimiento y entrega)**: hereda una etapa sin trabajo periódico. La medición de cierre de E3 es la primera comprobación real del presupuesto de 42 MB con la barra desplegada.

---

## 12. Decisiones de ingeniería sin traza a requerimiento

Se declaran para que no se confundan con requisitos. Ninguna de ellas cambia un CA; todas se ajustan en un punto.

| # | Decisión | Por qué no es requisito | Dónde se ajusta |
| --- | --- | --- | --- |
| 1 | Duración del *slide*: **160 ms** | §4.3-01 pide "animación fluida", sin cifra | `InteractionTimings.SlideMilliseconds` y el literal del XAML de §8.3 (D-14) |
| 2 | Micro-efecto: **escala 1.06 durante ~120 ms**, simultáneo con el *slide-in* | §4.3-02 pide "leve expansión"; HU02 CA #2 no da magnitud ni duración | `TabBorderStyle` de §8.3 y §4 (interpretación) |
| 3 | **Pestaña de 120×36** —huella plegada igual a la ventana de E1 y **sin margen**— y panel desplegado de **168×96** | RF03 y A7 exigen crecimiento y mínimo, sin valores; el micro-efecto no necesita holgura porque la pestaña no llena la ventana | `DockBarMetrics.Default` |
| 4 | La pestaña es el **único asa de arrastre** en ambos estados | Ni RF01 ni HU01 dicen por dónde se agarra una barra desplegada | `MainWindow.xaml` |
| 5 | La barra flotante crece hacia el **lado del borde más cercano** | Ningún RF trata el crecimiento de una barra flotante | `DockPlacementCalculator.GetNearestEdge` |
| 6 | Crecimiento **centrado** a lo largo del borde | Heredado de ADR-002 (ya declarado allí como ingeniería) | `GetDockedBounds`, sin cambios |
| 7 | El documento **coalesce** escrituras (*latest wins*) y `Dispose` espera hasta 2 s | §5.2-02 pide persistencia, no política de escritura | `ConfigurationDocument` |
| 8 | Valores numéricos del enum `DockState` y arranque siempre en `Tab` | El estado no se persiste; los números son estabilidad futura | `DockState`, `DockInteractionController` |
| 9 | La barra flotante se despliega con **fundido, sin dirección** | §4.3-01 describe el deslizamiento "desde el borde", que una barra flotante no tiene | Bloque de la barra flotante en §8.3 |
| 10 | El *slide* anima **contenido, no tamaño de ventana** | Es la única forma de mantener RNF-02 con `AllowsTransparency` | §8.2 |
| 11 | La salida es un **fundido de 120 ms**, no un desplazamiento invertido | §4.3-01 solo fija el despliegue; el repliegue no tiene forma prescrita | Bloque `Collapsing` de §8.3 |

---

## 13. Alternativas descartadas

- **Opción A de P1 (registro inmutable + servicio de escritura por preferencia)**: deja `SaveAsync` alcanzable y reabre D-07 en cuanto haya dos preferencias. Se reevaluaría solo si el documento dejara de ser compartido entre etapas, cosa que no va a ocurrir.
- **Opción C de P1 (clase mutable con eventos por propiedad)**: obliga a reescribir la persistencia y las pruebas de E0; no se reevalúa.
- **Opción A de P2 (medir y avisar desde `SizeChanged`)**: realimenta la propia escritura y mantiene D-01. Descartada.
- **Opción C de P2 (autosize del contenido)**: comportamiento dependiente del orden del *layout*, no determinista ni probable. Descartada.
- **Opción (b) de P3 (asa ampliada en la franja vacía)**: no sobrevive a E5 con la barra llena; reevaluable si el usuario encuentra la pestaña pequeña.
- **Opción (c) de P3 (toda la barra como asa)**: incompatible con los gestos de E5 por el `PreviewMouseLeftButtonDown` que marca el evento como manejado. No se reevalúa.
- **Opción (a) de P4.b (ampliar `IDockStateService` con las regiones)**: mezcla estado y entrada de puntero y obliga a todo consumidor del estado a implementar dos métodos que no le corresponden. Descartada.
- **Opción (c) de P4.b (`ICommand` publicados por el controlador)**: mete `System.Windows.Input` en el contrato de dominio y contradice la separación que E0 fijó. Descartada.
- **Opción A de P5 (`DispatcherTimer` en el ViewModel)**: ata el ViewModel a WPF y dispersa la política; contradice §5.2-01 en la práctica. Descartada.
- **Opción C de P4 (`async`/`await` + `CancellationTokenSource`)**: misma garantía con más superficie y peor prueba. Descartada.
- **Opción B de P5 (animación fotograma a fotograma desde el ViewModel)**: 60 Hz de notificaciones durante cada transición, contra RNF-02. Descartada.
- **Opción C de P5 (animación en el code-behind)**: §5.2-01 y E0-CA6. Descartada, ya pagada en H-E2-03.
- **Opción (a) de P5.b (`Storyboard` con `From`/`Duration` por *binding*)**: técnicamente imposible — `Timeline.From` y `Timeline.Duration` no son propiedades de dependencia, y el árbol de un `Style` además se congela. No es una alternativa, es el defecto que se corrige.
- **Opción (c) de P5.b (`Storyboard` construido en un *behavior* adjunto)**: traslada la animación a código sin volverla comprobable y duplica lo que un `DataTrigger` ya hace. Reevaluable solo si hiciera falta un valor calculado en tiempo de ejecución, cosa que no ocurre.
- **Desplazamiento de salida invertido (cuatro `MultiDataTrigger` más con `To` literal)**: no lo pide ningún CA y duplica la tabla de literales; la salida se resuelve con un fundido (§8.3, §12 #11). Reevaluable si el usuario pide explícitamente un repliegue deslizante.
- **Detección de puntero con *hook* global (§5.1-04 literal)**: incompatible con RNF-02 y sin criterio de aceptación que la exija (A6). Descartada y confirmada en §7.5.
- **Persistir el estado `Expanded`**: contradice §4.2 ("permanece en estado de reposo"), que describe la pestaña como estado natural al aparecer. Descartada.
- **Margen de holgura alrededor de la pestaña (`TabExpansionMargin = 4`)**: se retiró en la segunda revisión (H-7). Ampliaba la huella plegada a 128×44 —y con ella la superficie que intercepta clics en reposo, rompiendo E1-CA4 sin ningún mecanismo de *hit-test* que la hiciera inerte— y no hacía falta: la pestaña es de tamaño de contenido y el 1,06 cabe de sobra. Además dejaba el literal 60 sin derivación (96 − 44 = 52). No se reevalúa salvo que el producto gane *hit-test* selectivo (D-15).

---

## 14. Notas y pendientes

### Lo que el Director debe registrar en `docs/plan/ESTADO.md` (este ADR no lo modifica)

1. **ADR-003 aprobada por el usuario el 2026-10-28**, junto con la resolución de **Q-09** (opción 1: E3 entrega valor, validación, migración, persistencia y aplicación inmediata; la interfaz de configuración llega en E6). E3 abre contra este documento.
2. **Deudas que este ADR cierra cuando la implementación esté verificada** (no con el ADR): D-01, D-04, D-07 y D-09. Como en E2, el cierre debe basarse en el código y en las pruebas, no en el informe del implementador.
3. **N-01 no consta en el tablero**: el encargo del Director la usa como etiqueta del aviso de ADR-002 sobre la zona de arrastre. Debe registrarse como aviso resuelto (§3.3, opción a) para que no se pierda.
4. **Ampliación de superficie a registrar**: el esquema de persistencia sube a la versión 2 con migración. No lo pide ningún RF (AMP-01 solo dice "persistir la gracia"); lo exige el hecho de que E3-CA5 habla de persistencia entre ejecuciones y de que los `settings.json` existentes están en la versión 1.
5. **Afectación retroactiva a E1**: la pestaña debe alinearse según el borde (I3) y el `MainWindow.xaml` deja de tener `Width`/`Height` decorativos. La **huella plegada no cambia**: sigue siendo 120×36, la misma ventana de E1, y por eso E1-CA4 se conserva (§6.1, H-7). Es un ajuste visual de lo ya cerrado, del mismo tipo que AMP-03 en E2; la verificación manual M-04 de `ESTADO.md` (aspecto de la pestaña al 125 % y 150 %) debe repetirse tras E3.
6. **Sustitución parcial de ADR-002**: `IDockWindowHost.GetBarSize()` se retira y el constructor de `DockPlacementCoordinator` cambia. Debe registrarse con el mismo criterio que una ampliación de alcance, para que el ADR-002 no se lea como vigente en esos tres puntos (§5.5.1).

### Deuda nueva que este ADR deja declarada

| ID | Deuda | Etapa que debería abordarla |
| --- | --- | --- |
| D-11 | Los *storyboards* de despliegue y micro-efecto no tienen prueba automatizada; solo inspección y medición | E8 (documentación de arquitectura) |
| D-12 | `DockBarMetrics.Default` debe mantenerse coherente con el contenido real de la vista; hoy no hay comprobación automática de esa coherencia | E4 (cuando el contenido cambie el tamaño) |
| D-13 | Si en E4 alguna Cinta se implementa como ventana emergente, su región no llegará por `PointerRegionBehavior` y habrá que registrarla explícitamente | E4 |
| D-14 | Los literales del XAML (`±60` de desplazamiento y `0.16` de duración) duplican `DockBarMetrics` e `InteractionTimings` y ningún compilador comprueba la coherencia. Es el precio de P5.b: `Timeline.From` y `Timeline.Duration` no admiten *binding*. Se protege con la inspección de §10 y con M-05 | E8 (o la etapa que cambie `DockBarMetrics`) |
| D-15 | **La ventana no tiene *hit-test* selectivo.** `MainWindow` usa `Background="Transparent"` —*hit-testeable* en WPF— y no hay `WM_NCHITTEST` ni `HwndSource` filtrando, así que E1-CA4 se cumple porque la ventana colapsada mide exactamente la pestaña (120×36). Cualquier ampliación futura de la huella plegada (margen, asa, sombra, `Padding` en la raíz) hará que esa superficie intercepte clics y debe verificarse contra E1-CA4 | E8, y **obligatoria** para cualquier etapa que toque la huella plegada |

### Verificaciones manuales que E3 añade (no reproducibles en pruebas unitarias)

| ID | Verificación |
| --- | --- |
| M-05 | *Hover* sostenido y retirada: el micro-efecto se percibe antes de que la barra esté completamente desplegada, **no se recorta contra la ventana** en los cuatro bordes y el *slide* entra desde el borde correcto. Incluye comprobar que los literales del XAML (60 y 0.16 s) coinciden con `DockBarMetrics` e `InteractionTimings` |
| M-06 | La pestaña conserva su posición en pantalla al desplegar y al colapsar, en los cuatro bordes (I3) |
| M-07 | Aspecto de la pestaña y del contenido al 125 % y 150 % tras `PerMonitorV2` (repite M-04 de E2 ampliada) |
| M-08 | Un `settings.json` real de E2 (versión 1) se migra a la versión 2 conservando `dockEdge` y añadiendo `gracePeriodMs` |

### Preguntas que este ADR **no** abre

Ninguna pregunta de **requerimiento**. No hay contradicción nueva entre documentos, ningún RF queda reinterpretado y no se reduce alcance. Los valores de Q-03 (200 ms fijos y 300–500 ms de gracia) se aplican tal como se decidieron; A3, A4, A6, A7 y Q-08 no se tocan.

---

## 15. Preguntas abiertas al usuario

Se registran aquí para que el Director las traslade; **no bloquean la implementación de E3** salvo en lo que se indica.

### Q-09 — ¿Quién expone la configuración del temporizador de gracia?

- **Origen**: E3-CA5 exige que la gracia sea "configurable por el usuario dentro del rango 300–500 ms" y que su valor persista. La interfaz de configuración general es de E6 (§4.6-01, HU05 CA #1: la ventana se abre desde la bandeja).
- **Opciones**:
  1. **E3 entrega el valor, la validación, la persistencia y la aplicación inmediata; E6 expone la ventana. E3-CA5 se verifica sobre la configuración persistida y su efecto en el comportamiento, no sobre un control de interfaz.** *(recomendada)*
  2. E3 añade una ventana de configuración mínima con solo la gracia, y E6 la amplía. Duplica trabajo y adelanta una etapa cuyo alcance ya está definido.
  3. E3 deja la gracia fija en 400 ms y aplaza la configurabilidad a E6. Incumple E3-CA5 tal como está escrito.
- **Recomendación**: opción 1, con una nota para el `qa`: el criterio se comprueba (a) editando `settings.json` con 300, 400 y 500 ms y observando el tiempo de colapso, y (b) con las pruebas de migración, normalización y persistencia. Si el Director prefiere la lectura literal de "configurable por el usuario" como interfaz en E3, la opción 2 es la única compatible y hay que ampliar el alcance de la etapa antes de empezar.

### Q-10 — Al colapsar la barra, ¿se cierra la Cinta que estuviera abierta?

- **Origen**: §4.4 no lo dice. HU03 fija la exclusividad entre Cintas, no qué ocurre con una Cinta cuando la barra entera se oculta.
- **Opciones**:
  1. **Sí: el colapso cierra la Cinta abierta.** Coherente con que la Cinta pertenece al área interactiva de la barra (A4) y con que una Cinta abierta sin barra visible no tiene ancla. *(recomendada)*
  2. No: la Cinta se conserva y reaparece abierta en el siguiente despliegue. Deja una ventana flotante visible sin la barra, lo que contradice el propósito de RF02.
- **Recomendación**: opción 1, **decidida en E4**, no en E3. Este ADR deja la costura (`DockRegion.Ribbon` y `StateChanged`) para que E4 pueda implementarla sin tocar la máquina de estados.

### Q-11 — ¿El micro-efecto de expansión se emite en cada activación o solo la primera vez?

- **Origen**: §4.3-02 lo describe "durante el hover sostenido sobre la pestaña en reposo", sin distinguir entre la primera activación de la sesión y las siguientes.
- **Opciones**:
  1. **En cada activación.** Es la lectura literal y la que mantiene el *feedback* coherente. *(recomendada)*
  2. Solo la primera vez por sesión, para "no cansar". Añade estado sin respaldo en la especificación.
- **Recomendación**: opción 1. Si el usuario prefiere la 2, es un `if` sobre una bandera de sesión en el *storyboard* del estado `Arming` y no cambia ningún contrato.

---

*Fin del ADR-003.*






