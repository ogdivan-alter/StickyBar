# **Sticky Dock Utility**

# **Product Backlog & Historias de Usuario**

Especificación de requisitos funcionales y criterios de aceptación para el desarrollo de la utilidad de escritorio **Sticky Dock Utility** en entornos Windows.

---

## **Resumen de Epics**

| ID Epic | Nombre de la Epic | Historias Incluidas | Objetivo Principal |
| :---- | :---- | :---- | :---- |
| **EP01** | Núcleo UI, Acoplamiento Sticky y Control de Bordes | HU01, HU02 | Gestión del comportamientos de la barra en los bordes y ocultamiento dinámico. |
| **EP02** | Cintas y Contenedores Modulares | HU03 | Organización jerárquica y despliegue exclusivo de grupos de accesos directos. |
| **EP03** | Gestión Avanzada de Accesos Directos | HU04 | Soporte Drag & Drop e integridad de ejecución para archivos `.lnk`. |
| **EP04** | Multimonitor y Configuración del Sistema | HU05 | Administración vía System Tray y sincronización multi-pantalla. |

---

# **Epic 1: Núcleo UI, Acoplamiento Sticky y Control de Bordes**

## **HU01: Acoplamiento dinámico a bordes libres**

**Como** usuario de Windows,  
**Quiero** poder arrastrar y acoplar la barra flotante a cualquiera de los tres bordes de pantalla donde no esté la barra de tareas,  
Para organizar mi espacio de trabajo sin interferir con las herramientas nativas del sistema.

### **Criterios de Aceptación:**

* **Dado** que la barra de tareas de Windows está en el borde inferior,  
  **Cuando** el usuario arrastra la barra de la utilidad hacia los bordes superior, izquierdo o derecho,  
  **Entonces** la barra se adhiere magnéticamente (*sticky*) al borde elegido.  
* **Dado** un cambio en la posición de la barra de tareas de Windows,  
  **Cuando** el sistema notifique la reorganización del área de trabajo (*work area*),  
  **Entonces** la utilidad reubica su posición automáticamente para evitar colisiones.

---

## **HU02: Pestaña semioculta y activación por hover**

**Como** usuario enfocado en la productividad,  
**Quiero** que la barra permanezca semioculta como una pequeña pestaña de alto contraste y se despliegue mediante hover,  
**Para** no obstaculizar la visibilidad de mis aplicaciones activas.

### **Criterios de Aceptación:**

* **Dado** que la barra está en estado de reposo,  
  **Cuando** el tema de Windows sea Oscuro,  
  **Entonces** la pestaña semioculta se visualiza translúcida de tono claro (y viceversa para tema Claro).  
* **Dado** que el usuario coloca el cursor sobre la pestaña durante \~200 ms,  
  **Cuando** expira el tiempo de activación,  
  **Entonces** la barra emite un micro-efecto de expansión y se despliega mediante una animación *slide-in*.  
* **Dado** que la barra está desplegada,  
  **Cuando** el cursor sale de su área interactiva,  
  **Entonces** la barra espera un temporizador de gracia (300 ms \- 500 ms) antes de colapsar.

---

# **Epic 2: Cintas y Contenedores Modulares**

## **HU03: Creación y despliegue exclusivo de Cintas**

**Como** usuario organizado,  
**Quiero** crear Cintas para agrupar accesos directos y desplegarlas de forma perpendicular al borde,  
**Para** mantener categorías claras sin saturar la barra principal.

### **Criterios de Aceptación:**

* **Dado** una barra acoplada horizontalmente (o verticalmente),  
  **Cuando** el usuario activa una Cinta,  
  **Entonces** esta se despliega verticalmente (o horizontalmente para barra vertical) mediante una animación *slide*.  
* **Dado** que la Cinta 'A' está abierta,  
  **Cuando** el usuario hace clic en la Cinta 'B',  
  **Entonces** la Cinta 'A' se cierra automáticamente de forma exclusiva.

---

# **Epic 3: Gestión Avanzada de Accesos Directos**

## **HU04: Adición e integridad de accesos directos (.lnk)**

**Como** desarrollador / usuario avanzado,  
**Quiero** agregar accesos directos `.lnk` mediante Drag & Drop conservando sus argumentos de comandos,  
**Para** ejecutar herramientas complejas (como `wsl.exe`) directamente con un solo clic.

### **Criterios de Aceptación:**

* **Dado** un archivo `.lnk` en el explorador de archivos,  
  **Cuando** el usuario lo arrastra y suelta sobre la barra o cinta,  
  **Entonces** se agrega el elemento respetando el icono y los parámetros de línea de comandos originales.  
* **Dado** un ejecutable `.exe` o carpeta directa,  
  **Cuando** el usuario intenta arrastrarlo directamente sin ser un `.lnk`,  
  **Entonces** el sistema restringe la acción indicando que debe empaquetarse previamente como acceso directo.

---

# **Epic 4: Multimonitor y Configuración del Sistema**

## **HU05: Configuración desde System Tray y réplica multimonitor**

**Como** usuario con múltiples pantallas,  
**Quiero** gestionar los ajustes desde el icono del área de notificación e integrar réplica en monitores secundarios,  
Para disponer de los mismos accesos directos de forma homogénea en mi entorno de trabajo.

### **Criterios de Aceptación:**

* **Dado** el área de notificación de Windows (System Tray),  
  **Cuando** el usuario hace clic en el icono de la aplicación,  
  **Entonces** se abre la ventana de configuración general.  
* **Dado** que la opción de réplica multimonitor está habilitada,  
  **Cuando** el usuario agrega o reordena un acceso directo,  
  **Entonces** los cambios se reflejan de forma espejada en la barra del monitor secundario.

