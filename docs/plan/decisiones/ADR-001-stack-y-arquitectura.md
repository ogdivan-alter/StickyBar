# ADR-001: Stack de UI y estructura de la solución

- **Estado**: **Aceptada**
- **Fecha**: 2026-10-22 (aceptada por el usuario el 2026-10-22)
- **Etapa**: E0 (Cimientos y build)
- **Trazabilidad**: RF01–RF05, RNF (compatibilidad, rendimiento), §5.1 (stack y arquitectura)

## Contexto

La especificación §5.1 propone **.NET 8** con **WinUI 3 (Windows App SDK) o WPF**, efectos de composición (Mica/Acrylic), patrón MVVM, interoperabilidad Win32 por P/Invoke (`User32.dll` / `Shell32.dll`) y persistencia JSON o SQLite ligero en `%AppData%`.

La **versión concreta** de .NET se decide aparte, en la pregunta Q-05 de `docs/plan/ESTADO.md`: se adopta **.NET 10** (`net10.0-windows`), por ser la única versión LTS en soporte activo y la única instalada en el equipo de desarrollo. La mención «.NET 8» de la especificación se satisface con «.NET 8 o superior».

Esta decisión bloquea la etapa E0, porque determina la estructura de proyectos, el modelo de ventana *borderless* con `Topmost`, el mecanismo de translucidez, las animaciones de *slide*, el icono de bandeja, el drag & drop de archivos y, sobre todo, la forma de resolver los hooks Win32 (`SetWinEventHook`, `SHAppBarMessage`, detección de la barra de tareas, `TrackMouseEvent` para el hover).

## Drivers

1. **Interop Win32 y hooks de sistema.** Es la parte crítica del producto (RF01, HU01). Debe permitir hooks de eventos de ventana, notificaciones de cambio de *work area* y temporizadores de puntero sin entrar en conflicto con el ciclo de vida de la UI.
2. **RNF de reposo.** RAM < 50 MB y CPU < 1 % en reposo, con barras *always-on-top* translúcidas y animaciones.
3. **Transparencia y efectos de composición.** Pestaña translúcida de alto contraste con tono invertido según el tema (RF02, §4.2).
4. **Animaciones de *slide* de bajo coste.** Despliegue de la barra y apertura perpendicular de las cintas (§4.3, §4.4).
5. **Compatibilidad Windows 10 y 11 x64.** La instalación de Windows App SDK y su runtime son un punto de fricción real en Windows 10.
6. **Empaquetado y distribución.** Instalación limpia, auto-inicio (Startup, §4.6) y actualización.
7. **Velocidad de desarrollo y ecosistema.** MVVM, drag & drop de archivos, `NotifyIcon`, acceso a la API de accesos directos (`.lnk`) vía COM.

## Opciones consideradas

### Opción A — WPF sobre .NET 10

- **Descripción**: aplicación WPF con `AllowsTransparency`, `WindowChrome` y efectos de composición.
- **Ventajas**:
  - Funciona en Windows 10 y 11 sin runtime adicional: un único ejecutable autocontenido.
  - `WindowChrome`, `AllowsTransparency` y `SystemDropShadowChrome` resuelven bien la ventana *borderless* con `Topmost`.
  - El ecosistema arrastra ejemplos de interop Win32 muy probados (`HwndSourceHook`, `SetWinEventHook`, `SHAppBarMessage`).
  - `System.Windows.Forms.NotifyIcon` disponible para la bandeja del sistema.
  - El coste en reposo es predecible si se evita el *layout* continuo.
- **Desventajas / riesgos**:
  - `AllowsTransparency=true` desactiva el renderizado por hardware en algunos escenarios y puede encarecer el coste en reposo.
  - Mica/Acrylic no son nativos: hay que emularlos o renunciar a ellos.
  - Sin sistema de diseño visual propio: el material de Fluent no viene incluido.
- **Coste de adopción**: bajo.

### Opción B — WinUI 3 sobre .NET 10 (Windows App SDK)

- **Descripción**: aplicación WinUI 3 con backdrop Mica/Acrylic nativo, *title bar* personalizada y `AppWindow` para el modo *borderless*.
- **Ventajas**:
  - Mica/Acrylic y el contraste dinámico de tema vienen resueltos por el sistema.
  - `AppWindow` expone de forma controlada `Topmost`, tamaño y posición, que es exactamente lo que necesita el docking.
  - APIs modernas de fuentes y animaciones.
  - Camino natural hacia las APIs de Windows más recientes.
- **Desventajas / riesgos**:
  - Dependencia del runtime de Windows App SDK: hay que resolver la instalación, el *bootstrapper* o el empaquetado *MSIX* con sus requisitos de firma.
  - El modo *borderless* y los estilos de ventana requieren más trabajo manual que en WPF.
  - La interop con `HWND` y `HwndSource` es más delicada; el *threading* de la UI y el de los hooks deben coordinarse con cuidado.
