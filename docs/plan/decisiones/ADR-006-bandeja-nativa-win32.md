# ADR-006: Icono de bandeja nativo Win32 (`Shell_NotifyIconW`)

- **Estado**: **Propuesta** (implementada para verificación de RNF-01 en E6; pendiente de ratificación del usuario)
- **Fecha**: 2026-11-14
- **Etapa**: E6 (Bandeja del sistema, ventana de configuración y auto-inicio)
- **Trazabilidad**: RNF-01 (RAM < 50 MB), Q-08 / Q-12 (presupuesto interno < 46 MB), §4.6-01, HU05 CA #1, E6-CA1, E6-CA2
- **Sustituye parcialmente a**: **ADR-001** — mecanismo del icono de bandeja (`System.Windows.Forms.NotifyIcon` → `Shell_NotifyIconW` nativo). Todo lo demás de ADR-001 (.NET 10, WPF, `DwmEnableBlurBehindWindow`, JSON) sigue vigente.

> **Resumen en una frase**: el icono de la bandeja del sistema se implementa con `Shell_NotifyIconW` y menú contextual nativo (`CreatePopupMenu` / `TrackPopupMenuEx`) en `StickyDock.Interop` en lugar de `System.Windows.Forms.NotifyIcon`, para evitar cargar `System.Windows.Forms.dll` al arranque y cumplir el RNF de memoria (< 50 MB).

---

## 1. Contexto

Al verificar la etapa **E6** en Release publicado (30 muestras a 1 s tras 5 s de estabilización), `qa` rechazó la etapa por incumplimiento de **RNF-01**:

- **E5 (línea base antes de `NotifyIcon`)**: memoria privada comprometida promedio **40,79 MB** (máx. **40,93 MB**).
- **E6 (con `WinFormsTrayIconService` usando `System.Windows.Forms.NotifyIcon` y `ContextMenuStrip`)**: memoria privada comprometida promedio **61,20 MB** (min. **56,66 MB**, máx. **62,76 MB**).

Instanciar `System.Windows.Forms.NotifyIcon` y `ContextMenuStrip` al inicio carga el ensamblado `System.Windows.Forms.dll` junto con su infraestructura de temas, accesibilidad y GDI+, sumando **~20,4 MB** de memoria privada y superando tanto el presupuesto interno de **46 MB** (Q-12) como el límite duro de **50 MB** (RNF-01).

## 2. Decisión

Se sustituye `WinFormsTrayIconService` por **`Win32TrayIconService`** dentro de `StickyDock.Interop`, manteniendo intacta la interfaz `ITrayIconService` en `StickyDock.Core`:

1. **Icono de bandeja**: `Shell_NotifyIconW` (`shell32.dll`) con `NOTIFYICONDATAW` (`NIF_MESSAGE | NIF_ICON | NIF_TIP`) y `LoadIconW(0, IDI_APPLICATION)` (recurso compartido del sistema, sin fuga de `HICON`).
2. **Ventana de mensajes**: ventana oculta nativa (`CreateWindowExW` o `HwndSource` de 0×0) en el hilo de UI para recibir el mensaje de callback `WM_TRAYICON` (`WM_APP + 1`):
   - `WM_LBUTTONUP` → eleva `SettingsRequested` (E6-CA1, A2).
   - `WM_RBUTTONUP` / `WM_CONTEXTMENU` → abre el menú contextual nativo en la posición del cursor (`GetCursorPos` + `SetForegroundWindow` + `TrackPopupMenuEx` con `TPM_RETURNCMD | TPM_RIGHTBUTTON`).
3. **Menú contextual**: `CreatePopupMenu` + `AppendMenuW` (`Settings`, separador, `Exit`) + `DestroyMenu` en `finally`, sin cargar `System.Windows.Forms.dll` ni `System.Drawing`.
4. **Liberación**: `Dispose()` ejecuta `Shell_NotifyIconW(NIM_DELETE, ...)` y destruye la ventana receptora para no dejar iconos fantasma en la bandeja al salir.

## 3. Alternativas descartadas

- **Mantener `System.Windows.Forms.NotifyIcon`**: incumple RNF-01 (61,20 MB > 50 MB).
- **Usar un paquete NuGet externo (`Hardcodet.NotifyIcon.Wpf`)**: ya descartado en ADR-001 para evitar dependencias externas adicionales; además añade objetos visuales WPF innecesarios frente a `Shell_NotifyIconW`.

## 4. Consecuencias

- **Positivas**: cero dependencias externas, cero carga de `System.Windows.Forms.dll` al arranque, retorno de la memoria privada comprometida al entorno de **~41 MB** (< 46 MB Q-12 y < 50 MB RNF-01).
- **Negativas / deuda asumida**: el menú contextual de bandeja usa el menú nativo estándar de Win32 (`TrackPopupMenuEx`), suficiente y coherente con Windows 10/11 para las dos opciones (*Settings* / *Exit*).

