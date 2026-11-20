# Informe de Trazabilidad — StickyBar

Artefacto de la **Fase B**: inventario normalizado de requisitos, matrices de trazabilidad directa e inversa, y veredicto de cobertura del plan.

- **Propietario**: Director de Desarrollo
- **Elaborado por**: Analista de Requerimientos · **Consolidado por**: Director de Desarrollo
- **Fecha**: 2026-10-22
- **Fuentes**: `requerimiento/Especificación de Requerimientos y Alcance - Sticky Dock Utility.md`, `requerimiento/Product Backlog e Historias de Usuario - Sticky Dock Utility.md`
- **Plan evaluado**: `docs/plan/PLAN.md` (etapas E0–E8)
- **Decisiones aplicadas**: Q-01 a Q-07 (`docs/plan/ESTADO.md`)

> Este documento define el **esquema de IDs** que citan `PLAN.md` y `ESTADO.md`. Los IDs son estables: no se renumeran, se añaden al final.

---

## 1. Esquema de identificación

| Prefijo | Alcance | Ejemplo |
| --- | --- | --- |
| `RFnn` | Requerimiento funcional (§2) | `RF01` |
| `RNF-nn` | Requerimiento no funcional (§3) | `RNF-01` |
| `§x.y-nn` | Requisito observable dentro de §4 (UI/UX) | `§4.2-01` |
| `§5.x-nn` | Requisito observable dentro de §5 (arquitectura) | `§5.2-01` |
| `HUnn` | Historia de usuario (backlog) | `HU03` |
| `HUnn-CAn` | Criterio de aceptación de una historia | `HU03-CA2` |
| `En` | Etapa del plan | `E4` |
| `Q-nn` | Pregunta abierta al usuario | `Q-07` |
| `T-nn` | Hueco de trazabilidad detectado en la Fase B | `T-04` |
| `C-n` | Contradicción detectada | `C1` |
| `A-n` | Ambigüedad detectada | `A1` |
| `R-nn` | Riesgo | `R-02` |
| `AMP-nn` | Ampliación de alcance registrada | `AMP-01` |
| `I-nn` | Idea fuera de alcance | `I-04` |

---

## 2. Inventario de requerimientos funcionales

| ID | Módulo | Texto | Vigencia |
| --- | --- | --- | --- |
| RF01 | Posicionamiento | Acople dinámico a los 3 bordes libres; arrastre entre bordes; detección de cambios de posición o resolución de la barra de tareas | Vigente |
| RF02 | Interacción | Estado semioculto, activación por hover de 200 ms, ocultamiento tras tiempo configurable | Vigente |
| RF03 | Organización | Cintas: creación y gestión; despliegue perpendicular; longitud mínima con crecimiento dinámico | Vigente |
| RF04 | Accesos directos | Tipos admitidos; configuración y ejecución con argumentos de línea de comandos | **Parcial** (Q-01, Q-06) |
| RF05 | Pantallas | Pantalla principal por defecto; «réplica **o** configuración individual independiente» | **Parcial** (Q-04) |

Matices de vigencia:

- **RF04** — la admisión directa de `.exe`, carpetas y URIs no está vigente para la vía de arrastre (Q-01): solo se admiten `.lnk`. La «configuración» de argumentos dentro de la aplicación no se implementa (Q-06): la aplicación lee y reproduce los del `.lnk`.
- **RF05** — la «configuración individual independiente» queda fuera de alcance (Q-04, registrada como `I-04`). Solo se implementa la réplica espejada.

## 3. Inventario de requerimientos no funcionales

| ID | Categoría | Texto | Verificado en |
| --- | --- | --- | --- |
| RNF-01 | Memoria | «RAM < 50 MB en estado de ejecución normal» | E1 (reposo), E8 (peor caso) |
| RNF-02 | CPU | «CPU < 1 % en estado de reposo (*idle*)» | E1 (30 s en reposo), E8 |
| RNF-03 | Compatibilidad | Windows 10 y Windows 11, x64 | E8 |
| RNF-04 | Stack | «.NET 8 / WinUI 3 o WPF» | E0 (superado por Q-02 y Q-05) |
| RNF-05 | Integración | Hooks nativos `User32.dll` / `Shell32.dll` | E2, E3, E5 |

