# Documentación de Arquitectura — StickyBar

Aplicación de escritorio nativa para **Windows 10/11 x64** construida con **.NET 10** (`net10.0-windows`) y **WPF**, diseñada bajo separación estricta **MVVM**, interoperabilidad Win32/COM aislada y presupuesto estricto de memoria privada (< 50 MB) y CPU en reposo (< 1 %).

---

## 1. Estructura de la solución

| Proyecto | Responsabilidad |
| --- | --- |
| `StickyBar.Core` | Modelos de dominio, ViewModels, máquina de estados de interacción, cálculo geométrico de docking, persistencia JSON versionada con migraciones, localización y puertos/interfaces puras. Sin referencias a WPF ni tipos Win32 (`IntPtr`/`nint`). |
| `StickyBar.Interop` | Encapsulación de `P/Invoke` (`[LibraryImport]`) y COM: lectura de `.lnk` e iconos (`ShellLinkReader`), detección de tema (`WindowsThemeService`), geometría/barra de tareas/monitores (`Win32WindowGeometrySource`, `TaskbarMonitor`, `Win32MonitorCatalog`), bandeja nativa (`Win32TrayIconService`) y auto-inicio en registro (`WindowsAutoStartService`). |
| `StickyBar.App` | Shell WPF sin lógica en code-behind: vistas (`MainWindow`, `SettingsWindow`), *attached behaviors* (`PointerRegionBehavior`, `PressDragBehavior`, `ShortcutDragDropBehavior`, `ContextMenuHoldBehavior`), convertidores, `Bootstrapper` (raíz de composición) e instancia única (`Mutex`). |
| `StickyBar.Tests` | Suite automatizada xUnit que valida dominio, máquina de estados, geometría, migraciones JSON, COM `.lnk`, registro Win32, contratos XAML e instalador. |

---

## 2. Decisiones arquitectónicas clave (ADRs)

| ADR | Decisión |
| --- | --- |
| [ADR-001](plan/decisiones/ADR-001-stack-y-arquitectura.md) | Stack **.NET 10 + WPF**, `WindowStyle="None"` + `AllowsTransparency="true"`, `DwmEnableBlurBehindWindow` y persistencia JSON. |
| [ADR-002](plan/decisiones/ADR-002-docking-y-observacion-del-area-de-trabajo.md) | Geometría en píxeles de dispositivo (`PerMonitorV2`), *move loop* nativo (`SC_MOVE`) y observación filtrada de `Shell_TrayWnd` (`SetWinEventHook` + mensajes de ventana). |
| [ADR-003](plan/decisiones/ADR-003-hover-despliegue-y-propiedad-de-la-configuracion.md) | Documento único de configuración (`IConfigurationDocument`), máquina de estados de 4 fases (`Tab`, `Arming`, `Expanded`, `Collapsing`) con ranura única de temporizador y animaciones declarativas. |
| [ADR-004](plan/decisiones/ADR-004-region-interactiva-y-ciclo-de-vida-de-los-storyboards.md) | Regiones interactivas declaradas únicamente por elementos pintados (`ContentPanel`, `RibbonPanel`, `TabBorder`), matriz `DockRegionPresence` y `FillBehavior="Stop"` en todos los `Storyboard`. |
| [ADR-005](plan/decisiones/ADR-005-ancla-de-la-barra-flotante.md) | Ancla de barra flotante fijada por instancia en `DockPlacementCoordinator` (actualizada a ancla superior-izquierda fija en `DockPlacementCalculator.GetFloatingBounds`). |
| [ADR-006](plan/decisiones/ADR-006-bandeja-nativa-win32.md) | Icono de bandeja nativo con `Shell_NotifyIconW`, `HWND_MESSAGE`, `GetPhysicalCursorPos` y `TrackPopupMenuEx` en lugar de `System.Windows.Forms.NotifyIcon`, evitando cargar WinForms y ahorrando ~20 MB de RAM privada. |

---

## 3. Subsistemas principales

### 3.1 Máquina de estados de interacción (`DockInteractionController`)
- Estados: `Tab` → `Arming` (200 ms) → `Expanded` → `Collapsing` (160 ms) → `Tab`.
- Soporta bloqueo temporal por arrastre nativo/interno (`_dragging`), botón de fijación (`IsPinned`) y apertura de menús contextuales (`IsContextMenuOpen`).
- Cero *polling* en reposo: el único temporizador activo es una ranura única (`ISingleShotScheduler`) que solo se programa durante transiciones.

### 3.2 Docking y Multimonitor
- `DockPlacementCalculator` resuelve en píxeles de dispositivo el borde acoplado (`Top`, `Left`, `Right`, `Bottom`, excluyendo el borde de la barra de tareas) con *snap* magnético de 48 px y recorte al área de trabajo (`ClampIntoWorkArea`).
- `MultiMonitorCoordinator` observa `IMonitorCatalog` (`Win32MonitorCatalog`) y `IConfigurationDocument` para abrir/cerrar barras réplicas en monitores secundarios habilitados, manteniendo sincronizados Cintas y accesos directos mediante `IConfigurationDocument.Changed` como único origen de verdad.

### 3.3 Persistencia JSON versionada (Esquema v6)
- `JsonConfigurationStore<StickyBarConfiguration>` ejecuta escrituras atómicas (`AtomicFileWriter`), tolera BOM UTF-8, encadena migraciones (`v1 → v2 → v3 → v4 → v5 → v6`) y ante un archivo corrupto se recupera con valores por defecto registrando el aviso en el log.

### 3.4 Rendimiento y huella de memoria
- `RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly`, `System.GC.ConserveMemory=5` y bandeja nativa Win32 evitan cargar `System.Windows.Forms.dll` y controladores Direct3D de escritorio para mantener el consumo en reposo por debajo de 50 MB y 1 % de CPU.
