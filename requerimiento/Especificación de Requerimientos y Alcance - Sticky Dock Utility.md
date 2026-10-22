# **Documento de Especificación y Alcance**

# **Sticky Dock Utility**

Especificación técnica de requerimientos para el desarrollo de la utilidad nativa de productividad en Windows.

---

# **1\. Visión General del Proyecto**

**Sticky Dock Utility** es una aplicación nativa para el sistema operativo Windows (10/11) orientada a optimizar el flujo de trabajo y la productividad del usuario. El sistema permite organizar y acceder rápidamente a accesos directos mediante una barra flotante, adherible (*sticky*) y altamente modular organizadamente estructurada a través de contenedores denominados **Cintas**.

---

# **2\. Requerimientos Funcionales**

| Código | Módulo | Descripción del Requerimiento |
| :---- | :---- | :---- |
| **RF01** | Posicionamiento | **Comportamiento Sticky y Posicionamiento:**\* La barra se acopla dinámicamente a cualquiera de los 3 bordes de la pantalla donde no se encuentre alojada la Barra de Tareas de Windows.\* Permite arrastrar libremente la barra entre los bordes disponibles.\* Detecta automáticamente cambios en la posición o resolución de la barra de tareas de Windows para evitar colisiones. |
| **RF02** | Interacción | **Estado Semioculto y Activación por Hover:**\* Permanece en estado de reposo como una pequeña pestaña minimalista en el borde de la pantalla.\* Se activa al pasar el cursor sobre ella (*hover* sostenido durante 200 ms), desplegándose de forma flotante sobre las demás ventanas.\* Se oculta automáticamente cuando el cursor sale del área interactiva tras un tiempo configurable. |
| **RF03** | Organización | **Cintas (Contenedores) y Estructura:**\* Soporta la creación y gestión de "Cintas" (contenedores lógicos de accesos directos).\* En barra horizontal, las Cintas se despliegan de forma vertical hacia el interior de la pantalla.\* En barra vertical, las Cintas se despliegan de forma horizontal hacia el interior de la pantalla.\* La barra inicia con una longitud mínima y se extiende dinámicamente a medida que se agregan elementos o Cintas. |
| **RF04** | Accesos Directos | **Gestión Avanzada de Accesos Directos:**\* Compatibilidad completa con archivos `.lnk`, ejecutables `.exe`, carpetas del sistema y URIs.\* Permite la configuración y ejecución de accesos directos con argumentos de línea de comandos (ej. invocaciones vía `wsl.exe` o scripts personalizados). |
| **RF05** | Pantallas | **Soporte Multimonitor:**\* Configuración por defecto enfocada en una única pantalla principal.\* Opción para habilitar réplica o configuración individual independiente en monitores adicionales. |

---

# **3\. Requerimientos No Funcionales**

## **Rendimiento**

* **Consumo de Memoria:** RAM \< 50 MB en estado de ejecución normal.  
* **Uso de Procesador:** CPU \< 1% en estado de reposo (*idle*).

## **Compatibilidad y Entorno**

* **Sistema Operativo:** Compatible nativamente con Windows 10 y Windows 11 (arquitectura x64).

## **Stack Tecnológico Sugerido**

* **Framework:** .NET 8 / WinUI 3 o WPF.  
* **Integración:** Hooks nativos de la API de Windows (`User32.dll` / `Shell32.dll`) para el monitoreo de eventos del sistema, detección de la barra de tareas y posicionamiento de ventanas.

---

# **4\. Especificaciones de Interfaz de Usuario (UI/UX)**

## **4.1. Temas y Adaptabilidad Visual**

* **Integración con el Sistema:** Adaptación automática al tema Claro / Oscuro (*Light/Dark Mode*) de Windows.  
* **Efectos Visuales:** Soporte para transparencias y estilos nativos del sistema.

## **4.2. Pestaña Semioculta y Estado de Reposo**

* **Estilo Translúcido de Alto Contraste:** Inspirado en la estética translúcida de Windows Vista.  
* **Lógica de Contraste Dinámico:**  
  * Si el tema de Windows es **Oscuro**, la pestaña semioculta adopta un tono **Claro** translúcido.  
  * Si el tema de Windows es **Claro**, la pestaña semioculta adopta un tono **Oscuro** translúcido.  
* **Objetivo:** Garantizar la visibilidad y rápida localización de la pestaña en los bordes de la pantalla sin interrumpir la experiencia visual del usuario.

## **4.3. Animaciones y Tiempos de Interacción**