## 4. Inventario de requisitos de UI/UX (§4)

| ID | Texto | Etapa |
| --- | --- | --- |
| §4.1-01 | Adaptación automática al tema Claro / Oscuro de Windows | E1 |
| §4.1-02 | Soporte para transparencias y estilos nativos del sistema | E1 (parcial, `I-05`) |
| §4.2-01 | Pestaña semioculta con estilo translúcido de alto contraste | E1 |
| §4.2-02 | Tema Oscuro → pestaña translúcida de tono **claro** | E1 |
| §4.2-03 | Tema Claro → pestaña translúcida de tono **oscuro** | E1 |
| §4.2-04 | Objetivo: garantizar visibilidad y rápida localización | **No normativo** (C3) |
| §4.3-01 | Transición de despliegue con animación *slide-in* | E3 |
| §4.3-02 | Hover sostenido ~200 ms con micro-efecto de expansión | E3 |
| §4.3-03 | Ocultamiento tras temporizador de gracia configurable (300–500 ms) | E3, E6 |
| §4.4-01 | Apertura animada desde el cuadro indicador hacia el interior | E4 |
| §4.4-02 | Exclusividad de apertura (acordeón) | E4 |
| §4.4-03 | Estructura plana: sin anidamiento de Cintas | E4 |
| §4.5-01 | Alta de elementos por drag & drop en la barra o cualquier Cinta | E5 |
| §4.5-02 | Solo `.lnk`; se restringe `.exe`, carpetas y scripts | E5 |
| §4.5-03 | Integridad estricta de propiedades y parámetros del `.lnk` | E5 |
| §4.5-04 | Menú contextual: mover a Cinta o a la barra, y eliminar | E5 |
| §4.5-05 | Reordenamiento por drag & drop interno | E5 |
| §4.6-01 | Preferencias desde el icono de la bandeja del sistema | E6 |
| §4.6-02 | Réplica espejada en monitores adicionales | E7 |
| §4.6-03 | Inicio automático con Windows | E6 |

## 5. Inventario de requisitos de arquitectura (§5)

| ID | Texto | Etapa |
| --- | --- | --- |
| §5.1-01 | C# / .NET 8 o superior | E0 (Q-05: .NET 10) |
| §5.1-02 | WinUI 3 o WPF con diseño Fluent y efectos de composición | E0 (Q-02: WPF; Mica/Acrylic → `I-05`) |
| §5.1-03 | Hooks de sistema: `SetWinEventHook`, `SHAppBarMessage` | E2 |
| §5.1-04 | Detección de entrada de puntero y tiempos de reposo | E3 (A6) |
| §5.1-05 | Extracción nativa de iconos y lectura de `.lnk` | E5 |
| §5.2-01 | Patrón MVVM | E0 |
| §5.2-02 | Persistencia en `%AppData%` (JSON o SQLite) | E0 (`AMP-02`: JSON) |
| §5.2-03 | Proceso de fondo sin marco con icono permanente en la bandeja | E1, E6 |

---

## 6. Matriz de trazabilidad directa (CA → etapa)

Los 11 criterios de aceptación del backlog: 2 de HU01, 3 de HU02, 2 de HU03, 2 de HU04 y 2 de HU05.

