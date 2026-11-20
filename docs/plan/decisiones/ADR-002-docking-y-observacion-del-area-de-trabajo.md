# ADR-002: Docking, geometría y observación del área de trabajo

- **Estado**: **Aceptada**
- **Fecha**: 2026-10-26 (aceptada por el usuario el 2026-10-28)
- **Etapa**: E2 (Acoplamiento Sticky y detección de la barra de tareas) — rediseño previo a la reimplementación
- **Trazabilidad**: RF01 (§2), RF03 (eje de la barra), RF02 (§4.2 pestaña en reposo), §5.1-03 (`SetWinEventHook`, `SHAppBarMessage`), §5.2-01 (MVVM), HU01 (CA #1, CA #2), RNF-01/RNF-02 (Q-08, A5), R-05 (DPI mixto), E2-CA1..E2-CA6
- **Reemplaza a**: el diseño implícito de E2 vigente en `DockingCoordinator`, `DockEdgeService`, `TaskbarMonitor` y `DockingHookService`

> **Resumen en una frase**: E2 se rediseña alrededor de tres puertos (`IWindowGeometrySource`, `IDockingEventSource`, `IDockWindowHost`) y de un calculador puro de geometría; Core decide **en píxeles de dispositivo**, Interop lee y aplica, la vista solo hospeda; el arrastre es un *move loop* nativo de Windows y el redimensionado se observa con `WM_SETTINGCHANGE`/`WM_DISPLAYCHANGE`/`WM_DPICHANGED`/`WM_EXITSIZEMOVE` más un `SetWinEventHook` filtrado por la barra de tareas.

---

## Contexto

La etapa E2 se declaró completada, pero la auditoría del Director (H-E2-01 a H-E2-04 en `docs/plan/ESTADO.md`) demostró que **el acoplamiento no existe**. Hechos verificados en el código:

1. `MainWindow.OnMouseLeftButtonUp` calcula el borde de destino y **descarta el resultado**: la ventana nunca se mueve (`src\StickyBar.App\Views\MainWindow.xaml.cs:139-157`).
2. `DockingCoordinator.GetTaskbarEdge()` devuelve siempre `Bottom` y `GetWorkArea()` devuelve un `1920x1080` fijo (`src\StickyBar.Core\Services\DockingCoordinator.cs:62,86`). La detección real existe en Interop, pero Core no puede verla porque **no puede referenciar** `StickyBar.Interop` (el proyecto Interop referencia a Core, no al revés).
3. `DockingCoordinator.OnWorkAreaChanged` solo escribe en el log (`:116`); el acoplamiento a la reflexión (`GetEvent("WorkAreaChanged")`, `:47,146`) es un enlace frágil y no verificable por el compilador.
4. `DockingHookService` engancha `EVENT_OBJECT_LOCATIONCHANGE` **sin filtro**: se dispara con cualquier ventana de cualquier proceso, mientras se arrastra cualquier ventana del escritorio (H-E2-04), con coste de CPU incompatible con RNF-02.
5. `Bootstrapper.ShowMainWindow` intenta crear el hook con `WindowInteropHelper(window).Handle` **antes de `Show()`**, cuando el handle todavía es cero (`src\StickyBar.App\Bootstrapper.cs:114-148`). Todo el bloque está envuelto en `catch {}` vacíos.
6. Hay **dos enums de borde duplicados** (`Core.Services.TaskbarEdge` y `Interop.Win32.TaskbarEdge`, con los mismos valores) y un tercero distinto para el acoplamiento (`Configuration.DockEdge`, con **otro orden de valores**). La conversión se resuelve hoy con casts (`MainWindow.xaml.cs:145`).
7. El arrastre, las coordenadas de pantalla y hasta un `[DllImport("dwmapi.dll")]` viven en el code-behind de la vista (H-E2-03), en contra de §5.2-01 y de E0-CA6.
8. La suite tiene **60 pruebas, ninguna de E2** (H-E2-02).

Además, tres restricciones del proyecto condicionan el diseño:

- **RNF de reposo** (Q-08): la métrica es la **memoria privada comprometida**, con 36,5 MB medidos y un presupuesto interno de **42 MB**. No se admiten cachés, polling ni temporizadores permanentes. CPU < 1 % de un núcleo en reposo.
- **R-05 (DPI mixto)**: `Window.Left/Top/Width/Height` de WPF están en unidades independientes del dispositivo y su conversión a píxeles es ambigua cuando el monitor no es el primario o tiene otra escala. E2-CA3 exige reubicar y redimensionar sin salir de pantalla tras un cambio de resolución.
- **A3**: la franja de *snap* es de **48 px**, documentada y ajustable **en un solo punto**.

Y una restricción de alcance: E2 **no** incluye multi-monitor completo (E7), ni imanes en esquinas (I-03), ni configuración de usuario nueva.

## Drivers

Ordenados por peso:

1. **Que el acoplamiento ocurra de verdad y sea verificable.** HU01 CA #1 y CA #2 son criterios observables; el diseño debe permitir probarlos sin abrir una ventana (hoy no se puede probar nada).
2. **Frontera Core ↔ Interop explícita y tipada.** `StickyBar.Core` no puede ver Win32, pero sí necesita el borde de la barra de tareas, el área de trabajo y el DPI. La frontera debe ser un contrato compilable, no reflexión.
3. **Coste en reposo (RNF-02) y huella (RNF-01/Q-08).** La observación de cambios debe ser dirigida por eventos y filtrar en origen; nada de polling ni de hooks de alcance global sin criterio.
4. **DPI mixto y cambio de topología (R-05, E2-CA3).** La geometría se decide en píxeles de dispositivo, que es la única unidad inequívoca.
5. **MVVM verificable (§5.2-01).** Ni matemática de geometría, ni P/Invoke, ni coordenadas en el code-behind.
6. **Reutilización por E7.** E7 necesita una barra por monitor; el diseño debe instanciarse una vez por ventana sin reescribirse.

## Opciones consideradas

### Reparto de responsabilidades (MVVM / Core / Interop)

#### Opción A — Coordinador en Core con tres puertos, host en la App (ELEGIDA)

- **Descripción**: Core contiene un `DockPlacementCoordinator` (decide) y un `DockPlacementCalculator` (matemática pura). Core declara tres interfaces que implementan los proyectos externos: `IWindowGeometrySource` (leo, Interop), `IDockingEventSource` (eventos, Interop + App) e `IDockWindowHost` (muevo la ventana, App).
- **Ventajas**:
  - Toda la decisión es síncrona, sin WPF, y se prueba con dobles: geometría falsa, host falso y almacén falso. Los CA de E2 se convierten en aserciones sobre el `PixelRect` que recibe el host.
  - Simétrico con el patrón ya usado para el tema (`IThemeService` en Core, `WindowsThemeService` en Interop): no inventa una arquitectura nueva.
  - E7 reutiliza el coordinador tal cual: una instancia por barra, cada una con su `IWindowGeometrySource` ligada a su ventana.
- **Desventajas / riesgos**: una interfaz con método de escritura (`ApplyBounds`) es menos "pura" que un evento; hay que documentar con precisión la afinidad de hilo.
- **Coste de adopción**: bajo-medio (7 archivos nuevos en Core, todos pequeños).

#### Opción B — El ViewModel orquesta y llama a un servicio de posicionamiento

- **Descripción**: `DockBarViewModel` calcula el borde y el rectángulo, y llama a `IWindowPlacementService.Apply(...)`.
- **Ventajas**: menos tipos; el flujo es visible en un solo sitio.
- **Desventajas / riesgos**: mete matemática de geometría y decisión de estado en una clase acoplada a `INotifyPropertyChanged`; los tests del VM acaban verificando layout; E7 obliga a replicar la orquestación en cada VM de barra; y el "servicio de posicionamiento" acaba siendo el coordinador con otro nombre.
- **Coste de adopción**: bajo, pero con deuda.

#### Opción C — Core pasivo: publica `PlacementChanged` y la vista aplica

- **Descripción**: el coordinador calcula y publica el rectángulo; la App se suscribe y mueve la ventana. Core no manda, solo informa.
- **Ventajas**: Core no depende de un puerto de escritura; la vista queda como observadora.
- **Desventajas / riesgos**: Core **sigue necesitando leer** el tamaño de la barra y su rectángulo actual (un puerto de lectura de todos modos), así que el contrato queda asimétrico; la conversión DIP→píxel se va a la App y deja de ser comprobable; y hay dos observadores (App y VM) que pueden desincronizarse.
- **Coste de adopción**: medio.

#### Opción D — Mantener el gesto en la vista (`DragMove`) y avisar al final

- **Descripción**: el code-behind llama a `DragMove()` y al soltar invoca al coordinador.
- **Ventajas**: es lo que ya hay; cero trabajo.
- **Desventajas / riesgos**: es exactamente H-E2-03; deja la decisión de acoplamiento al alcance de la vista y no permite probarla.
- **Coste de adopción**: nulo, y ya se ha pagado con una etapa reabierta.

### Observación de cambios del área de trabajo

#### Opción A — Mensajes de la ventana + `SetWinEventHook` filtrado (ELEGIDA)

- **Descripción**: `WM_SETTINGCHANGE` con `wParam == SPI_SETWORKAREA`, `WM_DISPLAYCHANGE`, `WM_DPICHANGED` y `WM_EXITSIZEMOVE` en el `HwndSource` de la barra, más un `SetWinEventHook(EVENT_OBJECT_LOCATIONCHANGE, …)` con `idProcess` de `explorer.exe` **y** filtro por ventana de la barra de tareas en el callback.
- **Ventajas**: coste en reposo **cero** (no hay nada periódico); los mensajes son exactamente los que Windows emite para lo que E2 necesita; el filtro por ventana elimina el ruido de H-E2-04; el hook cubre lo que los mensajes no cubren (auto-ocultar/mostrar la barra de tareas, que no genera `SPI_SETWORKAREA`).
- **Desventajas / riesgos**: dos fuentes que hay que componer; si `explorer.exe` se reinicia, el hook queda apuntando a un proceso muerto (lo mitiga la ruta de mensajes).
- **Coste de adopción**: medio.

#### Opción B — El hook global actual (`EVENT_OBJECT_LOCATIONCHANGE` sin filtro)

- **Ventajas**: una sola fuente, tres líneas.
- **Desventajas / riesgos**: se dispara con **cualquier** ventana del sistema; cada disparo reconsultaría geometría y posición. Con un arrastre de ventana en el escritorio son decenas de eventos por segundo. Es un incumplimiento directo de RNF-02 y el hallazgo H-E2-04.
- **Coste de adopción**: nulo, y ya se ha pagado.

#### Opción C — Polling con `DispatcherTimer` cada 250 ms

- **Ventajas**: no se pierde ningún cambio; una sola fuente; trivial de escribir.
- **Desventajas / riesgos**: introduce trabajo periódico permanente (4 despertares por segundo) contra RNF-02 y el presupuesto de 42 MB; además el reacomodo llega tarde y visible. Descartada por principio.
- **Coste de adopción**: bajo pero contrario al RNF.

#### Opción D — Registrarse como *appbar* con `SHAppBarMessage(ABM_SETPOS)`

- **Ventajas**: sería el shell quien gestionara el espacio reservado y los cambios.
- **Desventajas / riesgos**: un *appbar* **reserva espacio de escritorio**: desplazaría a las demás ventanas, que es lo contrario de una barra flotante que se oculta (§4.2). Además exige un mensaje de callback propio en la ventana. Se usa `SHAppBarMessage` **solo para consultar** (`ABM_GETTASKBARPOS`, `ABM_GETSTATE`).
- **Coste de adopción**: alto y cambia la naturaleza del producto.

## Decisión

**Se adopta la Opción A en ambas dimensiones.**

1. **Reparto**: Core decide (`DockPlacementCoordinator` + `DockPlacementCalculator`), Interop lee y engancha (`Win32WindowGeometrySource`, `WinEventDockingEventSource`, `WindowPlacement`), la App hospeda (`WpfDockWindowHost`, `WpfMessageDockingEventSource`). El ViewModel solo **refleja** el borde acoplado para las vistas; no calcula nada.
2. **Observación**: mensajes de ventana para el área de trabajo, el display y el fin de arrastre; `SetWinEventHook` filtrado por la ventana de la barra de tareas como complemento.
3. **Unidades**: Core trabaja en **píxeles de dispositivo** (`PixelRect`); el tamaño de la barra entra en unidades independientes del dispositivo (las de WPF) y Core lo convierte con el DPI del monitor.
4. **Aplicación del movimiento**: `SetWindowPos` desde Interop, invocado por la App; **nunca** `Window.Left/Top` como fuente de verdad, y **nunca** `DragMove` en el code-behind.
5. **Un solo enum de borde**: `StickyBar.Core.Docking.DockEdge`, con los valores numéricos que ya están persistidos.

El criterio que decide la balanza es la **verificabilidad**: con esta forma, cinco de los seis criterios de E2 se prueban con dobles en `tests\StickyBar.Tests` sin abrir una ventana, y el sexto (recursos del hook) se prueba sobre el wrapper de Interop. La opción B no lo consigue y la C desplaza la parte no comprobable a la App.

### Consecuencias de la decisión sobre el arrastre

El arrastre de la barra se resuelve con un **move loop nativo del sistema**, no con lógica de la vista:

- **Ruta principal**: `WindowChrome.CaptionHeight` sobre la franja de la pestaña. Windows mueve la ventana; WPF no ejecuta código. `WindowChrome` ya forma parte de ADR-001, así que no añade dependencia.
- **Ruta de contingencia** (si el *caption* no responde con `AllowsTransparency="true"`, a verificar en la implementación): `WindowPlacement.BeginNativeMove(hwnd)` → `SendMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION, 0)`, disparado por un comando del ViewModel que el coordinador reenvía al host. Sigue sin haber `DragMove`, ni matemática, ni P/Invoke en el code-behind: el gesto es una línea que invoca un comando y el resto es Interop.
- En ambas rutas, el fin del arrastre se observa como `WM_EXITSIZEMOVE`, que llega igualmente al `HwndSource`.

Ambas rutas producen el mismo comportamiento observable y el mismo criterio de aceptación; la contingencia es un detalle de implementación, no una decisión distinta. Si se activa, el diseño gana exactamente dos piezas y ninguna más: `IDockWindowHost.BeginDrag()` (implementado con `WindowPlacement.BeginNativeMove`) y un `ICommand` en `DockBarViewModel` que el coordinador reenvía al host. Nada de geometría ni de Win32 cruza esa frontera.

---

## Contratos concretos

Nombres definitivos, en inglés. `namespace StickyBar.Core.Docking;` para todo lo que sigue salvo donde se indique.

### Enum único de borde

```csharp
// src/StickyBar.Core/Docking/DockEdge.cs
/// <summary>Edge of the screen. The single edge type of the product (ADR-002).</summary>
public enum DockEdge
{
    Top = 0,
    Left = 1,
    Right = 2,
    Bottom = 3,
}
```

**Los valores numéricos son parte del contrato**: son los que hoy se persisten en `settings.json` (`dockEdge: 0..3` con el orden `Top, Left, Right, Bottom` de `StickyBarConfiguration`). Cambiarlos reinterpretaría en silencio las configuraciones existentes y obligaría a una migración de esquema que E2 no necesita. `AppBarEdgeMapping` (Interop) traduce a/desde los valores `ABE_*` de `APPBARDATA.uEdge`, que **no coinciden** (`ABE_LEFT = 0`, `ABE_TOP = 1`), en un único punto y con prueba propia.

Este enum sustituye a los tres actuales: `Configuration.DockEdge`, `Core.Services.TaskbarEdge` e `Interop.Win32.TaskbarEdge`.

### Tipos de geometría

```csharp
// src/StickyBar.Core/Docking/DockingGeometry.cs

/// <summary>Rectangle in device pixels, origin at the virtual screen's top-left corner.</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public static PixelRect Empty => new(0, 0, 0, 0);

    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

/// <summary>Monitor hosting the bar, in device pixels, plus its DPI (96 = 100 %).</summary>
public readonly record struct MonitorGeometry(PixelRect Bounds, PixelRect WorkArea, int Dpi)
{
    /// <summary>Device pixels per device-independent unit.</summary>
    public double Scale => Dpi <= 0 ? 1d : Dpi / 96d;
}

/// <summary>Size of the bar as WPF lays it out, in device-independent units.</summary>
public readonly record struct DockBarSize(double WidthDips, double HeightDips);

/// <summary>Whether the taskbar is permanently visible or auto-hides.</summary>
public enum TaskbarState
{
    Visible = 0,
    AutoHide = 1,
}

/// <summary>
/// Taskbar of the monitor that hosts the bar. <paramref name="IsOnCurrentMonitor"/> is false when the probe could
/// not determine it; the bar then excludes no edge rather than excluding a wrong one.
/// </summary>
public readonly record struct TaskbarPlacement(
    TaskbarState State,
    DockEdge Edge,
    PixelRect Bounds,
    bool IsOnCurrentMonitor);
```

### Puertos

```csharp
// src/StickyBar.Core/Docking/IWindowGeometrySource.cs
/// <summary>Reads the geometry that surrounds one bar window. Implemented by StickyBar.Interop.</summary>
public interface IWindowGeometrySource
{
    /// <summary>Monitor that currently hosts the bar window.</summary>
    MonitorGeometry GetMonitorGeometry();

    /// <summary>Taskbar as seen from the monitor that hosts the bar window.</summary>
    TaskbarPlacement GetTaskbarPlacement();
}
```

```csharp
// src/StickyBar.Core/Docking/IDockWindowHost.cs
/// <summary>
/// The one window the coordinator drives. Implemented by StickyBar.App over WPF.
/// </summary>
/// <remarks>
/// Every member must be called on the thread that owns the window. The implementation enforces it (dispatches when
/// needed); the coordinator makes no assumption about it.
/// </remarks>
public interface IDockWindowHost
{
    /// <summary>Size WPF lays out for the bar, in device-independent units.</summary>
    DockBarSize GetBarSize();

    /// <summary>Current window rectangle, in device pixels.</summary>
    PixelRect GetBounds();

    /// <summary>Moves and resizes the window. The rect is authoritative, in device pixels.</summary>
    void ApplyBounds(PixelRect bounds);
}
```

```csharp
// src/StickyBar.Core/Docking/DockingEvents.cs
/// <summary>What changed in the environment of the bar.</summary>
public enum DockingEventKind
{
    WorkAreaChanged = 0,
    DisplayChanged = 1,
    TaskbarChanged = 2,
    WindowDragCompleted = 3,
}

public sealed record DockingEventArgs(DockingEventKind Kind);

/// <summary>Source of the environment changes the docking reacts to. Disposal releases the native hooks.</summary>
public interface IDockingEventSource : IDisposable
{
    event EventHandler<DockingEventArgs>? Changed;
}

/// <summary>Fans several sources in and owns their lifetime.</summary>
public sealed class CompositeDockingEventSource : IDockingEventSource
{
    public CompositeDockingEventSource(params IDockingEventSource[] sources);
    public event EventHandler<DockingEventArgs>? Changed;
    public void Dispose();   // unsubscribes and disposes every child, once
}
```

### Calculador (puro, estático)

```csharp
// src/StickyBar.Core/Docking/DockPlacementCalculator.cs
public static class DockPlacementCalculator
{
    /// <summary>Snap band, in device-independent units (A3). Single adjustment point.</summary>
    public const double SnapThresholdDips = 48d;

    /// <summary>True when the edge is not the one the taskbar occupies on this monitor.</summary>
    public static bool IsEdgeAvailable(DockEdge edge, TaskbarPlacement taskbar);

    /// <summary>
    /// Keeps <paramref name="current"/> when it is still available; otherwise the opposite edge, otherwise the first
    /// available edge in the order Top, Left, Right, Bottom. Never fails: at most one edge hosts the taskbar.
    /// </summary>
    public static DockEdge ChooseDockEdge(DockEdge? current, TaskbarPlacement taskbar);

    /// <summary>Rectangle the bar must occupy when docked to <paramref name="edge"/>.</summary>
    public static PixelRect GetDockedBounds(DockEdge edge, MonitorGeometry monitor, DockBarSize size);

    /// <summary>Edge the bar snaps to when released, or null when it must stay floating.</summary>
    public static DockEdge? ComputeSnapTarget(
        PixelRect draggedBounds,
        MonitorGeometry monitor,
        TaskbarPlacement taskbar,
        double thresholdDips = SnapThresholdDips);

    /// <summary>Moves a floating rectangle inside the work area without changing its size, if it does not fit.</summary>
    public static PixelRect ClampIntoWorkArea(PixelRect bounds, PixelRect workArea);
}
```

### Coordinador

```csharp
// src/StickyBar.Core/Docking/DockPlacementCoordinator.cs
public sealed record DockPlacementChangedEventArgs(DockEdge? Edge, PixelRect Bounds);

/// <summary>Owns the docked state, decides the placement and persists the edge (E2).</summary>
public sealed class DockPlacementCoordinator : IDisposable
{
    public DockPlacementCoordinator(
        IDockingEventSource events,                    // owned: disposed with the coordinator
        IWindowGeometrySource geometry,
        IDockWindowHost host,
        IConfigurationStore<StickyBarConfiguration> configurationStore,
        StickyBarConfiguration configuration,
        ILogger logger);

    /// <summary>Null while the bar floats.</summary>
    public DockEdge? CurrentEdge { get; }

    public event EventHandler<DockPlacementChangedEventArgs>? PlacementChanged;

    /// <summary>Subscribes to <paramref name="events"/> and applies the persisted edge. Called once, by the App.</summary>
    public void Start();

    /// <summary>Docks to an explicit edge; ignored when the taskbar occupies it (E2-CA4).</summary>
    public void DockTo(DockEdge edge);

    /// <summary>Leaves the bar floating where it is, without moving it.</summary>
    public void Undock();

    public void Dispose();   // unsubscribes, then disposes the event source
}
```

Propiedad de recursos explícita: el coordinador **posee** el `IDockingEventSource` (lo dispone en `Dispose`), porque es su único consumidor y así E2-CA5 se verifica cerrando un solo objeto. **No** dispone el host, la geometría ni el almacén: los posee la App.

### Contrato del lado de Interop

```csharp
namespace StickyBar.Interop.Win32;

/// <summary>Translates APPBARDATA.uEdge (ABE_*) to the product's DockEdge and back.</summary>
public static class AppBarEdgeMapping
{
    public static DockEdge FromAppBarEdge(uint appBarEdge);   // ABE_LEFT = 0 … ABE_BOTTOM = 3
    public static uint ToAppBarEdge(DockEdge edge);
}

/// <summary>Safe wrapper of SHAppBarMessage(ABM_GETTASKBARPOS / ABM_GETSTATE) and of the Shell_TrayWnd fallback.</summary>
public static class TaskbarMonitor
{
    public static TaskbarPlacement GetTaskbarPlacement(nint windowHandle);
}

/// <summary>Reads monitor bounds, work area and DPI of the monitor that hosts the given window.</summary>
public sealed class Win32WindowGeometrySource : StickyBar.Core.Docking.IWindowGeometrySource
{
    public Win32WindowGeometrySource(nint windowHandle);
    public MonitorGeometry GetMonitorGeometry();
    public TaskbarPlacement GetTaskbarPlacement();
}

/// <summary>
/// EVENT_OBJECT_LOCATIONCHANGE filtered to the taskbar window of the current session.
/// Callbacks arrive on the installing thread (WINEVENT_OUTOFCONTEXT): install it on the UI thread.
/// </summary>
public sealed class WinEventDockingEventSource : StickyBar.Core.Docking.IDockingEventSource
{
    public WinEventDockingEventSource();
    /// <summary>True while at least one native hook handle is installed. Diagnostic; used to verify E2-CA5.</summary>
    public bool IsActive { get; }
    public event EventHandler<DockingEventArgs>? Changed;
    public void Dispose();   // UnhookWinEvent on every non-zero handle, then zero the fields
}

/// <summary>Classifies the window messages the docking cares about. Pure and unit-testable.</summary>
public static class DockingMessageClassifier
{
    public static bool TryClassify(int message, nint wParam, out DockingEventKind kind);
    // WM_SETTINGCHANGE (0x001A) + wParam == SPI_SETWORKAREA (0x002F) → WorkAreaChanged
    // WM_DISPLAYCHANGE (0x007E)                                 → DisplayChanged
    // WM_DPICHANGED    (0x02E0)                                 → DisplayChanged
    // WM_EXITSIZEMOVE  (0x0232)                                 → WindowDragCompleted
}

/// <summary>Window movement driven by SetWindowPos (never by Window.Left/Top).</summary>
public static class WindowPlacement
{
    public static void MoveResize(nint windowHandle, StickyBar.Core.Docking.PixelRect bounds);
    public static StickyBar.Core.Docking.PixelRect GetBounds(nint windowHandle);
    public static void BeginNativeMove(nint windowHandle);   // only for the drag contingency
}
```

### Contrato del lado de la App

```csharp
namespace StickyBar.App.Docking;

/// <summary>Adapter between the coordinator and the WPF window. Contains threading and cache plumbing only.</summary>
internal sealed class WpfDockWindowHost : IDockWindowHost
{
    public WpfDockWindowHost(Window window);
    public DockBarSize GetBarSize();
    public PixelRect GetBounds();
    public void ApplyBounds(PixelRect bounds);
}

/// <summary>Forwards classified window messages of an HwndSource to the coordinator.</summary>
internal sealed class WpfMessageDockingEventSource : IDockingEventSource
{
    public WpfMessageDockingEventSource(HwndSource source);
}
```

### Cambios en tipos existentes

```csharp
// src/StickyBar.Core/Configuration/StickyBarConfiguration.cs
public DockEdge? DockEdge { get; init; } = DockEdge.Top;   // was `null`: first run now docks at the top,
                                                           // which is what E1 already did by hand.
// The enum moves to StickyBar.Core.Docking; property name, JSON name and numeric values are unchanged.

// src/StickyBar.Core/ViewModels/DockBarViewModel.cs
public DockEdge? DockEdge { get; private set; }            // mirrors coordinator.PlacementChanged; no persistence here
```

`DockBarViewModel` **deja de persistir** el borde (hoy lo hace en `PersistDockEdgeAsync`): hay un solo escritor, el coordinador. Se eliminan también sus dependencias de `DockEdgeService` y de `IConfigurationStore`.

---

## Geometría del acoplamiento

### Conversión de unidades

```
scale = monitor.Dpi / 96                       (Dpi = GetDpiForWindow, 96 = 100 %)
w     = Round(size.WidthDips  * scale)          MidpointRounding.AwayFromZero
h     = Round(size.HeightDips * scale)
w'    = Min(w, workArea.Width)                 (si la barra no cabe, se recorta: E2-CA3)
h'    = Min(h, workArea.Height)
```

### Rectángulo por borde (píxeles de dispositivo)

Sea `wa = monitor.WorkArea`:

| Borde | X | Y | Ancho | Alto |
| --- | --- | --- | --- | --- |
| `Top` | `wa.X + (wa.Width − w') / 2` | `wa.Y` | `w'` | `h'` |
| `Bottom` | `wa.X + (wa.Width − w') / 2` | `wa.Bottom − h'` | `w'` | `h'` |
| `Left` | `wa.X` | `wa.Y + (wa.Height − h') / 2` | `w'` | `h'` |
| `Right` | `wa.Right − w'` | `wa.Y + (wa.Height − h') / 2` | `w'` | `h'` |

- Se usa el **área de trabajo**, no los límites del monitor: así la barra nunca se solapa con la barra de tareas por construcción, que es el objetivo de RF01 y de HU01 CA #2.
- **Anclaje centrado a lo largo del borde**: decisión **de ingeniería, no trazable a un RF**. Es simétrica respecto a los cuatro bordes y estable cuando la barra crece (E4). Se ajusta en `GetDockedBounds`, un solo punto.
- La división entera trunca de forma determinista; las pruebas fijan los valores esperados con números impares para blindar el redondeo.
- Ejemplo (verificable en la suite): área de trabajo `1920×1040` con la barra de tareas abajo, barra de `120×36` unidades a 125 % → `w' = 150`, `h' = 45`. `Top` → `(885, 0, 150, 45)`; `Left` → `(0, 497, 150, 45)`; `Right` → `(1770, 497, 150, 45)`; `Bottom` → `(885, 995, 150, 45)`.

### Franja de snap (A3)

```
thresholdPx = Round(48 * scale)
gap(Top)    = max(0, dragged.Y − wa.Y)
gap(Left)   = max(0, dragged.X − wa.X)
gap(Right)  = max(0, wa.Right − dragged.Right)
gap(Bottom) = max(0, wa.Bottom − dragged.Bottom)
```

- Se mide la **distancia entre el rectángulo de la barra y el borde del área de trabajo**, no la posición del cursor: es la misma magnitud que el usuario ve y no depende de por dónde se agarró la ventana.
- `max(0, …)` hace que una barra soltada **fuera** del área de trabajo (encima de la barra de tareas, o más allá) cuente como distancia cero y por tanto se acoja. Es lo que se espera al arrastrarla contra el borde.
- Se **descartan antes de medir** los bordes no disponibles (el de la barra de tareas): un candidato excluido no puede ganar por tener la menor distancia. Esto es la implementación de E2-CA4.
- Gana la distancia menor; los empates se resuelven en el orden `Top, Left, Right, Bottom`, que es el orden de declaración. Determinista y probado.
- `48` es **una constante** (`SnapThresholdDips`) y se interpreta en unidades independientes del dispositivo: el imán se siente igual al 100 % y al 150 %. Es el único punto de ajuste.

### DPI mixto (R-05)

1. **La App se declara `PerMonitorV2`** mediante `app.manifest` (`dpiAwareness`). Hoy no hay manifiesto y la aplicación es *system DPI aware*, lo que hace ambiguo `GetDpiForWindow` y descuadra `SetWindowPos` en monitores con otra escala. Esta es una decisión **de ingeniería**: no la pide ningún RF, la pide R-05. Afecta a E1 (renderizado) y debe revisarse en su medición.
2. Todo cálculo parte de `GetMonitorInfoW` (`rcWork`, `rcMonitor`), que ya devuelve píxeles de dispositivo del monitor correcto.
3. `GetDpiForWindow` da el DPI del monitor donde está la ventana; como E2 solo acopla dentro de ese monitor, la escala es la correcta.
4. **Autocorrección**: `WM_DPICHANGED` está suscrito. Si una ventana cambia de monitor y el primer cálculo usó el DPI anterior, el mensaje dispara un segundo cálculo que ya usa el nuevo. En E7 esto es lo que permite crear una barra por monitor sin lógica nueva: se aplica la posición, llega `WM_DPICHANGED` si hace falta y el resultado converge.
5. `SetWindowPos` recibe píxeles de dispositivo, que con `PerMonitorV2` son los del monitor destino: no hay conversión implícita que se pueda equivocar.

### Barra flotante

- Si el usuario suelta fuera de la franja, la barra **no se mueve** (`Undock`), y el borde persistido pasa a `null`. Es literalmente E2-CA1.
- Cuando cambia el área de trabajo y la barra está flotante, `ClampIntoWorkArea` la mete completa dentro del área si quedó fuera: eso es la parte de «sin quedar fuera de pantalla» de E2-CA3 aplicada a la barra flotante.

---

## Detección de cambios: conjunto mínimo y coste

| Señal | Origen | Motivo | Filtro aplicado | Qué dispara |
| --- | --- | --- | --- | --- |
| `WM_SETTINGCHANGE` (`wParam = SPI_SETWORKAREA`) | `HwndSource` de la barra | Windows la difunde a todas las ventanas de nivel superior cuando cambia el área de trabajo (mover/redimensionar la barra de tareas) | `wParam` exacto | `WorkAreaChanged` → recolocar |
| `WM_DISPLAYCHANGE` | `HwndSource` | Cambio de resolución o de modo de pantalla | ninguno | `DisplayChanged` → recolocar |
| `WM_DPICHANGED` | `HwndSource` | La ventana cambia de escala | ninguno | `DisplayChanged` → recolocar (convergencia) |
| `WM_EXITSIZEMOVE` | `HwndSource` | Fin del move loop nativo (arrastre del usuario) | ninguno | `WindowDragCompleted` → decidir snap |
| `EVENT_OBJECT_LOCATIONCHANGE` | `SetWinEventHook` | La barra de tareas también se mueve u oculta sin `WM_SETTINGCHANGE` (auto-ocultar) | `idProcess` = `explorer.exe`; `idObject == OBJID_WINDOW`; `hwnd` = `Shell_TrayWnd` | `TaskbarChanged` → recolocar |

Coste: **cero en reposo**. No hay temporizadores, ni polling, ni suscripción a eventos de otras ventanas. Un evento de cualquiera de las fuentes ejecuta una reconsulta de geometría (dos llamadas nativas baratas) y un `SetWindowPos`, y solo si el rectángulo calculado difiere del último aplicado.

Tres filtros evitan el ruido que hoy penaliza H-E2-04:

1. **En origen** (Interop): proceso y ventana de la barra de tareas. Una ventana cualquiera moviéndose ya no genera nada.
2. **Por cambio real de estado** (Interop): antes de elevar `TaskbarChanged`, se compara la `TaskbarPlacement` recién leída con la última; si es igual, no se eleva. Un arrastre de la barra de tareas eleva una vez por rectángulo distinto, no una vez por evento.
3. **Idempotencia** (Core): el coordinador solo llama a `IDockWindowHost.ApplyBounds` cuando el rectángulo calculado difiere del último aplicado. Un `WM_EXITSIZEMOVE` producido por un clic sin movimiento no mueve la ventana.

El gancho de `explorer.exe` se resuelve una vez al instalar. Si `explorer.exe` se reinicia, el hook deja de recibir eventos, pero la ruta de mensajes sigue viva y la siguiente reconsulta resuelve la ventana nueva: se pierde la señal de auto-ocultar, no el acoplamiento. Se registra como deuda.

---

## Restauración y cambios de topología

**Arranque** (`DockPlacementCoordinator.Start()`, invocado desde `Bootstrapper.ShowMainWindow` en el manejador de `SourceInitialized`, cuando el handle **ya existe**):

1. Suscribe a `IDockingEventSource.Changed`.
2. Lee `configuration.DockEdge` (por defecto `Top`).
3. `ChooseDockEdge(persisted, GetTaskbarPlacement())`: si el borde guardado es ahora el de la barra de tareas, cae al opuesto y, si tampoco, al primero disponible. **Esto ya es E2-CA2 en el arranque**, sin código extra.
4. `GetDockedBounds` + `ApplyBounds`. Si el borde persistido es `null`, `ClampIntoWorkArea(GetBounds())`, sin mover.
5. Registra el borde efectivo y lo persiste si cambió respecto al documento.

**Cambio de área de trabajo, de display o de la barra de tareas**: `ResolvePlacement()` = pasos 3-5 con el estado vigente. La barra conserva su borde si sigue siendo legal, o migra al opuesto si la barra de tareas acaba de ocuparlo (HU01 CA #2), y se redimensiona al nuevo tamaño (`GetDockedBounds` recorta si no cabe).

**Reacoplamiento tras cambio de topología** (conectar/desconectar un monitor, cambiar la resolución): es el mismo camino. La barra que quede fuera de toda área de trabajo se recoloca porque su borde la ancla al área vigente; si está flotante, `ClampIntoWorkArea`. No hay lógica condicional nueva, y E7 hereda el comportamiento.

**Cierre**: `App.OnExit` → `Bootstrapper.Dispose` → `coordinator.Dispose()` → baja la suscripción y dispone el `CompositeDockingEventSource`, que dispone el hook (`UnhookWinEvent` sobre cada handle no nulo). Es el recorrido que verifica E2-CA5.

---

## Mapa E2-CA → pieza del diseño → prueba

| CA | Pieza que lo satisface | Cómo | Prueba en `tests\StickyBar.Tests` |
| --- | --- | --- | --- |
| **CA1** — snap a 48 px; fuera de la franja, flotante; el borde de la barra de tareas nunca es destino | `DockPlacementCalculator.ComputeSnapTarget`; rama `WindowDragCompleted` del coordinador; `ChooseDockEdge`/`IsEdgeAvailable` | Distancia rectángulo-borde ≤ `Round(48 × scale)`, candidatos excluidos antes de medir, empate resuelto por orden de declaración | `Dock/DockPlacementCalculatorTests`: `Gap_47_Snaps`, `Gap_48_Snaps`, `Gap_49_DoesNotSnap`, `TaskbarEdge_IsNeverA_Candidate`, `BarPastTheEdge_StillSnaps`, `Tie_BreaksByDeclarationOrder`, `Threshold_ScalesWithDpi` (96/120/144). `Dock/DockPlacementCoordinatorTests`: `DragEnd_InsideBand_DocksAndPersists`, `DragEnd_OutsideBand_KeepsBarInPlace_AndPersistsNull` |
| **CA2** — al cambiar la barra de tareas se reubica sin colisionar | `IDockingEventSource` (`WorkAreaChanged`/`TaskbarChanged`), `ChooseDockEdge`, `ResolvePlacement` | Evento → reconsulta → borde válido (`actual` → `opuesto` → orden fijo) → nuevo rectángulo | `Dock/DockPlacementCalculatorTests.ChooseDockEdge_*` (16 combinaciones borde × barra de tareas, exhaustivo). `Dock/DockPlacementCoordinatorTests`: `Repositions_WhenTaskbarMovesToTheDockedEdge`, `MovesToOppositeEdge_WhenCurrentEdgeBecomesIllegal` |
| **CA3** — cambio de resolución o de barra de tareas: se reubica y redimensiona sin salir de pantalla | `GetDockedBounds` (usa `WorkArea` y recorta), `ClampIntoWorkArea`, `WM_DISPLAYCHANGE`/`WM_DPICHANGED`, `app.manifest` `PerMonitorV2` | Invariante: el rectángulo aplicado está **siempre** contenido en `WorkArea` | `Dock/DockPlacementCalculatorTests.GetDockedBounds_StaysInsideWorkArea` (matriz 4 bordes × 5 geometrías, incluidas 800×600 y área menor que la barra), `ClampIntoWorkArea_*`. `Dock/DockPlacementCoordinatorTests.Resize_ShrinksBarAndKeepsItVisible`, `DpiChange_ConvergesToSameDockedRect` |
| **CA4** — el borde de la barra de tareas nunca es destino válido | `IsEdgeAvailable`; guarda en `DockTo`; filtro previo en `ComputeSnapTarget` | Triple defensa: cálculo, comando explícito y arranque | `Dock/DockPlacementCalculatorTests.IsEdgeAvailable_*` (4×4). `Dock/DockPlacementCoordinatorTests.DockTo_TaskbarEdge_IsRejectedAndLogged` (el host no recibe `ApplyBounds` y el borde no cambia) |
| **CA5** — el hook libera recursos al cerrarse | `WinEventDockingEventSource.Dispose` (`IsActive`), `CompositeDockingEventSource.Dispose`, `DockPlacementCoordinator.Dispose` (baja la suscripción) | `UnhookWinEvent` sobre cada handle no nulo y campo a cero; el coordinador deja de escuchar | `Interop/WinEventDockingEventSourceTests`: `Dispose_ReleasesHooks` (`IsActive == false`), `Dispose_TwiceIsIdempotent`. `Dock/DockPlacementCoordinatorTests.Dispose_UnsubscribesFromEventSource` (el doble expone `SubscriberCount == 0`) y `Dispose_DisposesEventSource` |
| **CA6** — el borde acoplado persiste tras reiniciar | `StickyBarConfiguration.DockEdge`, `Start()` (paso 3-4), persistencia en `DockTo`/`Undock` | El borde efectivo se escribe en `settings.json`; al arrancar se restaura y se valida contra la barra de tareas actual | `Dock/DockPlacementCoordinatorTests.Start_AppliesPersistedEdge` (los 4 valores), `Start_WhenPersistedEdgeIsTheTaskbarEdge_FallsBack`, `DockTo_PersistsEdge`, `Undock_PersistsNull`. `Configuration/DockEdgePersistenceTests`: `dockEdge` sigue escribiéndose como número con el orden `Top=0…Bottom=3` |

Cobertura de las fuentes no probables por unidad: `WpfMessageDockingEventSource` y `WpfDockWindowHost` no se prueban (necesitan un `HwndSource`), pero **no contienen decisiones**: la clasificación de mensajes vive en `DockingMessageClassifier` (probada) y la geometría en Core (probada). Se verifican por inspección, junto con E0-CA6.

---

## Consecuencias

### Positivas

- **El acoplamiento se prueba sin ventana.** Cinco de los seis CA de E2 son aserciones sobre el `PixelRect` que recibe un host falso; la suite deja de tener cero pruebas de E2 (H-E2-02).
- **Una sola fuente de verdad por concepto**: un enum de borde, un calculador, un coordinador, un escritor de configuración. Desaparecen los casts entre enums y la doble persistencia del borde (hoy escriben `DockingCoordinator` **y** `DockBarViewModel`).
- **Sin reflexión y sin `catch {}` vacíos**: el contrato es compilable y los fallos se registran con contexto.
- **Coste en reposo nulo**: sin temporizadores ni polling; los hooks filtran en origen. Compatible con RNF-02 y con el presupuesto de 42 MB (el diseño no añade cachés ni estructuras por elemento).
- **El code-behind vuelve a no tener lógica**: se borran `DragMove`, las coordenadas de pantalla, el `[DllImport]` de `dwmapi.dll` y todo el bloqueo de `Bootstrapper` con handles en cero.
- **E7 se apoya sin reescribir**: un coordinador por ventana, cada uno con su `IWindowGeometrySource` sobre su propio handle. El resto es idéntico.

### Negativas / deuda asumida

- **`PerMonitorV2` cambia el comportamiento de escalado de toda la aplicación.** Es necesario para R-05, pero afecta a E1: la pestaña y las animaciones deben revisarse al 125 %/150 %. Se anota como deuda de verificación, no como riesgo nuevo.
- **La sonda de la barra de tareas es la primaria.** `SHAppBarMessage(ABM_GETTASKBARPOS)` devuelve la barra principal; las barras secundarias de Windows 11 no son *appbars*. E2 se queda en el monitor de la barra; E7 lo extiende.
- **Reinicio de `explorer.exe`**: el hook queda inerte hasta el siguiente arranque; la ruta de mensajes mantiene la funcionalidad principal.
- **El *caption* puede no arrastrar con `AllowsTransparency`**: se verifica al principio de la implementación y, si falla, se aplica la contingencia `WM_NCLBUTTONDOWN`, que no cambia ningún criterio de aceptación.
- **El anclaje centrado a lo largo del borde** es una decisión estética de ingeniería; si el usuario prefiere el extremo, se cambia en un punto (`GetDockedBounds`).

### Impacto en otras áreas

- **E3** hereda `DockEdge` para orientar el despliegue y el `WM_EXITSIZEMOVE` ya está en el flujo; el `CaptionHeight` de la pestaña deberá reevaluarse cuando la barra se expanda (una barra expandida no debe arrastrarse por cualquier punto). Se anota como aviso.
- **E4** no toca geometría: al cambiar el tamaño de contenido, `GetBarSize()` devuelve otro valor y el siguiente `ResolvePlacement` recoloca. La longitud mínima (A7) es un valor de layout, no de este ADR.
- **E5** no se ve afectada.
- **E7** reutiliza todo con una instancia por barra; lo único nuevo será enumerar monitores y decidir cuántas barras existen.
- **E8** hereda la medición: el docking no añade trabajo periódico, así que el presupuesto de 42 MB no cambia por esta etapa.

---

## Alternativas descartadas

- **Opción B del reparto (VM orquesta)**: el ViewModel no debe contener geometría; impide probar los CA sin WPF y duplica la orquestación en E7. Volvería a evaluarse solo si Core tuviera que desaparecer como capa, cosa que no está en el horizonte.
- **Opción C del reparto (Core pasivo)**: deja la conversión DPI y la aplicación en la App, donde no es comprobable, y crea dos observadores del mismo estado.
- **Opción D del reparto (`DragMove` en la vista)**: contradice §5.2-01 y H-E2-03. No se reevalúa.
- **Opción B de observación (hook global sin filtro)**: incumple RNF-02 con cualquier ventana en movimiento.
- **Opción C de observación (polling)**: introduce trabajo permanente, que el presupuesto y el RNF prohíben por principio.
- **Opción D de observación (registrarse como *appbar*)**: reservaría espacio de escritorio y convertiría la barra flotante en una barra de sistema. Se reevaluaría solo si el producto cambiara de naturaleza.
- **Mantener tres enums de borde y convertir con casts**: es el estado actual; se descarta porque el cast silencioso entre dos órdenes distintos (`DockEdge` y `ABE_*`) es exactamente el tipo de error que no detecta el compilador.
- **Alinear el enum único con `ABE_*` y migrar el esquema a v2**: sería más "natural" frente a `APPBARDATA`, pero reinterpreta o migra datos ya persistidos para ahorrar una tabla de conversión de cuatro entradas. Se reevaluaría si en el futuro se persiste el borde por monitor (E7) con una migración de esquema ya en curso.

---

## Lista de verificación de implementación

### Crear — `StickyBar.Core`

- [ ] `src\StickyBar.Core\Docking\DockEdge.cs` — enum único, valores `Top=0, Left=1, Right=2, Bottom=3`.
- [ ] `src\StickyBar.Core\Docking\DockingGeometry.cs` — `PixelRect`, `MonitorGeometry`, `DockBarSize`, `TaskbarState`, `TaskbarPlacement`.
- [ ] `src\StickyBar.Core\Docking\DockingEvents.cs` — `DockingEventKind`, `DockingEventArgs`, `IDockingEventSource`, `CompositeDockingEventSource`.
- [ ] `src\StickyBar.Core\Docking\IWindowGeometrySource.cs`.
- [ ] `src\StickyBar.Core\Docking\IDockWindowHost.cs`.
- [ ] `src\StickyBar.Core\Docking\DockPlacementCalculator.cs` — `SnapThresholdDips = 48`, cinco métodos puros, sin estado.
- [ ] `src\StickyBar.Core\Docking\DockPlacementCoordinator.cs` — `DockPlacementChangedEventArgs` + coordinador (`Start`, `DockTo`, `Undock`, `Dispose`), un solo escritor de `DockEdge`.

### Crear — `StickyBar.Interop`

- [ ] `src\StickyBar.Interop\Win32\AppBarEdgeMapping.cs` — traducción `ABE_*` ↔ `DockEdge`, tabla explícita.
- [ ] `src\StickyBar.Interop\Win32\Win32WindowGeometrySource.cs` — `GetMonitorGeometry` (`MonitorFromWindow` + `GetMonitorInfoW` + `GetDpiForWindow`), `GetTaskbarPlacement`.
- [ ] `src\StickyBar.Interop\Win32\WinEventDockingEventSource.cs` — hook filtrado, `IsActive`, `Dispose` idempotente.
- [ ] `src\StickyBar.Interop\Win32\DockingMessageClassifier.cs` — cuatro mensajes, `wParam` de `SPI_SETWORKAREA`.

### Crear — `StickyBar.App`

- [ ] `src\StickyBar.App\Docking\WpfDockWindowHost.cs` — `GetBarSize` (`ActualWidth/Height`), `GetBounds`, `ApplyBounds` (`WindowPlacement.MoveResize` + espejo de `Left/Top/Width/Height` con conversión por DPI, bandera de reentrada y tolerancia de 0,5 unidades; guarda de `Dispatcher`).
- [ ] `src\StickyBar.App\Docking\WpfMessageDockingEventSource.cs` — `HwndSource.AddHook` → `DockingMessageClassifier` → `Changed`; `Dispose` → `RemoveHook`.
- [ ] `src\StickyBar.App\app.manifest` — `dpiAwareness = PerMonitorV2`.

### Crear — `tests\StickyBar.Tests`

- [ ] `Docking\DockPlacementCalculatorTests.cs`
- [ ] `Docking\DockPlacementCoordinatorTests.cs`
- [ ] `Docking\CompositeDockingEventSourceTests.cs`
- [ ] `Docking\FakeDockWindowHost.cs`, `Docking\FakeWindowGeometrySource.cs`, `Docking\FakeDockingEventSource.cs` (con `SubscriberCount`), `Docking\RecordingConfigurationStore.cs`
- [ ] `Interop\AppBarEdgeMappingTests.cs`
- [ ] `Interop\DockingMessageClassifierTests.cs`
- [ ] `Interop\WinEventDockingEventSourceTests.cs`
- [ ] `Configuration\DockEdgePersistenceTests.cs`

### Modificar

- [ ] `src\StickyBar.Core\Configuration\StickyBarConfiguration.cs` — borrar el enum local, usar `StickyBar.Core.Docking.DockEdge`, valor por defecto `Top`.
- [ ] `src\StickyBar.Core\ViewModels\DockBarViewModel.cs` — quitar `DockEdgeService` y `PersistDockEdgeAsync`; reflejar `PlacementChanged`.
- [ ] `src\StickyBar.Interop\Win32\NativeMethods.cs` — añadir `GetDpiForWindow`; retirar lo que quede sin uso.
- [ ] `src\StickyBar.Interop\Win32\TaskbarMonitor.cs` — devolver `TaskbarPlacement`; derivar el borde contra el monitor de la barra (no contra la pantalla primaria); borrar `TaskbarInfo`, `InteropRect` y el enum duplicado.
- [ ] `src\StickyBar.Interop\Win32\WindowPlacement.cs` — añadir `MoveResize`, `GetBounds`, `BeginNativeMove`; borrar `WorkArea` y `WindowHelpers.GetWorkArea` (los sustituye `Win32WindowGeometrySource`).
- [ ] `src\StickyBar.App\Views\MainWindow.xaml` — `WindowChrome` con `CaptionHeight` de la pestaña, `ResizeBorderThickness=0`, `GlassFrameThickness=0`, `CornerRadius=0`.
- [ ] `src\StickyBar.App\Views\MainWindow.xaml.cs` — dejar solo `InitializeComponent` y los dos manejadores de *hover* de E1; borrar arrastre, `[DllImport]`, `OnLoaded` y el posicionamiento.
- [ ] `src\StickyBar.App\Bootstrapper.cs` — componer el grafo de docking en `SourceInitialized` (handle ya creado), suscribir `Closing`/`OnExit` al `Dispose` del coordinador, eliminar el bloque con handles en cero y los `catch {}` vacíos.
- [ ] `src\StickyBar.App\StickyBar.App.csproj` — `<ApplicationManifest>app.manifest</ApplicationManifest>`.

### Borrar

- [ ] `src\StickyBar.Core\Services\DockingCoordinator.cs` (sustituido; su matemática pasa al calculador).
- [ ] `src\StickyBar.Core\Services\DockEdgeService.cs` (la matemática válida se conserva en `DockPlacementCalculator`).
- [ ] `src\StickyBar.Core\Services\TaskbarEdge.cs` (enum duplicado).
- [ ] `src\StickyBar.Interop\Win32\DockingHookService.cs` (sustituido por `WinEventDockingEventSource`).

### Verificación previa (espiga de 15 minutos, antes de escribir el resto)

- [ ] Comprobar que `WindowChrome.CaptionHeight` arrastra la ventana con `AllowsTransparency="true"`. Si no lo hace, activar la contingencia `BeginNativeMove` y continuar: no cambia ningún contrato de este ADR.
- [ ] Si se activa la contingencia, añadir `IDockWindowHost.BeginDrag()` (+ implementación sobre `WindowPlacement.BeginNativeMove`), un `ICommand` de arrastre en `DockBarViewModel` y su enlace en `MainWindow.xaml`. Con la ruta principal, **no** se añaden.

### Verificación final

- [ ] `dotnet build -c Release` y `-c Debug` sin errores **ni advertencias** (`TreatWarningsAsErrors`).
- [ ] `dotnet test` en verde con las pruebas nuevas (la cifra de 60 sube; H-E2-02 se cierra).
- [ ] `grep` de control: ni `DragMove`, ni `DllImport`, ni `IntPtr`/`nint`, ni `catch {}` en `src\StickyBar.App\Views` y `src\StickyBar.Core`.
- [ ] Medición de CPU en reposo de 30 s tras el cambio (RNF-02) y de memoria privada comprometida (presupuesto 42 MB).

---

## Notas y pendientes

### Lo que el Director debe registrar en `docs/plan/ESTADO.md` (no lo modifica este ADR)

1. **ADR-002 aprobada por el usuario el 2026-10-28**; E2 se reimplementa contra este documento.
2. **Cierre previsto de hallazgos**: H-E2-01 (posición aplicada y detección real), H-E2-02 (pruebas), H-E2-03 (code-behind limpio), H-E2-04 (hook filtrado) — se cierran **cuando la implementación esté verificada**, no con este ADR.
3. **R-05 pasa a «Mitigado (pendiente de verificación manual)»** cuando `PerMonitorV2` esté en su sitio y las pruebas de matriz de `GetDockedBounds` estén en verde.
4. **Ampliación de alcance a registrar**: `app.manifest` con `PerMonitorV2`. No la pide ningún RF; la exige R-05. Afecta a E1 y debe entrar en su repaso de escalado.
5. **Decisiones de ingeniería, sin traza a requerimiento** (para que no se confundan con requisitos): anclaje centrado a lo largo del borde; franja de 48 px medida en unidades independientes del dispositivo; primer arranque acoplado arriba por defecto; el borde efectivo se persiste solo cuando cambia.
6. **Aviso para E3**: `CaptionHeight` igual a la altura de la pestaña deja de ser correcto cuando la barra se expande; E3 debe decidir la zona de arrastre de la barra desplegada.
7. **Deuda registrada**: barras de tareas secundarias (E7) y reinicio de `explorer.exe`.

### Preguntas que este ADR **no** abre

Ninguna. A3 fija los 48 px, Q-08 fija la métrica de memoria, ADR-001 fija el stack y Q-02/Q-05/Q-06/Q-07 no se tocan. Todo lo demás son decisiones de diseño de este documento, revisables sin cambiar el requerimiento.