* **Transición de Despliegue:** Animación fluida de deslizamiento desde el borde de la pantalla (*Slide-in*).  
* **Feedback de Activación (Hover):** Durante el *hover* sostenido (\~200 ms) sobre la pestaña en reposo, la pestaña emite un micro-efecto visual de leve expansión antes de desplegar completamente la barra.  
* **Ocultamiento por Temporizador:** Al retirar el cursor del área interactiva de la barra, la ventana colapsa nuevamente tras un temporizador de gracia configurable (por defecto 300 ms \- 500 ms) para prevenir cierres accidentales durante la navegación.

## **4.4. Comportamiento y Despliegue de Cintas (Contenedores)**

* **Animación de Apertura:** Despliegue mediante animación fluida de deslizamiento (*slide*) hacia el interior de la pantalla desde el cuadro indicador de la Cinta.  
* **Exclusividad de Apertura (Acordeón):** Apertura exclusiva. Al activar una Cinta, cualquier otra Cinta que se encuentre abierta dentro de la misma barra se cerrará automáticamente.  
* **Estructura Plana (Sin Anidamiento):** Las Cintas contienen exclusivamente un nivel de accesos directos. No se permite el anidamiento de Cintas dentro de otras Cintas ("sub-cintas").

## **4.5. Gestión y Administración de Accesos Directos**

* **Inclusión por Arrastrar y Soltar (Drag & Drop):** Soporte nativo para agregar elementos mediante Drag & Drop directamente a la barra principal o a cualquier Cinta.  
* **Tipos de Elementos Permitidos:** Acepta exclusivamente archivos de acceso directo (`.lnk`). Se restringe el ingreso directo de ejecutables (`.exe`), carpetas o scripts directamente sin estar empaquetados en un archivo `.lnk`.  
* **Integridad de Accesos Directos:** Los accesos directos mantienen de forma estricta las propiedades y parámetros de su archivo `.lnk` original (incluidos argumentos de línea de comandos como `wsl.exe` o flags de ejecutable).  
* **Menú Contextual (Clic Derecho):** Al realizar clic derecho sobre un acceso directo en la barra o en una cinta, se despliega un menú con las siguientes opciones:  
  * Mover/Enviar a una Cinta (o regresar a la barra principal).  
  * Eliminar elemento de la Cinta o de la Barra.  
* **Reordenamiento Dinámico (Drag & Drop Interno):** Reorganización libre de la posición de los accesos directos mediante arrastre interno entre elementos de la barra o dentro de una Cinta.

## **4.6. Configuración General, Multimonitor e Inicio del Sistema**

* **Acceso a Preferencias:** La ventana/menú de configuración general se gestiona mediante el icono de la aplicación ubicado en el área de notificación de Windows (*System Tray*).  
* **Comportamiento Multimonitor (Réplica):** Al habilitar la presencia en múltiples pantallas mediante la configuración, el contenido, accesos directos y Cintas se replican de forma espejada en las pantallas seleccionadas.  
* **Inicio de Sistema:** Opción configurable para permitir la ejecución automática de la aplicación al arrancar Windows (*Startup*).

---

# **5\. Arquitectura Técnica y Guía de Desarrollo**

## **5.1. Stack Tecnológico Recomendado**

* **Lenguaje & Runtime:** C\# / .NET 8 (o superior).  
* **Framework de UI:** WinUI 3 (Windows App SDK) o WPF (.NET 8\) con diseño Fluent y efectos de composición (Mica/Acrylic).  
* **Integración Nativa (Win32 Interop):** Uso de `User32.dll` y `Shell32.dll` mediante P/Invoke para:  
  * Hooks de eventos de sistema (`SetWinEventHook`) para monitorear cambios en la barra de tareas y reajustes del área de trabajo (`SHAppBarMessage`).  
  * Detección de entrada de puntero global y tiempos de reposo para el evento *hover*.  
  * Extracción nativa de iconos y lectura de propiedades de accesos directos `.lnk`.

## **5.2. Arquitectura del Sistema**

* **Patrón de Diseño UI:** MVVM (Model-View-ViewModel) para desacoplar la lógica de estado de la barra y las animaciones.  
* **Persistencia de Datos:** JSON / SQLite liviano en `%AppData%` para almacenar el orden de accesos directos, grupos de Cintas y preferencias multimonitor.  
* **Gestor de Procesos y System Tray:** Proceso de fondo sin marco (*borderless window*) asociado a un icono permanente en la bandeja de sistema (*System Tray*).