| CA | HU / RF | Criterio (abreviado) | Tipo | Verificación | Etapa | Cobertura |
| --- | --- | --- | --- | --- | --- | --- |
| HU01-CA1 | HU01 / RF01 | Arrastre hacia borde superior, izquierdo o derecho → acople magnético | Funcional | Manual + prueba de ViewModel | E2 | Completa |
| HU01-CA2 | HU01 / RF01 | Cambio de posición de la barra de tareas → reubicación automática | Funcional | Manual + prueba de servicio | E2 | Completa |
| HU02-CA1 | HU02 / RF02 | Tema Oscuro → pestaña clara; Claro → oscura | Visual | Inspección visual | E1 | Completa |
| HU02-CA2 | HU02 / RF02 | Hover ~200 ms → micro-expansión y *slide-in* | Funcional | Prueba de ViewModel + inspección | E3 | Completa |
| HU02-CA3 | HU02 / RF02 | Salida del cursor → colapso tras gracia de 300–500 ms | Funcional | Prueba de ViewModel | E3 | Completa |
| HU03-CA1 | HU03 / RF03 | Barra horizontal → cinta vertical; vertical → horizontal | Funcional | Inspección visual | E4 | Completa |
| HU03-CA2 | HU03 / RF03 | Cinta 'A' abierta + clic en 'B' → 'A' se cierra | Funcional | Prueba de ViewModel | E4 | Completa |
| HU04-CA1 | HU04 / RF04 | `.lnk` arrastrado → se agrega con icono y argumentos del archivo | Funcional | Prueba con `.lnk` de `wsl.exe` | E5 | Completa |
| HU04-CA2 | HU04 / RF04 | `.exe` o carpeta → rechazo con mensaje de empaquetado | Error | Prueba de servicio | E5 | Completa |
| HU05-CA1 | HU05 / RF05 | Clic en el icono de la bandeja → ventana de configuración | Funcional | Manual | E6 | Completa |
| HU05-CA2 | HU05 / RF05 | Réplica habilitada → cambios espejados en el monitor secundario | Funcional | Manual + prueba de sincronización | E7 | Completa |

**Resultado: 11 de 11 CA con cobertura completa.** Los criterios que exceden un CA y provienen de §4/§5 figuran en la matriz inversa.

## 7. Matriz inversa (etapa → requisitos que cubre)

| Etapa | Requisitos cubiertos | CA verificados |
| --- | --- | --- |
| E0 | §5.1-01, §5.1-02, §5.2-01, §5.2-02, RNF-04 | — (habilita al resto) |
| E1 | RF02, §4.1-01, §4.1-02, §4.2-01, §4.2-02, §4.2-03, §5.2-03, RNF-01, RNF-02 | HU02-CA1 |
| E2 | RF01, RF03 (eje), §5.1-03, RNF-05 | HU01-CA1, HU01-CA2 |
| E3 | RF02, §4.3-01, §4.3-02, §4.3-03, §5.1-04, RNF-05 | HU02-CA2, HU02-CA3 |
| E4 | RF03, §4.4-01, §4.4-02, §4.4-03, §5.2-02 | HU03-CA1, HU03-CA2 |
| E5 | RF04 (Q-01, Q-06), §4.5-01 a §4.5-05, §5.1-05, RNF-05 | HU04-CA1, HU04-CA2 |
| E6 | §4.6-01, §4.6-03, §4.3-03 (preferencia), RF02 | HU05-CA1 |
| E7 | RF05 (Q-04), §4.6-02 | HU05-CA2 |
| E8 | RNF-01, RNF-02, RNF-03 | — (verificación final) |

---

## 8. Huecos de trazabilidad detectados y su resolución

