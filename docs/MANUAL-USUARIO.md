# Manual de Usuario — Sticky Dock Utility

**Sticky Dock Utility** es una barra de accesos directos flotante y acoplable a los bordes de la pantalla para **Windows 10/11 (x64)**, diseñada para mantenerse oculta en reposo como una pestaña minimalista y desplegarse al acercar el cursor.

---

## 1. Requisitos e Instalación

- **Sistema operativo**: Windows 10 x64 o Windows 11 x64.
- **Runtime**: .NET 10 Desktop Runtime (`Microsoft.WindowsDesktop.App` 10.x).

### Instalación rápida
1. Descomprima el paquete de distribución (`StickyDock-Setup-win-x64.zip`).
2. Ejecute **`Instalar-StickyDock.bat`** con doble clic.
3. Cuando el instalador pregunte si desea iniciar Sticky Dock automáticamente con Windows, responda **`S`** (Sí) o **`N`** (No).
4. La aplicación se instalará en `%LocalAppData%\StickyDock`, creará el acceso directo **Sticky Dock Utility** en el Menú Inicio y quedará registrada en *Aplicaciones instaladas* de Windows.

### Desinstalación
- Ejecute **`Desinstalar-StickyDock.bat`** o desinstale **Sticky Dock Utility** desde *Configuración de Windows → Aplicaciones → Aplicaciones instaladas*.
- El desinstalador cierra la aplicación si está activa, retira la entrada de auto-inicio (si existía), elimina el acceso directo del Menú Inicio y borra los archivos de programa.

---

## 2. Primer uso y acoplamiento a los bordes

- Al iniciar, Sticky Dock aparece como una pestaña discreta acoplada al borde superior (o en la última posición guardada).
- **Desplegar**: coloque el cursor sobre la pestaña durante **200 ms**; la pestaña emitirá un micro-efecto visual y la barra se desplegará suavemente.
- **Ocultar**: al retirar el cursor de la barra y sus Cintas, se ocultará automáticamente tras el **temporizador de gracia** (400 ms por defecto).
- **Mover y acoplar (Sticky)**:
  - Arrastre la barra desde la pestaña en reposo o desde el agarre **`⋮`** cuando está desplegada.
  - Al soltarla dentro de los **48 px** próximos a un borde libre (*Superior*, *Izquierdo* o *Derecho*, excluyendo el borde donde reside la barra de tareas), se acoplará magnéticamente a ese borde.
  - Si la suelta lejos de los bordes, permanecerá en **modo flotante** conservando su ancla superior-izquierda.

---

## 3. Organización por Cintas

Las **Cintas** permiten agrupar accesos directos en un solo nivel (estructura plana, sin subcintas):

- **Crear Cinta**: haga clic en el botón **`+`** de la barra desplegada.
- **Abrir / cerrar Cinta**: haga clic en el cuadro indicador numerado (`1`, `2`, `3`...) con la flecha direccional. Abrir una Cinta cierra automáticamente la anterior (**acordeón exclusivo**).
- **Reordenar o eliminar Cinta**:
  - Haga clic en el botón de edición **`✎`** al final de la Cinta abierta (o clic derecho sobre su cuadro indicador) para **Mover antes** (`◀`), **Mover después** (`▶`) o **Eliminar** (`✕`).
  - Si la Cinta contiene accesos directos, la aplicación pedirá confirmación indicando cuántos elementos se eliminarán.
- **Fijar barra abierta (`📌`)**: active el botón de pin en la barra desplegada si necesita mantenerla fija indefinidamente mientras organiza múltiples accesos directos.

---

## 4. Gestión de accesos directos (`.lnk`)

- **Agregar por Drag & Drop**: arrastre archivos de acceso directo (`.lnk`) desde el Explorador de Windows sobre la pestaña, sobre la barra desplegada, sobre el botón de una Cinta o dentro de una Cinta abierta.
  - *Nota*: por seguridad e integridad de ejecución, solo se admiten archivos **`.lnk`**; otros archivos (`.exe`, carpetas, etc.) serán rechazados con un aviso indicando que deben empaquetarse previamente como acceso directo.
- **Ejecutar**: haga clic sobre el acceso directo para lanzarlo conservando fielmente su destino, argumentos de línea de comandos y directorio de trabajo originales.
- **Mover y reordenar**:
  - Arrastre un acceso directo dentro de la barra o una Cinta para **reordenarlo**, o arrástrelo desde la barra hacia cualquier botón de Cinta para moverlo allí.
  - También puede usar el **clic derecho** sobre cualquier acceso directo para moverlo a otra Cinta, moverlo a la barra principal o eliminarlo.

---

## 5. Bandeja del sistema, configuración y multimonitor

El icono de Sticky Dock permanece en el área de notificación (bandeja del sistema):

- **Clic izquierdo**: abre la ventana de **Configuración**.
- **Clic derecho**: abre el menú contextual (**Configuración** / **Salir**).

En la ventana de **Configuración** puede ajustar:
- **Idioma**: **Español** (por defecto) o **English**, con aplicación inmediata.
- **Temporizador de gracia**: entre **300 ms** y **500 ms** (400 ms por defecto).
- **Auto-inicio con Windows**: habilita o deshabilita el arranque automático al iniciar sesión.
- **Réplica multimonitor**: activa el modo réplica espejada en los monitores secundarios conectados, manteniendo sincronizadas todas las Cintas y accesos directos en cada pantalla.

---

## 6. Ubicación de datos y registros

- **Configuración**: `%AppData%\StickyDock\settings.json` (escritura atómica con respaldo automático ante errores).
- **Registros (logs)**: `%AppData%\StickyDock\logs\stickydock.log`.