- **Coste de adopción**: medio-alto.

### Opción C — WinUI 3 alojado en un proceso WPF

- **Descripción**: un proceso WPF hospeda la ventana WinUI 3 y la UI de bandeja.
- **Ventajas**: combina la madurez de WPF con los efectos nativos de WinUI.
- **Desventajas / riesgos**: dos modelos de UI, dos ciclos de vida, complejidad de empaquetado y coste de mantenimiento alto.
- **Coste de adopción**: alto. Se descarta salvo que un análisis posterior demuestre que es necesario.

## Decisión

**Se adopta la Opción A: WPF sobre .NET 10.**

Decidido por el usuario el 2026-10-22 (pregunta Q-02 en `docs/plan/ESTADO.md`). El criterio que decide la balanza es, en este orden:

1. **Compatibilidad y distribución.** Un único ejecutable autocontenido, sin runtime de Windows App SDK que instalar en Windows 10 y sin la fricción de empaquetado y firma de MSIX.
2. **Interop Win32.** `HwndSource.AddHook`, `SetWinEventHook` y `SHAppBarMessage` tienen patrones maduros y abundante documentación, lo que reduce el riesgo de RF01 y HU01 (la parte más técnica del producto).
3. **RNF de reposo.** La huella de memoria base de WPF deja más margen bajo el límite de 50 MB que el stack de WinUI 3 con Windows App SDK.

Consecuencias inmediatas para la estructura de proyectos (a concretar en E0):

- Aplicación WPF sobre .NET 10, con `net10.0-windows` y `UseWPF`.
- Ventana principal con `WindowStyle="None"`, `AllowsTransparency="true"`, `Background="Transparent"` y `Topmost="true"`; el marco y la sombra se gestionan con `WindowChrome`.
- Icono de bandeja mediante `System.Windows.Forms.NotifyIcon`, habilitando `<UseWindowsForms>true</UseWindowsForms>` únicamente en el proyecto de aplicación.
- Efectos de composición: se emulan con `DwmEnableBlurBehindWindow` (P/Invoke) donde se necesiten. Mica/Acrylic nativos quedan fuera de alcance.
- Detección del tema claro/oscuro mediante `AppsUseLightTheme` y `WM_SETTINGCHANGE` con `UImmersiveColorSet`.

## Consecuencias

- **Positivas**:
  - Sin dependencia de runtime externo: instalación y despliegue triviales.
  - Superficie de interop madura y bien documentada para los hooks del sistema.
  - Margen mayor para cumplir el RNF de RAM.
  - `WindowChrome` cubre el modo *borderless* con `Topmost` sin trabajo manual de estilo.
- **Negativas / deuda asumida**:
  - No hay Mica/Acrylic nativos: la translucidez se resuelve con opacidad por píxel y, si se desea, desenfoque de fondo vía DWM. §4.1 («efectos visuales nativos del sistema») se satisface en la práctica, no de forma literal.
  - El sistema de diseño visual es propio: hay que construir el estilo Fluent a mano o adoptar una biblioteca ligera.
  - `AllowsTransparency="true"` desactiva el renderizado por hardware en algunos escenarios: **debe medirse el coste en reposo en E1** (riesgo R-02) y la medición ya figura entre los criterios de aceptación de esa etapa.
- **Impacto en otras áreas**: define la implementación de RF01 (docking), RF02 (hover y translucidez), RF03 (cintas), RF05 (multimonitor) y §4.6 (bandeja y auto-inicio).

## Alternativas descartadas

- **Opción B — WinUI 3 sobre .NET 10**: descartada por la fricción del runtime de Windows App SDK en Windows 10, el coste de empaquetado MSIX y la huella de memoria mayor frente al RNF.
- **Opción C — WinUI 3 alojado en un proceso WPF**: descartada por complejidad, salvo revisión posterior.
- **Prototipo comparativo previo**: no se realizó. La decisión se tomó con el análisis de este ADR; las mediciones reales de RAM y CPU quedan incorporadas como criterios de aceptación de E1, de modo que la evidencia de campo sustituirá al prototipo si en la práctica resultara inadequate.

## Notas y pendientes

- La medición de RAM en reposo y de CPU con la barra colapsada es un criterio de aceptación de **E1**. Si el resultado incumpliera el RNF, esta decisión debe revisarse (ADR-001 → *Revertida*), y la alternativa entonces sería WinUI 3 con backdrop DWM en lugar de acrylic.
- Q-01 (alcance de los tipos de acceso directo admitidos) está resuelta en `docs/plan/ESTADO.md`: solo `.lnk`.