| ID | Hueco | Resolución en el plan |
| --- | --- | --- |
| T-01 | RNF-03 (Windows 10/11 x64) sin etapa que lo verifique | CA añadido en **E8**: arranque y operación en ambos sistemas |
| T-02 | RNF-01 (RAM) no se medía; E1 solo medía CPU | CA añadido en **E1**: RAM en reposo < 50 MB, medición temprana de R-02 |
| T-03 | RF01 exige detectar cambios de **resolución**; E2 solo cubría posición | CA de **E2** ampliado a resolución de monitor y de barra de tareas |
| T-04 | RF03 exige «creación y gestión» de Cintas; E4 no tenía CA | CA añadido en **E4**: crear, renombrar y eliminar con persistencia |
| T-05 | §4.4-01 exige que la animación parta del cuadro indicador | CA y entregable añadidos en **E4** |
| T-06 | §4.1-02 «estilos nativos» no es alcanzable con WPF | Deuda de ADR-001, registrada como `I-05` |
| T-07 | §4.2-01 «alto contraste y translucidez» no era observable | CA de **E1** ampliado: transparencia perceptible |
| T-08 | §4.2-04 «garantizar la visibilidad» no es observable | Declarado **no normativo** por C3 |
| T-09 | §5.1-04 «puntero global» no tenía criterio | Resuelto como interpretación **A6**: detección por área de ventana |
| T-10 | RF04 «configuración» de argumentos sin criterio | Resuelto por **Q-06**: solo lectura |
| T-11 | §5.2-01 MVVM no era verificable | CA de inspección de código añadido en **E0** |
| T-12 | E2 citaba §4.4, que trata de Cintas y no del eje de la barra | Traza corregida a RF03 (eje de la barra) |
| T-13 | E6 citaba §4.6 completo, incluida la réplica de E7 | Acotado a §4.6-01 y §4.6-03 |
| T-14 | §5.2-02 no enumera el borde acoplado ni la gracia | Registrado como `AMP-01` |

## 9. Contradicciones y ambigüedades

| ID | Tipo | Resolución |
| --- | --- | --- |
| C1 | RF04 «configuración» vs §4.5-03 «integridad estricta» | **Q-06** (usuario): solo lectura |
| C2 | EP02 «jerárquica» vs §4.4-03 «sin anidamiento» | Estructura plana: §4.4-03 es taxativo |
| C3 | §4.2-04 «visibilidad garantizada» vs §4.2-02/03 tono invertido | Prevalece la regla literal; §4.2-04 no es normativo |
| A1 | «Cuadro indicador» sin definir | Indicador clicable con etiqueta; apertura por clic |
| A2 | «Ventana/menú de configuración» desde la bandeja | Clic izquierdo → ventana; clic derecho → menú |
| A3 | Umbral del acoplamiento magnético | Parámetro de implementación: 48 px |
| A4 | Alcance del «área interactiva» | Barra desplegada + Cintas abiertas |
| A5 | Métrica de RAM y CPU | *Working set*; CPU medida durante 30 s |
| A6 | «Puntero global» | Detección por área de ventana, sin hook global |
| A7 | «Longitud mínima» de la barra | Pestaña en reposo + zona de indicadores |

Detalle completo en `docs/plan/ESTADO.md` (secciones «Preguntas abiertas», «Interpretaciones resueltas por el propio documento» y «Ampliaciones de alcance registradas»).

---

## 10. Veredicto de la Fase B

| Comprobación | Resultado |
| --- | --- |
| ¿Todo CA del backlog está cubierto por alguna etapa? | **Sí** — 11 de 11 |
| ¿Todo RF tiene al menos una etapa que lo implementa? | **Sí** — RF04 y RF05 con alcance acotado por Q-01, Q-06 y Q-04 |
| ¿Todo RNF medible tiene etapa y criterio de aceptación? | **Sí** — RNF-01 y RNF-02 en E1 y E8; RNF-03 en E8 |
| ¿Quedan contradicciones abiertas? | **No** — C1 resuelta (Q-06); C2 y C3 resueltas por el propio documento |
| ¿Quedan ambigüedades abiertas? | **No** — A1 a A7 resueltas |
| ¿Quedan huecos de trazabilidad? | **No** — T-01 a T-14 cerrados |
| ¿El plan es ejecutable sin decisiones adicionales? | **Sí** — 7 preguntas resueltas, 0 abiertas |

**Conclusión**: el plan E0–E8 es trazable, verificable y ejecutable. La cobertura de los criterios de aceptación del backlog es completa. Las dos reducciones de alcance (solo `.lnk`, solo réplica) están decididas por el usuario y registradas como desviaciones visibles (`I-04`) o como matices de vigencia de RF04 y RF05.

**Deuda asumida y visible**: `I-05` (Mica/Acrylic nativos y estilo Fluent del sistema) por la elección de WPF en ADR-001.
