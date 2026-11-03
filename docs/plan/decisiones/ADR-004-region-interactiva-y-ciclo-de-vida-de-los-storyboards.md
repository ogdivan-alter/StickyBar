# ADR-004: Fuente de verdad de la región interactiva y ciclo de vida de los storyboards

- **Estado**: **Aceptada** (aprobada por el usuario el 2026-11-01, sin cambios sobre la propuesta)
- **Fecha**: 2026-11-01
- **Etapa**: E3 (Hover, *slide-in* y temporizador de gracia) — corrección de los hallazgos **H-E3-05** y **H-E3-06**
- **Trazabilidad**: RF02 (§2, §4.2, §4.3), HU02 (CA #2, CA #3), §4.3-01/02/03, §5.1-04, §5.2-01 (MVVM), RNF-01/RNF-02 (Q-08, A5), A4 (área interactiva), A6 (sin *hook* global de ratón), E1-CA4, E3-CA1..E3-CA5, H-E3-05, H-E3-06, deuda D-14 y D-15
- **Sustituye parcialmente a**: **ADR-003** — la tabla de transiciones (§7.2), el contrato de `DockInteractionController` (§5.3) y los *storyboards* (§8.3). El alcance exacto de la sustitución está en **§5.6**. Todo lo demás de ADR-003 sigue vigente: `IConfigurationDocument`, `DockBarMetrics`, `IDockBarSizeSource`, `NotifyBarSizeChanged`, `DockRegion`, `IDockRegionSink`, `GracePeriod`, `InteractionTimings` y el reparto de la animación entre ViewModel y vista.
- **No modifica**: ADR-001 (stack) ni ADR-002 (docking y geometría).

> **Resumen en una frase**: la región interactiva deja de deducirse del **rectángulo de la ventana** y pasa a deducirse de **lo que la ventana pinta** —el `ContentPanel` reporta su propia región con `PointerRegionBehavior`, la raíz deja de pintar y de reportar, y la región se deduce exclusivamente de lo que se pinta—, se dota a la máquina de estados de una **matriz de presencia por estado** que descarta toda región imposible y purga las regiones de contenido al entrar en `Tab`, y se prohíbe `FillBehavior="HoldEnd"` en favor de **valores de reposo declarados** con `FillBehavior="Stop"`, de modo que ninguna animación pueda quedar retenida tapando el despliegue siguiente.

---

## 1. Contexto

### 1.1 Los dos hallazgos, medidos y reproducidos

E3 tiene los cinco criterios de aceptación con evidencia automatizada (suite 368/368, QA favorable el 2026-11-01), pero **el comportamiento end-to-end del despliegue repetido no se cumple**. El Director lo midió sobre el binario Release con una sonda de `SetCursorPos` + `GetWindowRect` + `WindowFromPoint` + color de píxel, y lo registró como los dos hallazgos siguientes.

**H-E3-05 — estado pegado: la barra deja de colapsar y de reaccionar al *hover*.**

| Ciclo | Secuencia del log | Cosas que no ocurren |
| --- | --- | --- |
| 1 | `Enter Tab` → `Arming` → 214 ms → `Expanded` → `Leave Tab` → 400 ms → `Collapsing` → **(fin)** | — |
| 2 | `Enter Tab` → `Arming` → `Expanded` → `Leave Tab` → 400 ms → `Collapsing` → **131-135 ms** → `Enter Bar` → `Expanded` | nunca llega `Pointer left region Bar` |

Los 131-135 ms coinciden con el final del fundido de salida (120 ms). Al desaparecer el último píxel pintado del panel, el sistema deja de entregar el puntero, WPF no genera ningún evento, y `WindowRegionReporter.Reconcile` —que decide con `WindowPlacement.GetBounds`, es decir, con el **rectángulo de la ventana**— vuelve a declarar «dentro» y eleva un `Enter(Bar)` que nadie ha provocado. ADR-003 §7.2 usa ese `Enter` para revertir `Collapsing → Expanded`; el `Leave` correspondiente ya no puede llegar nunca, así que la región `Bar` queda pegada al conjunto y la gracia no se vuelve a armar. A partir de ahí la barra no colapsa ni responde salvo acercándose a la pestaña.

**H-E3-06 — panel desplegado e invisible a partir del segundo ciclo.**

| Ciclo | Ventana | Píxel del panel en `(1840,500)` | `WindowFromPoint` |
| --- | --- | --- | --- |
| 1 | `96x168` | `195,195,195` (pintado) | la aplicación |
| 2 | `96x168` durante 1.400 ms | `15,15,15` (el fondo) | Chrome (*click-through*) |

Causa: `ContentPanelStyle` (`MainWindow.xaml` l. 47-166) anima `Opacity` desde `EnterActions` con `FillBehavior="HoldEnd"` y **sin un solo `ExitActions`**. El reloj del colapso queda retenido en 0 y prevalece sobre el del despliegue; el panel crece y se mueve, pero se pinta a opacidad cero. El comentario del propio XAML (l. 42-45) documenta el razonamiento que llevó al defecto: se eligió `HoldEnd` para que un fundido anterior no reapareciera, y el precio fue que un valor animado retenido no se puede deshacer desde la vista.

### 1.2 Hechos del código que condicionan el diseño

1. **La región de la barra vive en la raíz y se fabrica.** `RootPanel` lleva `input:WindowRegionReporter.Region="{x:Static interaction:DockRegion.Bar}"` (`MainWindow.xaml` l. 245-249) con `Background="Transparent"`, y `WindowRegionReporter` (`src\StickyDock.App\Input\WindowRegionReporter.cs`, 284 líneas medidas) sostiene ese estado con tres mecanismos: reafirmación en cada movimiento, reconciliación con el cursor real por `CursorProbe` tras cada cambio de tamaño o posición (l. 282-317) y un `WindowRegionTracker` que deduplica observaciones (`src\StickyDock.Core\Interaction\WindowRegionTracker.cs`, 51 líneas medidas).
2. **El estado reportado no depende de lo pintado.** `Reconcile` pregunta por el puntero dentro del **rectángulo de la ventana**, no dentro de una superficie pintada. En una ventana *layered* (`AllowsTransparency="True"`) un píxel con alfa 0 es *click-through*: el sistema no entrega allí ningún mensaje, de modo que la región puede afirmar «dentro» sobre una zona que el usuario no puede tocar y sobre la que WPF nunca volverá a hablar.
3. **La raíz es ancestro de la pestaña.** Por eso el comentario del XAML concluye que «no hay entrada que observar» y que la región debe colgar del rectángulo. Ese razonamiento era correcto para la raíz y es falso para el panel: el panel **no** es ancestro de la pestaña, así que entrar en él desde la pestaña sí produce un `MouseEnter` propio.
4. **`Enter(Tab)` durante `Collapsing` también revierte.** `EnterRegion` revierte cualquier región con `_state == DockState.Collapsing` (`DockInteractionController.cs` l. 81-87), así que volver a la pestaña mientras la ventana encoge la reabre. Con el reporter fabricando entradas, ese camino se dispara sin que el usuario haya tocado nada.
5. **La reversión es la única defensa del colapso, y por eso el `Leave` perdido es fatal.** El conjunto de regiones es la memoria del área interactiva (A4); si una región entra y no sale, la gracia no se arma jamás. No hay ninguna validación que compruebe que una región sigue siendo posible en el estado vigente.
6. **El colapso mantiene el panel pintado durante todo su transcurso.** `Visibility` solo pasa a `Collapsed` en `Tab`/`Arming` (l. 55-60); en `Collapsing` el panel sigue visible y solo se le baja la opacidad, y la ventana encoge cuando vence el temporizador de asentamiento (160 ms), no antes.
7. **Los cinco literales de animación no tienen comprobación.** El desplazamiento `±60` (= `96 − 36`) y las duraciones `0.16` / `0.12` viven solo en el XAML (deuda D-14), y la prueba que hoy los vigila —`tests\StickyDock.Tests\Views\MainWindowRegionLayoutTests.cs`— **afirma lo contrario de lo que hay que garantizar**: exige que la región cuelgue de la raíz, que la raíz pinte un fondo y que el panel **no** declare región. Es una prueba del diseño defectuoso.
8. **La huella plegada es exactamente la ventana de E1.** `DockBarMetrics.Default` declara `(120, 36, 168, 96)` y E1-CA4 se cumple porque la ventana colapsada mide 120×36; no existe *hit-test* selectivo (deuda D-15), así que **toda** superficie que intercepte el clic en reposo cuenta.
9. **El código de la vista está limpio.** `MainWindow.xaml.cs` solo tiene `InitializeComponent` y `MainViewModel`; la región entra por propiedades adjuntas. No hay code-behind que tocar en este ADR.

### 1.3 Por qué ADR-003 lo permitía

Tres decisiones de ADR-003, correctas por separado, se componen mal:

| Pieza de ADR-003 | Lo que se buscaba | Cómo se convirtió en el defecto |
| --- | --- | --- |
| A4 + D7: el área interactiva es la unión de regiones, y las regiones se **notifican** | que moverse entre la barra y sus cintas no colapse la barra (E3-CA3) | la unión es monótona: una región que entra y no sale no se puede retirar por ningún otro camino |
| §7.2: `Collapsing + Enter → Expanded` | reversibilidad del colapso (§4.3-03) | la reversión confía en que el `Enter` sea real; con el reporter no lo era |
| §8.3: `FillBehavior="HoldEnd"` en la salida | que el panel no reapareciera a media opacidad | el reloj retenido gana al reloj nuevo y nadie lo libera |

ADR-003 no se equivocó en la forma de la máquina: se equivocó en **de dónde vienen las entradas** y en **cómo termina una animación**. Este ADR corrige esas dos cosas y conserva lo demás.

### 1.4 Las preguntas que hay que resolver antes de escribir una línea

1. **¿Cuál es la fuente de verdad de la región interactiva?** (hoy: el rectángulo de la ventana, que es mentira).
2. **¿La reversión de `Collapsing` se conserva, se restringe o se retira?**
3. **¿Cómo se garantiza que ninguna región quede pegada, es decir, que el `Leave` no sea imprescindible?**
4. **¿Cómo termina una animación sin dejar valores retenidos que ganen a la siguiente?**
5. **¿Cómo se demuestra todo esto con pruebas y con una medición reproducible?**

---

## 2. Drivers

Por peso. Los cinco primeros son requisito; el sexto es ingeniería declarada.

1. **Corregir H-E3-05 y H-E3-06 sin reabrir E3-CA1..CA5.** El producto debe desplegarse, colapsar y volver a desplegarse **indefinidamente**, y verse en cada ciclo (§4.3-01, HU02 CA #2/#3).
2. **El comportamiento observable manda sobre la infraestructura.** Una región que no se puede tocar no debe existir; un panel que no se pinta no debe conservar estado. Es la regla de E1-CA4 (*«el resto de su área no intercepta el clic»*) aplicada hacia dentro.
3. **RNF-02: cero trabajo periódico y cero reloj retenido.** Ni *polling*, ni reconciliación por temporizador, ni `WH_MOUSE_LL`, ni `CompositionTarget.Rendering`, ni un `Clock` que siga pidiendo fotogramas después de terminar.
4. **RNF-01/Q-08: presupuesto de 42 MB de memoria privada comprometida.** Eliminar el reporter, el tracker, el sondeo de cursor y el `RegionReport` reduce superficie y estado; el diseño no debe añadir cachés ni estructuras por elemento.
5. **§5.2-01 y E0-CA6: la política en Core y servicios.** La matriz de presencia y las transiciones se prueban sin ventana; la vista solo declara *bindings*, estilos y propiedades adjuntas.
6. **(Ingeniería)** Que la corrección sea **reversible y barata de auditar**: dos hallazgos con una sola raíz conceptual —*lo que la ventana pinta no coincide con el rectángulo que el código cree que ocupa*—, de modo que el diseño nuevo se explique con una frase y se compruebe con un *grep*.

## 3. Opciones consideradas

### 3.1 P1 — Fuente de verdad de la región interactiva (H-E3-05)

#### Opción A — Conservar el reporter y corregirlo (reconciliar solo tras transiciones reales)

- **Descripción**: mantener `WindowRegionReporter` y el sondeo de cursor, pero reconciliar únicamente al **entrar y salir** de estados con cambio de huella, y no tras cada `SizeChanged`/`LocationChanged`.
- **Ventajas**: cambio pequeño; el estado actual se conserva.
- **Desventajas / riesgos**: **el defecto se atenúa, no se corrige**. Sigue preguntando por el rectángulo de la ventana, que en una ventana *layered* incluye píxeles que no reciben nada; sigue afirmando «dentro» sobre zonas *click-through* y sigue pudiendo fabricar un `Enter` sin `Leave` (basta con que el fundido acabe 1 ms antes que el asentamiento). Añade la exigencia de acertar con el conjunto exacto de disparadores, que es precisamente lo que hoy está mal.
- **Coste de adopción**: bajo, con el mismo riesgo.

#### Opción B — El `ContentPanel` reporta su propia región y la raíz deja de interceptar (ELEGIDA)

- **Descripción**: la región `Bar` pasa a `input:PointerRegionBehavior` sobre `ContentPanel`, igual que la pestaña ya lleva la suya. Se retira `Background="Transparent"` de `RootPanel` y con él toda propiedad `WindowRegionReporter`. Se borran `WindowRegionReporter`, `WindowRegionTracker`, `RegionReport`, `CursorProbe`, `NativeMethods.GetCursorPos` y `PixelRect.Contains` (cuyo único llamador era el reporter).
- **Ventajas**:
  - **El estado se deduce de lo que el usuario puede tocar**: si un píxel se pinta, WPF recibe el puntero y hay evento; si no se pinta, el sistema no entrega nada y no hay evento que pueda mentir. La invariante es una sola línea y se comprueba con el color de un píxel.
  - **Cero código propio**: se reutiliza `PointerRegionBehavior`, ya probado por la pestaña desde E3, y se eliminan 284 + 51 líneas de maquinaria (medidas) más el sondeo nativo.
  - Sin sondas, sin `Dispatcher.BeginInvoke` en `Input`, sin estado que reconciliar: **no hay ninguna vía por la que fabricar un `Enter`**.
- **Desventajas / riesgos**: hay que demostrar que el puntero nunca puede quedarse «sobre el panel» sin que la región lo sepa. Ver **§4.1** (corolario): no puede, porque el despliegue solo ocurre con el puntero sobre la pestaña y el panel crece **hacia dentro** desde la pestaña, que está anclada al borde exterior (ADR-003, invariante I2/I3). El otro riesgo es la asimetría de recorte durante el *slide* (el panel se traslada fuera del `ClipPanel`): aceptado y acotado a 160 ms, porque durante ese tiempo la zona descubierta no muestra nada.
- **Coste de adopción**: bajo-negativo (borra más de lo que añade).

#### Opción C — `WM_NCHITTEST` en la ventana para calcular la región con el *hit-test* real del sistema

- **Descripción**: interceptar `WM_NCHITTEST` en el `HwndSource` y derivar de ahí la región.
- **Ventajas**: sería la respuesta autoritativa del sistema, y de paso daría el *hit-test* selectivo que falta (D-15).
- **Desventajas / riesgos**: **`WM_NCHITTEST` no llega sobre píxeles con alfa 0 de una ventana *layered***: el sistema descarta el píxel antes de consultar a la ventana, que es exactamente la razón de ser del problema. Además añade P/Invoke y una ventana con comportamiento no estándar a una etapa que se cierra por comportamiento visual, y duplica una información que los eventos de ratón de WPF ya dan gratis. Reevaluable solo si E8 decide atacar D-15 de raíz.
- **Coste de adopción**: medio-alto y fuera de alcance.

### 3.2 P2 — Reversibilidad del `Collapsing` (H-E3-05)

#### Opción A — Retirar la reversión: `Collapsing` siempre termina en `Tab`

- **Descripción**: durante el asentamiento no se acepta ninguna entrada; la barra colapsa y se vuelve a desplegar con un *hover* completo.
- **Ventajas**: máquina más simple; ningún estado transitorio que revertir.
- **Desventajas / riesgos**: incumple el espíritu de §4.3-03 y de E3-CA3: un puntero que sale y vuelve en 200 ms vería la barra desplegarse, desaparecer y obligarle a esperar otros 200 ms, con el parpadeo que el temporizador de gracia existe para evitar.
- **Coste de adopción**: nulo.

#### Opción B — Conservar la reversión pero solo para regiones de contenido, y exigir que la entrada sea real (ELEGIDA)

- **Descripción**: `Collapsing + Enter(Bar | Ribbon) → Expanded` se conserva. `Enter(Tab)` durante `Collapsing` **no** revierte: el asentamiento termina en `Tab` y, si el puntero está sobre la pestaña, se arma una activación nueva de 200 ms (que es lo que `OnSettleElapsed` ya hace llamando a `Reevaluate`).
- **Ventajas**: conserva el comportamiento que el usuario espera (volver al panel lo reabre) y a la vez **elimina la vía por la que se colgaba**: la entrada sobre la pestaña ya no puede reabrir la barra sin *hover*, así que el ciclo `Collapsing → Enter(Tab) → Expanded` desaparece. Con P1-B, además, la entrada de `Bar` exige un píxel pintado: no se puede fabricar.
- **Desventajas / riesgos**: dos comportamientos distintos para `Enter` según la región. Se documenta en la tabla de transiciones (§6) y se prueba en ambos sentidos.
- **Coste de adopción**: bajo.

#### Opción C — Revertir con una gracia corta en lugar de inmediatamente

- **Descripción**: `Enter` durante `Collapsing` no revierte, sino que rearma la gracia.
- **Ventajas**: uniformidad conceptual.
- **Desventajas / riesgos**: durante el asentamiento la ventana ya está encogiendo; rearmar la gracia obligaría a crecer otra vez desde un tamaño intermedio, con dos transiciones visibles para un solo gesto. Además complica la máquina con un cuarto temporizador. Descartada.
- **Coste de adopción**: medio.

### 3.3 P3 — Cómo garantizar que ninguna región quede pegada

#### Opción A — Confiar en que el par `Enter`/`Leave` esté equilibrado

- **Descripción**: estado actual. Si los eventos son simétricos, el conjunto se vacía solo.
- **Ventajas**: cero código.
- **Desventajas / riesgos**: es la suposición que produjo H-E3-05. Un elemento que se oculta, se colapsa o se recorta **no genera `MouseLeave`** en WPF; la simetría no la garantiza el framework, y el estado pegado es indistinguible de «el usuario sigue dentro».
- **Coste de adopción**: nulo, y ya se pagó con una etapa reabierta.

#### Opción B — Matriz de presencia por estado, con descarte al entrar y purga al cambiar de estado (ELEGIDA)

- **Descripción**: un ayudante puro `DockRegionPresence` declara en qué estados puede existir cada región y qué regiones son «de contenido». `EnterRegion` descarta una región que no esté presente en el estado vigente; `SetState` **purga** las regiones no presentes en el estado nuevo **antes** de elevar `StateChanged`.
- **Ventajas**:
  - El estado pegado pasa a ser **inexpresable**: en `Tab` y `Arming` el conjunto no puede contener `Bar` ni `Ribbon`, se hayan recibido o no los `Leave`.
  - La regla coincide con lo que se pinta: `Bar`/`Ribbon` solo existen en `Expanded`/`Collapsing`, que son exactamente los estados en que el panel está visible.
  - Es una función pura de cuatro por cuatro, comprobable en una tabla.
- **Desventajas / riesgos**: hay que enumerar las regiones futuras de E4 (`Ribbon` ya está contemplada). Si E4 introduce un elemento visible en un estado nuevo, debe ampliar la matriz; queda anotado.
- **Coste de adopción**: bajo (un archivo de ~40 líneas y dos llamadas).

#### Opción C — Refrescar las regiones desde la vista tras cada transición (`Mouse.DirectlyOver`)

- **Descripción**: al cambiar de estado, la vista re-evalúa el elemento bajo el cursor y reporta la región.
- **Ventajas**: se ajusta a la realidad sin sondas nativas.
- **Desventajas / riesgos**: `Mouse.DirectlyOver` es estado de WPF, no del sistema: devuelve el último elemento que WPF consideró bajo el puntero, que en una ventana *layered* puede ser un elemento ya invisible. Y reintroduce el patrón «el estado se corrige a posteriori» que es la raíz del defecto. Descartada.
- **Coste de adopción**: bajo, con el mismo riesgo que P1-A.

### 3.4 P4 — Ciclo de vida de los *storyboards* (H-E3-06)

#### Opción A — Añadir `ExitActions` que devuelvan la opacidad a 1

- **Descripción**: mantener `HoldEnd` y, en la salida del disparador, animar de vuelta.
- **Ventajas**: cambio pequeño.
- **Desventajas / riesgos**: sigue habiendo un valor retenido mandando sobre la propiedad; solo se añade otro reloj que compite con él. El orden de preferencia entre relojes retenidos no es el orden de los `EnterActions`, así que el defecto puede reaparecer en la siguiente combinación. Descartada.
- **Coste de adopción**: bajo, con deuda.

#### Opción B — `FillBehavior="Stop"` en todo, con los valores de reposo declarados por estado (ELEGIDA)

- **Descripción**: `Opacity` de reposo es `1` como `Setter` del estilo; el valor `0` es el **valor de reposo del estado `Collapsing`**, declarado como `Setter` en su `DataTrigger`, no como resultado de una animación. Toda animación termina en el valor de reposo de su estado y usa `FillBehavior="Stop"`, de modo que el reloj se libera al terminar y no queda ningún valor animado mandando. La duración del fundido de salida pasa a `0:0:0.16`, igual que el asentamiento, para que el panel esté pintado mientras la ventana encoge y desaparezca justo cuando encoge.
- **Ventajas**:
  - **El valor mostrado deja de depender del historial de animaciones**: es la propiedad de reposo del estado vigente. Repetir el ciclo N veces da el mismo resultado, que es el criterio con el que se midió el defecto.
  - Con `Stop`, el `Clock` se detiene al terminar: no pide fotogramas (RNF-02) y no hay nada que liberar a mano.
  - Coherente con el resto del estilo, que ya declara `Visibility` por estado con `Setter`.
  - Al no fijar `From` en el despliegue, un despliegue que llegue con el fundido a medias (reversión) arranca desde la opacidad vigente y converge a 1 sin salto, porque el reloj nuevo sustituye al anterior (`SnapshotAndReplace`, el comportamiento por defecto de WPF al animar la misma propiedad desde otro `BeginStoryboard`).
- **Desventajas / riesgos**: el fundido de salida obliga a que el panel se pinte con opacidad decreciente durante 160 ms; es un degradado de opacidad en el compositor, sin *layout* ni notificaciones, y ya se paga hoy. Se verifica con la medición de reposo de cierre de etapa.
- **Coste de adopción**: bajo.

#### Opción C — Animar un `VisualBrush`/`Effect` en lugar de `Opacity`

- **Descripción**: sustituir el fundido por una capa compuesta.
- **Ventajas**: ninguna frente a B.
- **Desventajas / riesgos**: más coste de composición y fuera de alcance. Descartada.

### 3.5 P5 — Verificación

| Opción | Veredicto |
| --- | --- |
| **A — Conservar `MainWindowRegionLayoutTests` y añadir pruebas del controlador** | Descartada: la prueba estructural **afirma lo contrario** (región en la raíz, raíz con fondo, panel sin región). Dejarla sería blindar el defecto. |
| **B — Sustituirla por una prueba de contrato de los *storyboards* y de la estructura, derivando los literales de Core, y añadir pruebas de la secuencia real sobre el controlador (ELEGIDA)** | Elegida: convierte D-14 en una comprobación de máquina, ata la estructura de la vista a la invariante «paint ⇒ hit-test ⇒ region» y reproduce H-E3-05 como aserciones de estado. |
| **C — Solo verificación manual con la sonda** | Descartada: no sobrevive a un refactor y no reproduce la secuencia con la precisión del reloj manual. La sonda **se conserva**, pero como evidencia de cierre (§9.5), no como única garantía. |

---

## 4. Decisión

Se adoptan las opciones marcadas como ELEGIDAS: **P1-B, P2-B, P3-B, P4-B y P5-B**. Se concretan en cinco decisiones.

### 4.1 D1 — La región interactiva se deduce de lo que se pinta, no del rectángulo de la ventana

`ContentPanel` declara la región `Bar` con `input:PointerRegionBehavior`, exactamente igual que `TabBorder` declara la suya desde E3. Se retira `Background="Transparent"` de `RootPanel` y con él las tres propiedades `WindowRegionReporter` de la raíz. Se eliminan los tipos que sostenían el estado fabricado: `WindowRegionReporter`, `WindowRegionTracker`, `RegionReport`, `CursorProbe`, `NativeMethods.GetCursorPos` (con sus estructuras `POINT`/`POOL` si quedan sin uso) y `PixelRect.Contains`, cuyo único llamador era `Reconcile`.

**La invariante del diseño es una sola y es comprobable con el color de un píxel:**

> **paint ⇒ hit-test ⇒ region**, y su recíproca útil: **no paint ⇒ no input ⇒ no region**.

**Corolario que cierra el caso D-E3-01** (el que justificaba la sonda): un píxel pintado no puede aparecer bajo un puntero quieto sin que ese píxel venga del panel que crece. Y el panel **no puede crecer bajo un puntero quieto** sin que ese puntero estuviera antes sobre el panel, porque:

1. El despliegue solo ocurre cuando el puntero lleva 200 ms sobre la **pestaña** (E3-CA1); si sale antes, la activación se cancela (E3-CA4). Marcharse de la pestaña impide desplegar.
2. La pestaña está anclada al borde exterior y **no se mueve** al desplegar (ADR-003, invariantes I2 e I3), y el panel crece **hacia dentro de la pantalla** desde ese borde (ADR-003 §6.2: el rectángulo acoplado conserva el anclaje).
3. Por tanto, en el instante del despliegue el puntero está sobre la pestaña, y la zona que el panel ocupa al crecer es la que queda **por dentro**, que el puntero no ocupa.

Es decir: el escenario «puntero quieto sobre un panel que crece bajo él» **no puede darse**, y no hace falta ninguna sonda que lo cubra. El único caso en que el puntero puede acabar sobre el panel sin haber entrado en él es el *slide* de 160 ms (el contenido se traslada, no crece) y ese caso no requiere región: la zona descubierta durante el traslado no muestra nada, y al asentarse el contenido bajo el puntero lo que hay debajo es el panel, que ya reportó su entrada al hacerse visible si el puntero estaba dentro de su rectángulo final.

### 4.2 D2 — La reversión del colapso se conserva, pero solo sobre contenido y solo con entrada real

| Estado | Entrada recibida | Resultado | Motivo |
| --- | --- | --- | --- |
| `Collapsing` | `Enter(Bar)` / `Enter(Ribbon)` | `Expanded`, cancelando el asentamiento | el usuario ha vuelto a poner el puntero sobre el panel que se está desvaneciendo; §4.3-03 pide no cerrar en falso |
| `Collapsing` | `Enter(Tab)` | **sin cambio**: el asentamiento termina en `Tab` y, si el puntero sigue en la pestaña, se arma una activación nueva | volver a la pestaña no es «volver a la barra»: reabrirla sin *hover* es justo el ciclo que se colgaba |

`Enter(Tab)` durante `Collapsing` no revierte **aunque el asentamiento termine en `Tab` con la pestaña ya activa**, porque en ese momento la rama `Tab` de `Reevaluate` arma la activación de 200 ms en lugar de desplegar de inmediato. El usuario ve exactamente lo que HU02 CA #2 describe: pestaña, micro-efecto y despliegue.

### 4.3 D3 — Ninguna región puede quedar pegada: matriz de presencia y purga

`DockRegionPresence` (nuevo, puro, en Core) declara dos hechos:

- **Qué regiones son de contenido**: `Bar` y `Ribbon`. `Tab` no lo es; `None` tampoco.
- **En qué estados puede existir cada región**:

| Región | `Tab` | `Arming` | `Expanded` | `Collapsing` |
| --- | :---: | :---: | :---: | :---: |
| `None` | — | — | — | — |
| `Tab` | ✔ | ✔ | ✔ | ✔ |
| `Bar` | — | — | ✔ | ✔ |
| `Ribbon` | — | — | ✔ | ✔ |

La tabla **no es una preferencia estética: es la traducción de `Visibility`**. El panel (y con él las cintas de E4) está `Collapsed` en `Tab` y `Arming` y visible en `Expanded` y `Collapsing`; la pestaña está siempre visible. De ahí que la regla «una región solo existe en los estados en que su elemento se pinta» se cumpla por construcción.

Se aplica en dos puntos y solo dos:

1. **`EnterRegion` descarta lo imposible.** Si `IsPresent(region, State)` es falso, la entrada se ignora (con traza de depuración). Es la red que atrapa un `MouseEnter` que llegue tarde, un elemento que se oculte entre el evento y su procesamiento o una región mal declarada por E4.
2. **`SetState` purga lo que deja de existir, antes de elevar `StateChanged`.** Al entrar en `Tab` se retiran `Bar` y `Ribbon`. Sin esta purga, una región de contenido que no recibió su `Leave` sobreviviría a `Tab` y, al volver a `Expanded`, impediría armar la gracia: el síntoma exacto de H-E3-05. Con la purga, **el `Leave` deja de ser imprescindible**: el estado es la autoridad.

`DockRegion` y `IDockRegionSink` **no cambian**. `DockInteractionController` sigue implementando `IDockStateService` e `IDockRegionSink` (ADR-003, D7 intacta). No se abre ninguna pregunta de contrato.

### 4.4 D4 — Los valores de reposo se declaran; ninguna animación los retiene

Tres reglas, aplicables a todo *storyboard* de la vista:

- **R1 — `FillBehavior="HoldEnd"` queda prohibido.** Todo *storyboard* usa `Stop`.
- **R2 — el valor en reposo de una propiedad es el del estado vigente y se declara con `Setter`.** `Opacity = 1` en el estilo; `Opacity = 0` en el `DataTrigger` de `Collapsing`; `Visibility = Collapsed` en `Tab` y `Arming`. Toda animación termina en el valor de reposo de su estado, de modo que al liberar el reloj no hay salto.
- **R3 — ninguna animación fija `From` salvo que el valor inicial sea distinto del de reposo de su propio estado.** El despliegue conserva `From="±60"` (el contenido viene de fuera, no de la opacidad). El fundido de entrada **no** fija `From`: parte de la opacidad vigente, que es 0 si viene de `Tab` (invisible) o un valor intermedio si viene de una reversión, y converge a 1 sin salto.

Consecuencia aritmética: el fundido de salida pasa de `0:0:0.12` a **`0:0:0.16`**, igual que el asentamiento (`InteractionTimings.SlideMilliseconds`). Así el panel está pintado durante todo el `Collapsing` y se apaga justo cuando la ventana encoge: no queda ninguna superficie pintada fuera de la huella nueva.

El micro-efecto de la pestaña (`Arming`) conserva `To="1.06"`, `0:0:0.06` y `AutoReverse` —escala y vuelve, terminando en 1, que es su valor de reposo— y pasa a `Stop`, con lo que **se reproduce en cada activación** (Q-11, opción 1) en lugar de depender de un valor retenido.

### 4.5 D5 — Verificación: contrato de los *storyboards*, matriz de presencia y secuencia real

- **Se sustituye** `tests\StickyDock.Tests\Views\MainWindowRegionLayoutTests.cs` —cuyas aserciones están invertidas— por `tests\StickyDock.Tests\Views\MainWindowStoryboardContractTests.cs`, que comprueba: `Bar` declarada **en `ContentPanel`** y **no** en `RootPanel`; `RootPanel` **sin** `Background` y sin ninguna propiedad `WindowRegionReporter` en todo el XAML; `TabBorder` con su región y su asa de arrastre; ausencia de `HoldEnd`; que **todo** `Storyboard` tiene `FillBehavior="Stop"`; que ninguna animación de `Opacity` fija un `From` cuyo valor sea 0.3; que el desplazamiento literal de cada borde vale `|DockBarMetrics.Default.ExpandedThickness − CollapsedThickness|` deducido de **Core**, y que las duraciones valen `InteractionTimings.SlideMilliseconds` (lo que convierte D-14 en una comprobación de máquina).
- **Se añade** `tests\StickyDock.Tests\Interaction\DockRegionPresenceTests.cs`: las 16 combinaciones de la matriz y `IsContent` para las cuatro regiones.
- **Se añaden pruebas de la secuencia real** en `DockInteractionControllerTests`: entrada de `Bar` ignorada en `Tab` y en `Arming`, reversión desde `Collapsing` con `Bar`, **no** reversión con `Tab` seguida de redespliegue tras el *hover*, purga de las regiones de contenido al asentar, y entradas repetidas de una región activa que no arman ni cancelan la ranura.
- **Se borra** `tests\StickyDock.Tests\Interaction\WindowRegionTrackerTests.cs` (su sujeto desaparece).
- **Se conserva la sonda del Director** como evidencia de cierre, ampliada a tres ciclos (§9.5).

## 5. Contratos y cambios concretos

Nombres definitivos, en inglés, y comentarios XML en inglés. Todo lo que no se cita aquí **no cambia** respecto de ADR-003.

### 5.1 Módulo comparativo de la raíz

```csharp
// src\StickyDock.Core\Interaction\DockRegionPresence.cs  (new file)
namespace StickyDock.Core.Interaction;

/// <summary>
/// Declares which regions can exist in which state (ADR-004, D3). It is the single translation of the
/// <c>Visibility</c> the view declares for each element: a region may only be active while its element paints.
/// </summary>
/// <remarks>
/// This is what makes a stuck region unrepresentable. The pointer events of WPF are not guaranteed to be symmetric —an
/// element that is hidden, collapsed or clipped never raises its own exit—, so the machine cannot trust an exit to make
/// the interaction area shrink back. Instead the state is the authority: an impossible region is dropped on entry and
/// purged when the state changes. Pure and stateless, so the table is verified without a window.
/// </remarks>
public static class DockRegionPresence
{
    /// <summary>
    /// Gets a value indicating whether <paramref name="region"/> belongs to the content that is only painted while the
    /// bar is deployed. Content regions are purged when the bar leaves the deployed footprint.
    /// </summary>
    /// <param name="region">Region to classify.</param>
    public static bool IsContent(DockRegion region) => region is DockRegion.Bar or DockRegion.Ribbon;

    /// <summary>
    /// Gets a value indicating whether <paramref name="region"/> can be active while the bar is in
    /// <paramref name="state"/>.
    /// </summary>
    /// <param name="region">Region the pointer would be in.</param>
    /// <param name="state">State of the bar at that moment.</param>
    /// <returns>
    /// <see langword="true"/> for the tab in the four states; for the bar and the ribbons only in
    /// <see cref="DockState.Expanded"/> and <see cref="DockState.Collapsing"/>; never for
    /// <see cref="DockRegion.None"/>.
    /// </returns>
    public static bool IsPresent(DockRegion region, DockState state)
        => region switch
        {
            DockRegion.Tab => true,
            DockRegion.Bar or DockRegion.Ribbon
                => state is DockState.Expanded or DockState.Collapsing,
            _ => false,
        };
}
```

`DockRegionPresence` no necesita prueba de concurrencia ni estado: es una función total de `(DockRegion, DockState)`.

### 5.2 Cambios en `DockInteractionController`

```csharp
// src\StickyDock.Core\Interaction\DockInteractionController.cs  (amendments; the rest of the class is unchanged)
public sealed class DockInteractionController : IDockStateService, IDockRegionSink, IDisposable
{
    /// <inheritdoc />
    public void EnterRegion(DockRegion region)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (region == DockRegion.None)
        {
            return;
        }

        // ADR-004 D3: a region whose element does not paint in this state cannot be entered. The rule is what makes a
        // late or duplicated pointer event harmless, and it is the only guard that does not depend on a matching exit.
        if (!DockRegionPresence.IsPresent(region, _state))
        {
            _logger.Debug($"Ignored region {region}: it is not present in state {_state}.");
            return;
        }

        if (!_regions.Add(region))
        {
            return;
        }

        _logger.Debug($"Pointer entered region {region}.");

        // ADR-004 D2: only the content reverses the slide-out. Coming back to the tab is not coming back to the bar:
        // the settle completes and the hover of the tab —200 ms, E3-CA1— starts a new activation.
        if (_state == DockState.Collapsing && DockRegionPresence.IsContent(region))
        {
            _scheduler.Cancel();
            _pending = PendingTimer.None;
            SetState(DockState.Expanded);
            return;
        }

        if (_state == DockState.Collapsing)
        {
            // The tab entered while the bar slides out: nothing to decide, the settle owns the transition.
            return;
        }

        Reevaluate();
    }

    private void SetState(DockState newState)
    {
        if (_state == newState)
        {
            return;
        }

        _state = newState;

        // ADR-004 D3: the content regions stop existing the moment the bar leaves the deployed footprint, whether or
        // not their exit arrived. Purging before the event leaves every subscriber —and every reevaluation the event
        // triggers— looking at a set that is already consistent with the new state.
        PurgeAbsentRegions();

        _logger.Debug($"Dock state changed to {newState}.");
        StateChanged?.Invoke(this, new DockStateChangedEventArgs(newState));
    }

    private void PurgeAbsentRegions()
    {
        foreach (var region in _regions.Where(region => !DockRegionPresence.IsPresent(region, _state)).ToArray())
        {
            _regions.Remove(region);
            _logger.Debug($"Dropped region {region}: it is not present in state {_state}.");
        }
    }
}
```

**La rama `Arming` de `Reevaluate` se simplifica** (ADR-003 §7.3, rama «`Leave(Tab)` con otra región activa»), porque pasa a ser **inalcanzable**: con D3 no puede haber `Bar` ni `Ribbon` en `Arming`, ya que su elemento no se pinta.

```csharp
        if (_state == DockState.Arming)
        {
            if (_regions.Contains(DockRegion.Tab))
            {
                // The activation keeps running; the pointer never left the tab.
                return;
            }

            // The only region that can exist in Arming is the tab, and it is gone: cancel and go back (E3-CA4).
            CancelPendingTimer();
            SetState(DockState.Tab);
            return;
        }
```

`Expand()`, `CollapseToTab()`, `BeginDrag()`, `EndDrag()`, `Dispose()`, `OnActivationElapsed`, `OnGraceElapsed`, `OnSettleElapsed` y `Arm` **no cambian**. `EndDrag` sigue llamando a `Reevaluate` con el conjunto vigente, que ya es consistente por la purga.

### 5.3 Cambios en la vista

```xml
<!-- src\StickyDock.App\Views\MainWindow.xaml -->

<!-- ADR-004 D1: the root no longer paints and no longer reports. Nothing of WindowRegionReporter survives in the
     XAML: the region is deduced from what is painted, and a transparent pixel of a layered window never receives the
     pointer. The window keeps its own Background="Transparent" on purpose: it is what makes the window hit-testable
     and it is not part of this decision. -->
<Grid x:Name="RootPanel">
    <Grid x:Name="ClipPanel" ClipToBounds="True">
        <!-- ADR-004 D1: the panel reports its own region, exactly as the tab always did. Its own entry is real: it is
             not an ancestor of the tab, so moving from the tab into the panel raises MouseEnter. -->
        <Border x:Name="ContentPanel"
                Style="{StaticResource ContentPanelStyle}"
                input:PointerRegionBehavior.Region="{x:Static interaction:DockRegion.Bar}"
                input:PointerRegionBehavior.EnterCommand="{Binding EnterRegionCommand}"
                input:PointerRegionBehavior.LeaveCommand="{Binding LeaveRegionCommand}">
            <Border.RenderTransform>
                <TranslateTransform X="0" Y="0" />
            </Border.RenderTransform>
        </Border>
    </Grid>

    <Border x:Name="TabBorder" Panel.ZIndex="1" ... />
</Grid>
```

Cuatro cambios y ninguno más:

1. `RootPanel` pierde `Background="Transparent"` y sus tres propiedades `WindowRegionReporter`.
2. `ContentPanel` gana `input:PointerRegionBehavior.Region="{x:Static interaction:DockRegion.Bar}"` y sus dos comandos.
3. `TabBorder` no cambia.
4. `MainWindow.xaml.cs` no cambia: ya no contiene manejadores.

### 5.4 *Storyboards* corregidos

```xml
<!-- ADR-004 D4, rule R2: the resting value of the opacity is declared per state, not left to the last animation. -->
<Style TargetType="{x:Type Border}" x:Key="ContentPanelStyle">
    <Setter Property="Opacity" Value="1" />
    <Setter Property="Visibility" Value="Visible" />
    <Style.Triggers>
        <DataTrigger Binding="{Binding DockState}" Value="{x:Static coreServices:DockState.Tab}">
            <Setter Property="Visibility" Value="Collapsed" />
        </DataTrigger>
        <DataTrigger Binding="{Binding DockState}" Value="{x:Static coreServices:DockState.Arming}">
            <Setter Property="Visibility" Value="Collapsed" />
        </DataTrigger>

        <!-- Deployed against the top edge. ADR-004 D4, rule R3: the offset keeps a From —the content really comes from
             outside—, the opacity does not: it starts wherever it is and converges to its resting value. -->
        <MultiDataTrigger>
            <MultiDataTrigger.Conditions>
                <Condition Binding="{Binding DockState}" Value="{x:Static coreServices:DockState.Expanded}" />
                <Condition Binding="{Binding DockEdge}" Value="{x:Static coreDocking:DockEdge.Top}" />
            </MultiDataTrigger.Conditions>
            <MultiDataTrigger.EnterActions>
                <BeginStoryboard>
                    <Storyboard FillBehavior="Stop">
                        <DoubleAnimation
                            Storyboard.TargetProperty="(UIElement.RenderTransform).(TranslateTransform.Y)"
                            From="-60" To="0" Duration="0:0:0.16" />
                        <DoubleAnimation Storyboard.TargetProperty="Opacity" To="1" Duration="0:0:0.16" />
                    </Storyboard>
                </BeginStoryboard>
            </MultiDataTrigger.EnterActions>
        </MultiDataTrigger>

        <!-- The same block for Bottom (From="60", Y) and for Left/Right (From="-60"/"60", X). -->

        <!-- Floating bar: no edge to come from, so only the fade. -->
        <!-- ... Opacity To="1" Duration="0:0:0.16" ... -->

        <!-- ADR-004 D4: the ending value of the slide-out is the resting value of Collapsing, declared here, and the
             fade lasts as long as the settle (0.16 s = InteractionTimings.SlideMilliseconds) so that the panel is
             painted while the window shrinks and goes dark exactly when the window has its folded footprint. -->
        <DataTrigger Binding="{Binding DockState}" Value="{x:Static coreServices:DockState.Collapsing}">
            <Setter Property="Opacity" Value="0" />
            <DataTrigger.EnterActions>
                <BeginStoryboard>
                    <Storyboard FillBehavior="Stop">
                        <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                         From="1" To="0" Duration="0:0:0.16" />
                    </Storyboard>
                </BeginStoryboard>
            </DataTrigger.EnterActions>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

y, en `TabBorderStyle`, el micro-efecto:

```xml
<!-- ADR-004 D4: Stop instead of HoldEnd. The animation returns to its base value (1) through AutoReverse, so releasing
     the clock changes nothing and the effect replays on every activation (Q-11, option 1). -->
<DataTrigger Binding="{Binding DockState}" Value="{x:Static coreServices:DockState.Arming}">
    <DataTrigger.EnterActions>
        <BeginStoryboard>
            <Storyboard FillBehavior="Stop">
                <DoubleAnimation
                    Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleX)"
                    To="1.06" Duration="0:0:0.06" AutoReverse="True" />
                <DoubleAnimation
                    Storyboard.TargetProperty="(UIElement.RenderTransform).(ScaleTransform.ScaleY)"
                    To="1.06" Duration="0:0:0.06" AutoReverse="True" />
            </Storyboard>
        </BeginStoryboard>
    </DataTrigger.EnterActions>
</DataTrigger>
```

**Tabla de literales** (la que vigila `MainWindowStoryboardContractTests`):

| Borde | Eje | `From` | `To` | Fuente del `From` |
| --- | --- | --- | --- | --- |
| `Top` | Y | `−60` | `0` | `−(ExpandedThickness − CollapsedThickness)` |
| `Bottom` | Y | `+60` | `0` | `+(ExpandedThickness − CollapsedThickness)` |
| `Left` | X | `−60` | `0` | idem |
| `Right` | X | `+60` | `0` | idem |
| flotante | — | — (sin desplazamiento) | — | — |

| Animación | Duración literal | Origen |
| --- | --- | --- |
| Despliegue | `0:0:0.16` | `InteractionTimings.SlideMilliseconds` |
| Fundido de salida | `0:0:0.16` | `InteractionTimings.SlideMilliseconds` (antes `0:0:0.12`) |
| Micro-efecto | `0:0:0.06` con `AutoReverse` | decisión de ingeniería (ADR-003 §12 #2) |

### 5.5 Tipos y miembros que desaparecen

| Archivo | Qué se retira | Quién lo usaba |
| --- | --- | --- |
| `src\StickyDock.App\Input\WindowRegionReporter.cs` | **Archivo completo** | la raíz en XAML |
| `src\StickyDock.Core\Interaction\WindowRegionTracker.cs` | **Archivo completo**, con `RegionReport` | el reporter |
| `src\StickyDock.Interop\Win32\CursorProbe.cs` | **Archivo completo** | el reporter |
| `src\StickyDock.Interop\Win32\NativeMethods.cs` | `GetCursorPos` y las estructuras que quedaran solo para él | `CursorProbe` |
| `src\StickyDock.Core\Docking\DockingGeometry.cs` | `PixelRect.Contains(int, int)` | `Reconcile` |
| `tests\StickyDock.Tests\Interaction\WindowRegionTrackerTests.cs` | **Archivo completo** | `WindowRegionTracker` |
| `tests\StickyDock.Tests\Views\MainWindowRegionLayoutTests.cs` | **Archivo completo**, sustituido | la estructura anterior |

Ninguno tiene otro consumidor: la comprobación está hecha sobre el árbol (`grep` sin resultados fuera de los archivos listados).

### 5.6 Sustitución parcial de ADR-003

Lo que cambia de ADR-003, y nada más:

| Elemento de ADR-003 | Cómo queda | Motivo |
| --- | --- | --- |
| §7.2, fila `Collapsing` + `Enter(cualquiera)` | Se parte en dos: `Enter(Bar \| Ribbon) → Expanded`; `Enter(Tab) → sin cambio` | D2: la pestaña no puede reabrir la barra sin *hover* |
| §7.2, fila `Arming` + `Leave(Tab)` con otra región activa | **Retirada**: inalcanzable | D3: en `Arming` no puede haber región de contenido |
| §7.2, fila `Tab`/`Arming`/`Expanded`/`Collapsing` con `Expand()`/`CollapseToTab()` | Sin cambios | no se tocan |
| §5.3, contrato de `DockInteractionController` | Se añade la guarda de presencia en `EnterRegion` y la purga en `SetState`; `Expand`, `CollapseToTab`, `BeginDrag`, `EndDrag`, `IsDragging`, `ActiveRegions`, `Dispose` y los eventos **no cambian** | D3 |
| §5.3, `DockRegion`, `IDockRegionSink`, `IDockStateService`, `GracePeriod`, `InteractionTimings`, `ISingleShotScheduler` | **Sin cambios** | no hace falta tocar el contrato |
| §8.2, estructura de la vista | `RootPanel` deja de declarar región y de pintar; `ContentPanel` declara la región `Bar` | D1 |
| §8.3, *storyboards* | `HoldEnd` → `Stop` en los seis; `Opacity` de reposo por estado; sin `From="0.3"`; fundido de salida `0:0:0.12` → `0:0:0.16`; micro-efecto con `Stop` | D4 |
| §9, mapa E3-CA → prueba | Se conserva entero; se le **añade** el mapa de los dos hallazgos (§8) | D5 |
| §14, deudas D-11/D-14/D-15 | D-14 pasa a comprobación de máquina (D5); D-15 **no cambia**: quitar el `Background` de `RootPanel` no altera el *hit-test* de la ventana, porque la `Window` conserva `Background="Transparent"` y el alfa del píxel sigue decidiendo qué se entrega | D5 |

Lo que **no** cambia y sigue vigente de ADR-003: `IConfigurationDocument` y el escritor único (D1 de ADR-003), `DockBarMetrics` e `IDockBarSizeSource` (D2), la pestaña como único asa (D3), los cuatro estados (D4), la animación declarativa en la vista (D5), el esquema v2 (D6), `IDockRegionSink` (D7), la sustitución parcial de ADR-002 (§5.5.1) y las decisiones de ingeniería de §12 salvo el literal del fundido de salida.

---

## 6. Tabla de transiciones corregida

Vigente en lugar de ADR-003 §7.2. La columna «Purga» indica qué regiones retira `SetState` al entrar en el estado destino.

| Estado | Evento | Estado siguiente | Acción | Purga |
| --- | --- | --- | --- | --- |
| `Tab` | `Enter(Tab)` | `Arming` | `Schedule(Activation)`; el micro-efecto arranca por `DataTrigger` | ninguna |
| `Tab` | `Enter(Bar \| Ribbon)` | `Tab` | **descartado** (región no presente): no entra en el conjunto | — |
| `Arming` | `Timer(Activation)` | `Expanded` | eleva `StateChanged` → la App crece → arranca el *slide-in* | ninguna |
| `Arming` | `Leave(Tab)` | `Tab` | `Cancel()` (E3-CA4) | ninguna |
| `Arming` | `Enter(Bar \| Ribbon)` | `Arming` | **descartado** (región no presente) | — |
| `Expanded` | regiones vacías | `Expanded` | `Schedule(Grace)`; no cambia de estado (E3-CA2) | ninguna |
| `Expanded` | `Enter(cualquiera)` con gracia pendiente | `Expanded` | `Cancel()`: la gracia se rearma la próxima vez (E3-CA3) | ninguna |
| `Expanded` | `Enter(Bar \| Ribbon)` | `Expanded` | `Cancel()`, sin transición | ninguna |
| `Expanded` | `Timer(Grace)` | `Collapsing` | fundido de salida (0,16 s) y `Schedule(Settle)` | ninguna (`Bar`/`Ribbon` siguen pintándose) |
| `Collapsing` | `Enter(Bar \| Ribbon)` | `Expanded` | `Cancel()`; el despliegue relanza su animación desde el desplazamiento inicial | ninguna |
| `Collapsing` | `Enter(Tab)` | `Collapsing` | **sin cambio**: el asentamiento manda (D2) | — |
| `Collapsing` | `Timer(Settle)` | `Tab` | la App encoge la ventana a la huella plegada; después `Reevaluate()` | **`Bar`, `Ribbon`** |
| cualquiera | `BeginDrag` | sin cambio | `Cancel()`; `IsDragging = true`; las regiones se siguen registrando | — |
| cualquiera | `EndDrag` | según regiones | `IsDragging = false`; `Reevaluate()` | — |
| cualquiera | `Expand()` | `Expanded` | `Cancel()`; salto directo (E6) | ninguna |
| cualquiera | `CollapseToTab()` | `Tab` | `Cancel()`; salto directo (E6) | **`Bar`, `Ribbon`** |

Las tres filas que cambian respecto de ADR-003 están marcadas con **D2** o **D3** en la prosa de §5.6. La rutina `Reevaluate` de ADR-003 §7.3 se conserva con la única simplificación de §5.2.

---

## 7. Por qué el ciclo repetido ya funciona

### 7.1 Traza del ciclo que se colgaba

Secuencia medida en el ciclo 2 de H-E3-05, con el diseño nuevo:

| Instante | Hecho del sistema | Región | Estado | Qué ocurre |
| --- | --- | --- | --- | --- |
| t0 | el puntero entra en la pestaña (elemento pintado) | `{Tab}` | `Tab → Arming` | se arma la activación |
| t0+200 ms | vence la activación | `{Tab}` | `Arming → Expanded` | la ventana crece **hacia dentro**; la pestaña no se mueve; el panel aparece por dentro del puntero |
| t0+250 ms | el puntero sale del panel por el interior | `{ }` | `Expanded` | se arma la gracia de 400 ms |
| t0+650 ms | vence la gracia | `{ }` | `Expanded → Collapsing` | fundido de 160 ms; `Bar` sigue presente y sigue pintándose |
| t0+810 ms | vence el asentamiento | `{ }` | `Collapsing → Tab` | **purga de `Bar`**; la ventana encoge |
| — | el puntero está fuera de la ventana | `{ }` | `Tab` | nada armado, nada pendiente |

El fundido termina en el **mismo instante** que el asentamiento (§5.4), así que en el ciclo 2 ya no hay una ventana de 40 ms en la que el panel esté apagado y la ventana siga grande: no existe un píxel transparente que el sistema pueda dejar de reportar mientras la ventana aún ocupe la huella desplegada.

Y si el puntero vuelve a la pestaña durante el colapso, la fila `Collapsing + Enter(Tab)` **no revierte**: el asentamiento termina en `Tab`, la purga deja el conjunto en `{Tab}` y `OnSettleElapsed` arma una activación nueva. El usuario ve el despliegue que HU02 CA #2 pide, y no el rebote que se medía.

### 7.2 Traza del ciclo que quedaba invisible

| Ciclo | `Opacity` de reposo del estado | Reloj vivo al terminar | Resultado del píxel |
| --- | --- | --- | --- |
| 1 | `Expanded` → 1 | ninguno (`Stop` al acabar los 160 ms) | pintado |
| 2 | `Expanded` → 1 | ninguno | pintado, **igual que el ciclo 1** |
| N | `Expanded` → 1 | ninguno | pintado |

El valor mostrado deja de depender del historial: es un `Setter` del estado vigente. Ningún reloj anterior puede ganar al nuevo porque ninguno sobrevive a su transición, y al no haber `From` en el fundido de entrada, la única fuente para el valor inicial es la propiedad vigente, que es el valor de reposo del estado anterior.

### 7.3 Coste en reposo

- **Sin temporizador pendiente** en `Tab` y en `Expanded` sin gracia, como en ADR-003.
- **Sin *storyboard* retenido**: al usar `Stop`, el `Clock` se elimina al terminar la animación; un reloj en `Stop` no solicita fotogramas (RNF-02). Con `HoldEnd`, en cambio, el reloj queda retenido y su valor sigue siendo el efectivo, que es la definición del defecto que se corrige.
- **Sin sonda ni reconciliación**: desaparecen `CursorProbe`, el `Dispatcher.BeginInvoke(Input)` del reporter y las suscripciones a `SizeChanged`/`LocationChanged` de la ventana. El reposo cuesta **menos** que antes de este ADR.
- **Un degradado de opacidad de 160 ms** por colapso, ya presente hoy (120 ms), sin *layout* ni notificaciones.
- Nada nuevo que medir por elemento: la matriz de presencia es una función estática sin asignación.

---

## 8. Mapa hallazgo → pieza → prueba

### 8.1 H-E3-05 — región pegada y estado pegado

| Pieza | Qué garantiza | Prueba |
| --- | --- | --- |
| `ContentPanel` con `PointerRegionBehavior` (D1) | la región solo se afirma sobre píxeles que el puntero puede tocar | `Views\MainWindowStoryboardContractTests.BarRegion_IsDeclaredByTheContentPanel` |
| `RootPanel` sin `Background` y sin `WindowRegionReporter` (D1) | no queda ninguna vía de fabricar una entrada | `Views\MainWindowStoryboardContractTests.RootPanel_DoesNotPaint_And_DoesNotReportRegions`, `NoWindowReporter_IsDeclaredAnywhere` |
| `DockRegionPresence.IsPresent` en `EnterRegion` (D3) | una entrada imposible no entra en el conjunto | `Interaction\DockRegionPresenceTests.PresenceMatrix_MatchesTheTable`, `IsContent_OnlyForTheBarAndTheRibbons`; `Interaction\DockInteractionControllerTests.EnterBar_WhileTab_IsIgnored`, `EnterBar_WhileArming_IsIgnored` |
| purga en `SetState` (D3) | el conjunto nunca contiene regiones de contenido fuera de `Expanded`/`Collapsing` | `Interaction\DockInteractionControllerTests.SettleToTab_DropsTheContentRegions`, `CollapseToTab_DropsTheContentRegions` |
| fila `Collapsing + Enter(Tab)` sin reversión (D2) | volver a la pestaña no reabre sin *hover* | `Interaction\DockInteractionControllerTests.CollapseThenEnterTabDuringSettle_DoesNotReverse_AndRedeploysAfterTheHover` |
| fila `Collapsing + Enter(Bar)` con reversión (D2) | volver al panel lo reabre mientras se pinta | `Interaction\DockInteractionControllerTests.CollapseThenEnterBarDuringSettle_ReversesToExpanded` |
| `WindowRegionReporter`, `WindowRegionTracker` y `CursorProbe` borrados (D1) | la causa raíz desaparece del árbol | `grep` de control §9.4; `NoWindowReporter_IsDeclaredAnywhere` |
| ciclo completo repetido | el escenario medido se reproduce como aserción | `Interaction\DockInteractionControllerTests.ThreeConsecutiveCycles_DeployAndCollapseEveryTime` |

### 8.2 H-E3-06 — panel invisible

| Pieza | Qué garantiza | Prueba |
| --- | --- | --- |
| `FillBehavior="Stop"` en todos los *storyboards* (D4/R1) | ningún reloj queda retenido | `Views\MainWindowStoryboardContractTests.EveryStoryboardStopsItsClock` (`HoldEnd` ausente en todo el archivo) |
| opacidad de reposo declarada por estado (D4/R2) | el valor mostrado no depende del historial | `Views\MainWindowStoryboardContractTests.CollapsingDeclaresItsRestingOpacity` (existe un `Setter` de `Opacity` = 0 en el `DataTrigger` de `Collapsing` y el estilo declara `Opacity` = 1) |
| sin `From="0.3"` (D4/R3) | el fundido de entrada converge desde cualquier valor, incluida una reversión | `Views\MainWindowStoryboardContractTests.DeployFade_HasNoAbsoluteStartingPoint` |
| fundido de salida de 0,16 s (D4) | el panel está pintado mientras la ventana encoge | `Views\MainWindowStoryboardContractTests.FadeOutDuration_EqualsTheSettleDuration` |
| micro-efecto con `Stop` + `AutoReverse` (D4) | el efecto se repite en cada activación (Q-11) y no deja escala retenida | `Views\MainWindowStoryboardContractTests.MicroExpansion_StopsAndReturnsToItsBaseValue` |
| literales derivados de Core (D5, cierra D-14) | el XAML no puede desalinearse de `DockBarMetrics` ni de `InteractionTimings` sin que la suite falle | `Views\MainWindowStoryboardContractTests.SlideOffsets_MatchTheThicknessGained`, `Durations_MatchInteractionTimings` |

### 8.3 Pruebas existentes que deben cambiar

Cambiar una prueba porque el comportamiento esperado ha cambiado **no es aflojarla**: cada cambio tiene que dejar la aserción **más estricta** que antes, y se documenta aquí para que la revisión lo compruebe.

| Prueba (`tests\StickyDock.Tests\Interaction\DockInteractionControllerTests.cs`) | Situación | Cambio |
| --- | --- | --- |
| `EnteringACollapsingBar_ReExpandsIt` (l. 358) | sigue válida | ninguna: es la fila `Collapsing + Enter(Bar)` de D2 |
| `EnteringTheTabAfterTheCollapse_StartsANewActivation` (l. 373) | sigue válida pero hoy para a mitad | se avanza el **asentamiento** y después la **activación**, y se afirma `Expanded`; se renombra a `SettleThenHover_DeploysTheBarAgain` |
| `SecondActivation_AfterACollapse_DeploysAgain` (l. 388) | sigue válida | ninguna: es la regresión de H-E3-05 y se conserva tal cual |
| `LeaveTab_WhenAnotherRegionIsStillActive_DeploysDirectly` (l. 344) | la rama ya no existe | **se borra** y su intención la cubre `EnterBar_WhileArming_IsIgnored`, que afirma lo contrario: en `Arming` la región `Bar` no existe |
| `ActiveRegions_TracksEntriesAndExits` (l. 568) | entra `Bar` estando en `Arming` | se entra `Bar` estando en `Expanded` (a través del flujo real) y se afirma `{Tab, Bar}` y luego `{Bar}` |
| `ReconciledReportsOfTheSameSide_DoNotArmOrCancelTheSlot` (l. 228) | su sujeto (el tracker) desaparece | se **fusiona** en `RepeatedEntriesOfAnActiveRegion_DoNotArmOrCancelTheSlot`, que conserva las cuatro aserciones de recuentos |
| `EnterRegion_WithNone_IsIgnored` (l. 583) | sigue válida | ninguna |
| `LeaveRegion_WhenTheRegionWasNotActive_IsIgnored` (l. 594) | sigue válida | ninguna |

Ninguna prueba se elimina sin sustituto salvo `LeaveTab_WhenAnotherRegionIsStillActive_DeploysDirectly`, cuyo comportamiento **deja de existir** por diseño (D2/D3) y queda explicado en §5.6.

---

## 9. Verificación

### 9.1 Compilación y suite

```powershell
dotnet build -c Release
dotnet build -c Debug
dotnet test
```

Criterio: **0 errores y 0 advertencias** en ambos perfiles (`TreatWarningsAsErrors`) y suite en verde.

**Cifra de la suite, como estimación con método**, no como objetivo:

| Concepto | Casos |
| --- | ---: |
| Suite vigente al abrir la corrección | 368 |
| − `tests\...\Interaction\WindowRegionTrackerTests.cs` (7 `[Fact]`, el sujeto desaparece) | −7 |
| − `tests\...\Views\MainWindowRegionLayoutTests.cs` (4 `[Fact]`, se sustituye) | −4 |
| + `DockRegionPresenceTests` (teoría de 16 combinaciones + `IsContent`) | +17 |
| + `DockInteractionControllerTests` (8 pruebas de §9.2) | +8 |
| + `MainWindowStoryboardContractTests` (11 comprobaciones de §9.2) | +11 |
| **Estimación** | **393** |

La estimación es aritmética sobre casos enumerados, no un recuento de ejecución. La fusión de `ReconciledReportsOfTheSameSide_…` en `RepeatedEntriesOfAnActiveRegion_…` (§8.3) puede mover el total en ±1 y el `[Theory]` de presencia puede contarse como 1 caso o como 16 según el informador de pruebas; el `implementador` fija la cifra exacta y el `qa` la comprueba contra la ejecución real. **No se admite bajar de 368** sin justificación escrita.

Ninguna prueba puede usar esperas reales: el reloj es el `ManualScheduler` de ADR-003.

### 9.2 Pruebas nuevas, con su aserción

`tests\StickyDock.Tests\Interaction\DockRegionPresenceTests.cs`

| Prueba | Aserción |
| --- | --- |
| `PresenceMatrix_MatchesTheTable` | `[Theory]` con las 16 combinaciones; `Tab` verdadero en los cuatro estados, `Bar`/`Ribbon` verdaderos solo en `Expanded` y `Collapsing`, `None` falso siempre |
| `IsContent_OnlyForTheBarAndTheRibbons` | `Bar` y `Ribbon` verdaderos; `Tab` y `None` falsos |

`tests\StickyDock.Tests\Interaction\DockInteractionControllerTests.cs` (nuevas)

| Prueba | Aserción |
| --- | --- |
| `EnterBar_WhileTab_IsIgnored` | el estado sigue en `Tab`, `ActiveRegions` vacío y no hay temporizador armado |
| `EnterBar_WhileArming_IsIgnored` | el estado sigue en `Arming`, la ranura conserva la activación y su retardo es de 200 ms |
| `CollapseThenEnterBarDuringSettle_ReversesToExpanded` | tras la gracia y con el asentamiento pendiente, `Enter(Bar)` deja el estado en `Expanded` y la ranura vacía |
| `CollapseThenEnterTabDuringSettle_DoesNotReverse_AndRedeploysAfterTheHover` | `Enter(Tab)` en `Collapsing` no cambia el estado; al avanzar el asentamiento queda en `Tab` con una activación armada; avanzados 200 ms, `Expanded` |
| `SettleToTab_DropsTheContentRegions` | entrando en `Expanded` con `Bar` activa y sin `Leave`, tras la gracia y el asentamiento `ActiveRegions` está vacío |
| `CollapseToTab_DropsTheContentRegions` | `CollapseToTab()` con `Bar` activa deja `ActiveRegions` vacío |
| `RepeatedEntriesOfAnActiveRegion_DoNotArmOrCancelTheSlot` | cinco entradas seguidas de la misma región activa no cambian `ScheduleCount` ni `CancelCount` |
| `ThreeConsecutiveCycles_DeployAndCollapseEveryTime` | tres ciclos completos dejan tres `Expanded` y tres `Tab` en la secuencia de estados, y terminan sin temporizador pendiente |

`tests\StickyDock.Tests\Views\MainWindowStoryboardContractTests.cs` (nuevas; carga el XAML con el mismo patrón de `RepositoryRoot()` que la prueba que sustituye)

| Prueba | Aserción |
| --- | --- |
| `BarRegion_IsDeclaredByTheContentPanel` | el único elemento con `PointerRegionBehavior.Region` = `Bar` es `ContentPanel`, y lleva `EnterCommand` y `LeaveCommand` |
| `RootPanel_DoesNotPaint_And_DoesNotReportRegions` | `RootPanel` no tiene atributo `Background` ni ninguna propiedad `WindowRegionReporter` |
| `NoWindowReporter_IsDeclaredAnywhere` | ningún elemento del XAML menciona `WindowRegionReporter` |
| `TabBorder_KeepsItsRegionAndItsDragHandle` | `TabBorder` declara `Tab` y `PressDragBehavior.Command` (se conserva de la prueba anterior) |
| `EveryStoryboardStopsItsClock` | todos los `Storyboard` del archivo declaran `FillBehavior="Stop"` y no aparece `HoldEnd` |
| `CollapsingDeclaresItsRestingOpacity` | el estilo declara `Opacity` = 1 y el `DataTrigger` de `Collapsing` declara `Setter` `Opacity` = 0 |
| `DeployFade_HasNoAbsoluteStartingPoint` | ninguna animación de `Opacity` declara `From` |
| `FadeOutDuration_EqualsTheSettleDuration` | la duración del fundido de salida es `TimeSpan.FromMilliseconds(InteractionTimings.SlideMilliseconds)` |
| `SlideOffsets_MatchTheThicknessGained` | el valor absoluto de cada `From` de desplazamiento es `|DockBarMetrics.Default.ExpandedThickness − DockBarMetrics.Default.CollapsedThickness|`, y el eje y el signo coinciden con el borde |
| `Durations_MatchInteractionTimings` | las dos duraciones del despliegue son `SlideMilliseconds` |
| `MicroExpansion_StopsAndReturnsToItsBaseValue` | las dos animaciones de escala van a `1.06` con `AutoReverse` y `Stop`, en el `DataTrigger` de `Arming` |

### 9.3 Por qué la prueba estructural sustituida se sustituye y no se corrige

`MainWindowRegionLayoutTests` no estaba mal escrita: estaba escrita para el diseño defectuoso y **afirmaba exactamente lo contrario** de lo que hay que garantizar (`BarRegion_IsReportedByTheWindowReporter_OnTheRootContainer` exigía región en la raíz y fondo en la raíz; `BarRegion_IsNotDeclaredByTheSlidingPanel` exigía que el panel **no** tuviera región). Corregir sus aserciones una a una dejaría una prueba cuyo nombre miente y cuyo comentario explica un razonamiento derogado. Se sustituye por completo y el razonamiento nuevo se documenta en el encabezado del archivo nuevo.

### 9.4 Greps de control

```powershell
# La maquinaria fabricada no puede sobrevivir en ninguna forma.
Get-ChildItem -Path src,tests -Recurse -Include *.cs,*.xaml |
    Select-String -Pattern 'WindowRegionReporter|WindowRegionTracker|RegionReport|CursorProbe|GetCursorPos|PixelRect\.Contains'

# Ningún reloj retenido en la vista.
Get-ChildItem -Path src\StickyDock.App\Views -Recurse -Include *.xaml |
    Select-String -Pattern 'HoldEnd'

# La raíz no pinta ni reporta.
Get-ChildItem -Path src\StickyDock.App\Views -Recurse -Include *.xaml |
    Select-String -Pattern 'RootPanel' -Context 0,4

# La política sigue fuera de la vista (E0-CA6, §5.2-01).
Get-ChildItem -Path src\StickyDock.App\Views -Recurse -Include *.cs |
    Select-String -Pattern 'DockState|Timer|DispatcherTimer|Thread\.Sleep|CompositionTarget'

# Nada periódico nuevo en Core.
Get-ChildItem -Path src\StickyDock.Core -Recurse -Include *.cs |
    Select-String -Pattern 'DispatcherTimer|CompositionTarget\.Rendering|Thread\.Sleep'
```

`Get-ChildItem -Recurse` y no `Select-String -Path src\**\*.cs`: `-Path` no expande `**` de forma recursiva en Windows PowerShell 5.1, así que la forma anterior **no habría inspeccionado** los archivos de los subdirectorios —incluidos los que se borran— y habría dado un falso verde. Los cinco comandos se ejecutan desde la raíz del repositorio.

Resultado exigido: **ninguna coincidencia** en el primero, el segundo, el cuarto y el quinto; en el tercero, `RootPanel` sin `Background` ni `WindowRegionReporter`.

### 9.5 Sonda de medición reproducible (evidencia de cierre)

Script descartable `.tmp-e3diag\probeD.ps1`, ampliación del `probeB2.ps1` que el Director usó para medir los dos hallazgos. No forma parte del producto y no se versiona.

**Requisitos previos**: la aplicación en Release, con `dockEdge: 2` (borde derecho) y un solo monitor a 100 %. Con ese borde la huella plegada es `36x120` y la desplegada `96x168`.

**Algoritmo**:

1. Localizar la ventana de la aplicación enumerando con `EnumWindows` y filtrando por el **PID** del proceso y por la clase `HwndWrapper[StickyDock;;<guid>]`; guardar ese `HWND`. **No** sirve `MainWindowHandle` (vale 0 porque la ventana usa `ShowInTaskbar="False"`) ni buscar por título: el título real es `Sticky Dock Utility`, no `StickyDock`, y un `FindWindow` con ese nombre devuelve 0.
2. Leer `GetWindowRect` en reposo y **afirmar** `36x120`.
3. Calcular dos puntos a partir del rectángulo plegado: `P_tab` en el centro de la franja exterior (la pestaña) y `P_panel` en una coordenada que solo exista con la barra desplegada (por ejemplo `(rect.Left − 84, rect.Top + 60)` para el borde derecho, que corresponde al `(1840, 500)` de la medición original).
4. Repetir **tres ciclos**:
   1. `SetCursorPos(P_tab)` con dos micro-movimientos (`P_tab + 1` y de vuelta) para que WPF reciba un movimiento real; `Start-Sleep 600 ms`.
   2. **Afirmar** `GetWindowRect` = `96x168`.
   3. **Afirmar** `WindowFromPoint(P_panel)` = el `HWND` de la aplicación. Es la comprobación que fallaba en el ciclo 2 con Chrome.
   4. **Afirmar** el color del píxel `P_panel` distinto del fondo del escritorio (comparación con una muestra tomada antes de empezar).
   5. `SetCursorPos` lejos de la ventana (`+500, +300`); `Start-Sleep 900 ms`.
   6. **Afirmar** `GetWindowRect` = `36x120` (gracia de 400 ms + asentamiento de 160 ms, con margen).
   7. **Afirmar** `WindowFromPoint(P_panel)` ≠ `HWND` de la aplicación.
5. Al final: **afirmar** que el proceso sigue vivo y que `Get-Process` no muestra un consumo de CPU acumulado anómalo.

**Criterio de aprobación**: los tres ciclos pasan las siete afirmaciones. La medición debe repetirse con la barra en los bordes **superior**, **inferior** e **izquierdo** girando `P_panel` según la tabla de ADR-003 §6.2, y con la barra **flotante** (donde no se comprueba el *slide*, solo el fundido y el regreso a `36x120` tras el colapso).

**Diagnóstico adicional si algo falla**: adjuntar el log de la aplicación de la ventana de 5 s, donde la secuencia por ciclo debe ser `Enter Tab` → `Arming` → `Expanded` → `Leave Tab` → `Collapsing` → `Tab`, **sin** ningún `Enter Bar` espurio y **sin** repeticiones de `Expanded` dentro del mismo ciclo.

### 9.6 Verificaciones manuales

| ID | Verificación | Criterio o deuda que cubre |
| --- | --- | --- |
| M-05 (vigente) | *Hover* sostenido y retirada: micro-efecto perceptible antes de estar desplegada, sin recortarse, y *slide* entrando desde el borde correcto; literales del XAML coherentes con Core | E3-CA1, ahora también D-14 por máquina |
| M-06 (vigente) | la pestaña conserva su posición en pantalla al desplegar y colapsar, en los cuatro bordes | E3-CA1, E3-CA2, invariante I3 |
| M-07 (vigente) | aspecto de la pestaña y del contenido al 125 % y 150 % tras `PerMonitorV2` | AMP-03 |
| M-08 (vigente) | un `settings.json` de E2 (versión 1) migra a la versión 2 conservando `dockEdge` y añadiendo `gracePeriodMs` | E3-CA5 |
| **M-09 (nueva)** | **Repetición**: cinco ciclos seguidos de *hover* y retirada. En cada uno la barra se despliega **visible**, colapsa y vuelve a desplegarse con el mismo aspecto. Es la verificación manual de H-E3-05 y H-E3-06 | HU02 CA #2/#3, H-E3-05, H-E3-06 |
| **M-10 (nueva)** | **Reversión**: volver al panel durante el colapso lo reabre sin parpadeo; volver a la pestaña durante el colapso muestra la pestaña, y a los 200 ms la barra se despliega de nuevo | D2, E3-CA3 |
| **M-11 (nueva, regresión)** | **El *hit-test* de la ventana no cambia**: con la barra colapsada, la ventana sigue midiendo `120×36` (100 %) y su comportamiento de clic es el de E1 —un clic dentro del rectángulo de la ventana lo recibe la aplicación, porque la `Window` conserva `Background="Transparent"`; un clic fuera del rectángulo llega a la ventana de debajo—. La comprobación vigila que quitar el `Background` de `RootPanel` **no** altera lo que el sistema entrega, ni para bien ni para mal | E1-CA4 como **no-regresión**; D-15 sigue abierta |

---

## 10. Lista de verificación de implementación

### Crear — `StickyDock.Core`

- [ ] `src\StickyDock.Core\Interaction\DockRegionPresence.cs` — `IsPresent`, `IsContent` (§5.1).

### Crear — pruebas

- [ ] `tests\StickyDock.Tests\Interaction\DockRegionPresenceTests.cs` — matriz de 16 combinaciones y `IsContent`.
- [ ] `tests\StickyDock.Tests\Views\MainWindowStoryboardContractTests.cs` — las once comprobaciones de §9.2, con `RepositoryRoot()` copiado del archivo que sustituye.

### Modificar

- [ ] `src\StickyDock.Core\Interaction\DockInteractionController.cs` — guarda de presencia en `EnterRegion`, reversión solo para contenido, retorno temprano para `Tab` en `Collapsing`, `PurgeAbsentRegions()` invocada desde `SetState` **antes** de elevar el evento, y simplificación de la rama `Arming` de `Reevaluate` (§5.2).
- [ ] `src\StickyDock.App\Views\MainWindow.xaml` — `RootPanel` sin `Background` ni propiedades `WindowRegionReporter`; `ContentPanel` con `PointerRegionBehavior` (`Bar`, `EnterCommand`, `LeaveCommand`); los seis `Storyboard` con `FillBehavior="Stop"`; `Opacity` de reposo por estado; eliminación de los cinco `From="0.3"`; fundido de salida a `0:0:0.16` con `Setter` `Opacity` = 0 en el `DataTrigger`; micro-efecto con `Stop`. Se actualizan los dos comentarios que documentan el razonamiento derogado (l. 37-46 y l. 227-244).
- [ ] `src\StickyDock.Interop\Win32\NativeMethods.cs` — retirar `GetCursorPos` y las estructuras que queden sin uso.
- [ ] `src\StickyDock.Core\Docking\DockingGeometry.cs` — retirar `PixelRect.Contains(int, int)`.
- [ ] `tests\StickyDock.Tests\Interaction\DockInteractionControllerTests.cs` — ocho pruebas nuevas y los cuatro cambios de §8.3.

### Borrar

- [ ] `src\StickyDock.App\Input\WindowRegionReporter.cs`.
- [ ] `src\StickyDock.Core\Interaction\WindowRegionTracker.cs`.
- [ ] `src\StickyDock.Interop\Win32\CursorProbe.cs`.
- [ ] `tests\StickyDock.Tests\Interaction\WindowRegionTrackerTests.cs`.
- [ ] `tests\StickyDock.Tests\Views\MainWindowRegionLayoutTests.cs`.

### No tocar

- [ ] `src\StickyDock.App\Views\MainWindow.xaml.cs` — ya no contiene manejadores y no debe contener ninguno.
- [ ] `src\StickyDock.App\Input\PointerRegionBehavior.cs` — se reutiliza tal cual.
- [ ] `src\StickyDock.Core\Interaction\DockRegion.cs`, `IDockRegionSink.cs`, `GracePeriod.cs`, `InteractionTimings.cs`, `DockBarSizeSource.cs`, `src\StickyDock.Core\Timing\ISingleShotScheduler.cs`.
- [ ] `src\StickyDock.Core\Docking\*` — `DockBarMetrics`, `IDockBarSizeSource`, `DockPlacementCoordinator`, `DockPlacementCalculator`.
- [ ] `src\StickyDock.Core\Configuration\*` y el esquema v2.

### Verificación final

- [ ] `dotnet build -c Release` y `-c Debug` sin errores **ni advertencias**.
- [ ] `dotnet test` en verde, sin esperas reales, con la cifra justificada en §9.1.
- [ ] Los cinco greps de control de §9.4 en el resultado exigido.
- [ ] `probeD.ps1` pasa los tres ciclos en los cuatro bordes y en flotante.
- [ ] M-05 a M-11 comprobadas en el equipo de desarrollo; M-01 a M-04 y M-07 según el plan de E8.
- [ ] Medición de cierre: memoria privada comprometida < 42 MB y CPU < 1 % de un núcleo durante 30 s con la barra colapsada (RNF-01, RNF-02), más la misma medición con la barra desplegada.

## 11. Consecuencias

### 11.1 Positivas

1. **H-E3-05 pasa de improbable a inexpresable.** La región `Bar` solo puede entrar mientras el estado la admita y su elemento se pinte (D3, primera guarda), y el conjunto se vacía al cambiar de estado sin depender de un `Leave` que WPF no garantiza (D3, purga). Ya no existe ninguna vía por la que el área interactiva pueda afirmar «dentro» sobre píxeles que el sistema no entrega.
2. **H-E3-06 pierde su causa material.** Con `FillBehavior="Stop"` no queda ningún reloj retenido, y el valor mostrado deja de ser «el último que animó» para ser el `Setter` del estado vigente (D4, R1 y R2). El ciclo N se comporta como el ciclo 1 por la misma razón que el ciclo 1 se comportaba como el ciclo 1.
3. **El balance de líneas es negativo.** Desaparecen `WindowRegionReporter` (284 líneas medidas), `WindowRegionTracker` (51 medidas), `CursorProbe`, `GetCursorPos` con sus estructuras y `PixelRect.Contains`, además de dos archivos de prueba. Se añade una función pura de dos métodos (unas 40 líneas), dos guardas y doce pruebas. La corrección **reduce** la huella de memoria privada comprometida en lugar de aumentarla (RNF-01, presupuesto de 42 MB).
4. **La verificación deja de depender de la inspección para lo que más se desalinea.** Los literales de animación pasan a estar vigilados por la suite contra `DockBarMetrics.Default` e `InteractionTimings` (D5), de modo que un ajuste en Core sin ajustar el XAML —o al revés— rompe `dotnet test` en vez de descubrirse midiendo píxeles.
5. **D-15 no empeora.** Quitar el `Background` de `RootPanel` **no** cambia lo que el sistema entrega: la `Window` conserva `Background="Transparent"`, así que el *hit-test* de una ventana *layered* sigue decidiéndose por el alfa del píxel, exactamente como antes. La deuda **no se cierra ni mejora**: sigue sin haber *hit-test* selectivo y la regla de ADR-003 §14 se mantiene (cualquier ampliación futura de la huella plegada debe verificarse contra E1-CA4). La decisión no se apoya en ningún efecto colateral sobre E1-CA4.
6. **El reposo cuesta menos, no más.** Desaparecen el sondeo nativo del cursor, el `Dispatcher.BeginInvoke` del reporter y sus suscripciones a `SizeChanged`/`LocationChanged`; la matriz de presencia es una función estática sin asignación y ninguna animación deja un reloj vivo. RNF-02 y RNF-01 mejoran en el mismo sentido que ADR-003, no en contra.
7. **La corrección es auditable con cinco `grep`.** La maquinaria fabricada no puede sobrevivir en ninguna forma sin que §9.4 la delate, y el nombre `WindowRegionReporter` deja de existir en el árbol.
8. **E4 y E7 heredan la costura sin cirugía.** `DockRegion.Ribbon` ya está en la matriz y el controlador lo trata como una región más; `PointerRegionBehavior` no cambia; cada barra de E7 tendrá su propio controlador y su propio conjunto, sin estado compartido nuevo.
9. **El code-behind sigue vacío.** La región se declara con una propiedad adjunta en el XAML y no se añade ni un manejador a `MainWindow.xaml.cs` (E0-CA6, §5.2-01).
10. **La prueba estructural deja de mentir.** El archivo sustituido afirmaba el diseño defectuoso; el nuevo afirma la invariante y deriva sus números de Core (§9.3).

### 11.2 Negativas / deuda asumida

1. **La matriz de presencia y el `Visibility` del XAML son dos declaraciones del mismo hecho.** D3 las hace coincidir hoy, pero nada en el compilador impide que un cambio futuro de la vista las separe: la prueba de contrato comprueba que `Bar` está en `ContentPanel` y que la opacidad de reposo existe, no la equivalencia completa «estado ↔ visibilidad de cada elemento». Se registra como **D-18** y la primera etapa que lo notará es E4, que añade elementos.
2. **El fundido de salida dura 40 ms más** (0,16 s frente a 0,12 s). El panel permanece pintado durante todo el asentamiento, que es justo lo que evita el instante en que la ventana ocupa la huella desplegada sin nada pintado. El coste es un degradado de opacidad de 40 ms adicionales en el compositor, sin *layout* ni notificaciones, y ya existe un temporizador de esa duración en el controlador.
3. **`Enter(Tab)` durante el colapso pierde inmediatez.** Antes revertía al instante (y por ahí se colgaba); ahora el usuario espera el final del asentamiento (≤160 ms) más los 200 ms del *hover*. Es el único comportamiento que este ADR hace más lento, es deliberado (D2) y M-10 existe para comprobar que no se lee como una avería.
4. **El orden «purga antes de `StateChanged`» no tiene una prueba de orden explícita.** D-19: hoy `SettleToTab_DropsTheContentRegions` observa el conjunto después del hecho. El endurecimiento consiste en afirmar `ActiveRegions` desde dentro del manejador de `StateChanged`, y queda anotado para el `implementador`.
5. **La sonda sigue siendo evidencia manual.** D-11 no se cierra: ninguna prueba automatizada dibuja la ventana. Lo que se gana es reproducibilidad —tres ciclos, siete afirmaciones, cuatro bordes y el caso flotante (§9.5)—, no automatización.
6. **Los literales del XAML siguen siendo literales.** D-14 se mitiga, no se cierra: `Timeline.From` y `Timeline.Duration` no admiten *binding* (ADR-003, P5.b), así que el valor no puede venir de Core; la diferencia es que ahora un desajuste **se detecta** en lugar de **prevenirse**.
7. **Se elimina una prueba cuyo comportamiento deja de existir** (`LeaveTab_WhenAnotherRegionIsStillActive_DeploysDirectly`). Queda explicado en §5.6 y §8.3 para que no se lea como un aflojamiento de la suite.
8. **La etapa se reabrió dos veces por un defecto conceptual.** La prueba estructural no lo detectó porque **afirmaba el diseño defectuoso**; la lección queda escrita en §9.3: una prueba de la estructura de la vista solo puede afirmar una invariante, nunca la disposición vigente.

### 11.3 Impacto en otras áreas

- **E1 (cerrada)**: no se toca código suyo. Quitar el `Background` a `RootPanel` **no** cambia el *hit-test* de la ventana —la `Window` conserva el suyo—, así que E1-CA4 sigue cumpliéndose por la misma razón que antes (la ventana plegada mide exactamente la pestaña) y D-15 sigue abierta. M-04 y M-07 se repiten solo por precaución, al cambiar la composición de la raíz.
- **E4 (Cintas)**: hereda `Ribbon` en la matriz y la purga en `Tab`. Cada cuadro o panel de Cinta debe declarar su región en el elemento que se pinta y estar `Collapsed` en los estados en que la matriz la niega. Si alguna Cinta se implementa como ventana emergente (D-13 de ADR-003), su región no llegará por `PointerRegionBehavior` y habrá que registrarla explícitamente; la matriz seguirá siendo válida, pero la entrada dejará de ser automática. **D3 no responde a Q-10**: la purga vacía el *conjunto de regiones*, no el estado de apertura de la Cinta, que es contenido y sigue siendo de E4.
- **E6 (bandeja)**: `Expand()` y `CollapseToTab()` no cambian de contrato. Ver la observación de §14.2, que afecta al significado del comando del menú.
- **E7 (multimonitor)**: cada barra tendrá su `DockInteractionController` y su conjunto de regiones; la matriz es una función pura del estado, así que la réplica no comparte estado nuevo.
- **E8 (rendimiento y entrega)**: M-09, M-10 y M-11 pasan al informe de entrega; D-11 se reduce pero no desaparece; la medición de reposo se repite con menos superficie que medir y sin sondeo nativo.

---

## 12. Decisiones de ingeniería sin traza a requerimiento

Se declaran para que no se confundan con requisitos. Ninguna cambia un criterio de aceptación y todas se ajustan en un punto.

| # | Decisión | Por qué no es requisito | Dónde se ajusta |
| --- | --- | --- | --- |
| 1 | El fundido de salida dura lo mismo que el asentamiento (**0,16 s**) | §4.3-01 fija el despliegue; el repliegue no tiene forma ni duración prescritas | Bloque `Collapsing` de §5.4 y `FadeOutDuration_EqualsTheSettleDuration` |
| 2 | La región `Bar` se declara sobre **`ContentPanel`** y no sobre la raíz | Ningún RF dice dónde se declara una región; lo que la especificación exige es que el área interactiva sea la barra y sus Cintas (A4) | `MainWindow.xaml` |
| 3 | `Enter(Tab)` durante `Collapsing` **no** revierte | §4.3-03 pide no cerrar en falso, no reabrir sin *hover*; la lectura es exegética | §4.2 y `DockInteractionController.EnterRegion` |
| 4 | La purga ocurre en `SetState`, **antes** de elevar `StateChanged` | Es orden interno de una clase; ningún RF lo prescribe | `SetState` |
| 5 | Micro-efecto de **1,06 durante ~120 ms**, simultáneo con el *slide* | §4.3-02 pide «leve expansión», sin magnitud ni duración | Heredado de ADR-003 §12 #2; `TabBorderStyle` |
| 6 | `FillBehavior="Stop"` se exige en **todo** el archivo, no solo en el panel | RNF-02 pide no dejar trabajo vivo; no dice cómo se declara una animación | `EveryStoryboardStopsItsClock` |
| 7 | `IsContent` existe como método propio de `IsPresent` | Ergonomía de la tabla; podría ser una lista privada del controlador | `DockRegionPresence` |
| 8 | La sonda hace **tres ciclos** con **siete afirmaciones** | La especificación no fija cómo se verifica el rendimiento ni la repetición | `.tmp-e3diag\probeD.ps1` (§9.5) |

---

## 13. Alternativas descartadas

- **P1-A — conservar el reporter y corregirlo.** No corrige la causa: sigue preguntando por el rectángulo de la ventana, que en una ventana *layered* incluye píxeles que no reciben nada. Se reevaluaría solo si apareciera un caso real de píxel pintado que no entrega el puntero, que no existe por definición del sistema.
- **P1-C — `WM_NCHITTEST`.** No llega sobre píxeles con alfa 0, que es la razón de ser del problema, y añade P/Invoke a una etapa que se cierra por comportamiento visual. Se reevaluaría si E8 decide atacar **D-15** de raíz con un *hit-test* selectivo.
- **P2-A — retirar la reversión del todo.** Ahorra máquina pero introduce el parpadeo que el temporizador de gracia existe para evitar (§4.3-03, E3-CA3). Se reevaluaría si M-10 mostrara que el comportamiento actual se percibe como un fallo.
- **P2-C — revertir con una gracia corta en lugar de inmediatamente.** Añade un estado intermedio y un temporizador más sin comprar ningún criterio; el usuario volvería a ver la barra «a medias». Descartada.
- **P3-A — confiar en que el par `Enter`/`Leave` esté equilibrado.** Es la suposición que produjo H-E3-05. **No se reevalúa**: WPF no garantiza el `Leave` de un elemento que se oculta, se colapsa o se recorta a mitad del gesto.
- **P3-C — refrescar las regiones con `Mouse.DirectlyOver`.** Es estado de WPF, no del sistema, y en una ventana *layered* puede apuntar a un elemento ya invisible; reintroduce el patrón «el estado se corrige a posteriori» que es la raíz del defecto. Descartada.
- **P4-A — añadir `ExitActions` que devuelvan la opacidad a 1.** Mantiene el valor retenido y añade otro reloj que compite con él; el orden de preferencia entre relojes retenidos no es el orden de los `EnterActions`, así que el defecto puede reaparecer en la siguiente combinación. Se reevaluaría solo si WPF cambiara esa semántica.
- **P4-C — animar un `VisualBrush` o un `Effect` en lugar de `Opacity`.** Más coste de composición y fuera de alcance; no aporta nada frente a declarar el valor de reposo. Descartada.
- **P5-A — conservar `MainWindowRegionLayoutTests` y añadir pruebas del controlador.** Blindaría el defecto: el archivo afirma lo contrario de la invariante. **No se reevalúa.**
- **P5-C — dejar la verificación en manos de la sonda.** No sobrevive a un refactor y no reproduce la secuencia con la precisión del reloj manual. La sonda se conserva como evidencia de cierre, no como garantía.
- **`FillBehavior="HoldEnd"` como estrategia general de la vista.** Es la causa material de H-E3-06: un valor animado retenido manda sobre la propiedad y no se puede deshacer desde la vista. Se prohíbe en todo el archivo y la prohibición se vigila por prueba.
- **`TabExpansionMargin` (ADR-003, hallazgo H-7).** No se reabre: ampliar la huella plegada rompe E1-CA4 sin *hit-test* selectivo y no hace falta, porque la pestaña es de tamaño de contenido. El corolario de D1 en §4.1 descansa en que la pestaña **no** crece, así que reabrirlo obligaría a revisar también esta decisión.
- **Animar `Width`/`Height` de la ventana (ADR-003 §12 #10).** No se reabre: obligaría a un *layout* por fotograma y a `SizeChanged` en bucle, contra RNF-02, y el *slide* del contenido ya cumple §4.3-01.

---

## 14. Notas y pendientes

### 14.1 Lo que el Director debe registrar en `docs/plan/ESTADO.md`

Este ADR no modifica el tablero; estas son las entradas que corresponden registrarse:

1. **ADR-004 aprobada por el usuario el 2026-11-01**, sin cambios sobre la propuesta; pasa a *Aceptada*. E3 sigue **En curso** al 95 % y no se cierra hasta que la implementación esté verificada.
2. **H-E3-05 y H-E3-06 no se cierran con este ADR**, sino cuando la implementación esté verificada por el `qa` con la suite, los `grep` de §9.4 y la sonda de §9.5. Es el mismo criterio con el que se cerraron H-E2-01 a H-E2-04.
3. **Sustitución parcial de ADR-003** que hay que registrar con el mismo criterio que una ampliación de alcance: tabla de transiciones §7.2, contrato de `DockInteractionController` §5.3, estructura de la vista §8.2 y *storyboards* §8.3. El alcance exacto está en **§5.6**; todo lo demás de ADR-003 sigue vigente.
4. **D-14 pasa a comprobación de máquina**: no se cierra —los literales siguen en el XAML—, pero un desajuste con Core rompe la suite.
5. **D-15 no cambia**: quitar el `Background` de `RootPanel` no altera el *hit-test* de la ventana, porque la `Window` conserva `Background="Transparent"` y sigue siendo el alfa del píxel el que decide qué se entrega. D-15 sigue **abierta** como ausencia de *hit-test* selectivo, con su regla de vigilancia intacta.
6. **Verificaciones manuales nuevas M-09, M-10 y M-11** (§9.6), que se suman a M-05 a M-08.
7. **Prueba derogada y sustituida**: `tests\StickyDock.Tests\Views\MainWindowRegionLayoutTests.cs` desaparece y su lugar lo ocupa `MainWindowStoryboardContractTests.cs`. Debe constar en la bitácora para que no parezca una prueba eliminada sin justificación (§9.3).
8. **Cifra de la suite**: la estimación con método está en §9.1 y da **393** (368 − 7 − 4 + 17 + 8 + 11), con ±1 de variación por la fusión de §8.3 y por cómo cuenta el `[Theory]`. La cifra exacta la fija el `implementador` y la comprueba el `qa` contra la ejecución real; **no se admite bajar de 368** sin justificación escrita.
9. **Deuda nueva D-18, D-19 y D-20** (§14.3), y la observación de §14.2, que es de E6.

### 14.2 Observación fuera de alcance detectada al redactar este ADR

`DockInteractionController.Expand()` cancela el temporizador pendiente y pasa a `Expanded`, pero **no arma la gracia**. Hoy no tiene consecuencias porque solo lo llaman las pruebas; en E6, el «expandir» del menú de la bandeja dejaría la barra desplegada **indefinidamente**, hasta que el puntero entrara y saliera de la barra al menos una vez.

No es un defecto de E3 y **no se corrige aquí**: qué debe hacer el comando de la bandeja —armar la gracia, quedarse desplegada hasta que el usuario la cierre o mostrarse durante un tiempo acotado— es una decisión de comportamiento de E6, no de esta corrección. Se anota para que se decida con el contrato delante.

### 14.3 Deuda nueva que este ADR deja declarada

| ID | Deuda | Etapa que debería abordarla |
| --- | --- | --- |
| D-18 | **La matriz de `DockRegionPresence` y el `Visibility` del XAML son dos declaraciones del mismo hecho.** Hoy coinciden por construcción, pero la prueba de contrato no verifica la equivalencia completa «estado ↔ visibilidad»: comprueba que `Bar` está en `ContentPanel` y que existen los valores de reposo. Un elemento nuevo con una visibilidad que no siga la matriz solo se detecta midiendo | E4 (al añadir Cintas) |
| D-19 | **El orden «purga antes de `StateChanged`» no tiene prueba de orden explícita.** Se protege por inspección y por una aserción posterior; endurecerla exige afirmar `ActiveRegions` desde dentro del manejador del evento | Implementación de E3 |
| D-20 | **Una sola constante (`InteractionTimings.SlideMilliseconds`) gobierna tres duraciones**: el *slide* del despliegue, el asentamiento del controlador y el fundido de salida. Es lo que garantiza que el panel se apague justo cuando la ventana encoge, pero impide cambiar una sin las otras; separarlas exigiría una constante propia para el fundido y ajustar `FadeOutDuration_EqualsTheSettleDuration` | E8, o la etapa que ajuste las animaciones |

### 14.4 Preguntas que este ADR **no** abre

Ninguna pregunta de **requerimiento**. No hay contradicción nueva entre documentos, ningún RF queda reinterpretado y no se reduce alcance: RF02, HU02 CA #2/#3, §4.3, A4, A6, Q-03 y Q-11 se aplican tal como estaban decididos, y Q-10 sigue diferida a E4. Las únicas preguntas nuevas son de ingeniería y quedan resueltas dentro del documento (§12) o convertidas en deuda con etapa asignada (§14.3).

---

*Fin del ADR-004.*
