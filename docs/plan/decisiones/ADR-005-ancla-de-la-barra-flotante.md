# ADR-005: Ancla de la barra flotante

- **Estado**: **Aceptada** (aprobada por el usuario el 2026-11-08, sin cambios sobre la propuesta)
- **Fecha**: 2026-11-08
- **Etapa**: E4 (Cintas y despliegue exclusivo) — corrección de **H-E3-07**
- **Trazabilidad**: RF01, RF02, HU01, HU02, HU03, E3-CA1, E3-CA2, E4-CA1…E4-CA7, ADR-003 §6.3 y §12 #5, ADR-004 §9.5, Q-13, H-E3-07, QA-E3-02
- **Sustituye parcialmente a**: **ADR-003** — §6.3 (ancla por borde más cercano) y §12 #5. Todo lo demás de ADR-003 sigue vigente. **ADR-002 y ADR-004 no se modifican.**

> **Resumen en una frase**: el ancla de la barra flotante se fija en el momento de soltarla y se conserva mientras siga flotante, en lugar de re-derivarse en cada transición con `GetNearestEdge`.

---

## 1. Contexto

### 1.1 El defecto, medido

`DockPlacementCoordinator.ResolveFloating` (`src\StickyDock.Core\Docking\DockPlacementCoordinator.cs:250-276`) vuelve a derivar el ancla en **cada** transición, y la deriva con `DockPlacementCalculator.GetNearestEdge` (`:259`) sobre el rectángulo que la ventana tiene **antes** de redimensionarse (`_host.GetBounds()`, `:252`). El mismo `current` alimenta así dos estados con tamaños distintos, y el ancla puede cambiar entre ellos.

Traza medida en Release, un monitor de 1920×1080 al 100 %, barra flotante:

| Paso | Rectángulo | Ancla derivada | Distancia |
| --- | --- | --- | --- |
| Reposo | `104,104 120x36` | `Top`/`Left` empatan (104 · 104) → gana `Top` por orden de declaración | — |
| Despliegue (168×96) | `80,104 168x96` | `Top` | salto de **24 px** en X |
| Colapso (120×36) | `80,134 120x36` | sobre el rectángulo **desplegado**, `Left` (80) gana a `Top` (104) → **cambia a `Left`** | deriva de **24 px en X y 30 px en Y** |

Son **dos causas encadenadas**, no una: (1) el ancla se deriva sobre el rectángulo de reposo, donde `Top` y `Left` empatan y el desempate hace que la barra se recentre en X al crecer; (2) al colapsar se deriva otra vez, ahora sobre el rectángulo desplegado, y el ancla cambia de lado. El primer efecto es una decisión de ingeniería de ADR-003 §6.3 (anclar el lado más cercano y recentrar el otro eje); el segundo es el defecto.

### 1.2 Lo que **no** es la causa

`DockPlacementCalculator.GetFloatingBounds` (`src\StickyDock.Core\Docking\DockPlacementCalculator.cs:194-216`) es determinista para un rectángulo, un ancla y un tamaño dados, y `GetNearestEdge` (`:162-180`) también. Ambos están cubiertos por la suite y **no se tocan** (QA-E3-02). Tampoco es la causa el orden de desempate: `GetNearestEdge` rompe empates con `<` estricto en el orden `Top`, `Left`, `Right`, `Bottom` (`:172`), y eso es parte de su contrato.

### 1.3 Estado actual del ancla

Hoy no existe: nadie la guarda. `HandleDragCompleted` (`:317-354`) deja flotante la barra por `ReleaseToFloating` (`:345`, `:361-374`) sin ancla, `Undock` (`:138-156`) tampoco la fija, y `Start` (`:94-111`) solo llega a derivarla de forma implícita a través de `ResolvePosition` → `ResolveFloating`. El único rastro del ancla es la llamada de `:259`, que se repite en cada transición.

### 1.4 Por qué se corrige ahora y aquí

Q-13 adoptó la opción 1: corregir al abrir E4. E4 es la **primera etapa que vuelve a cambiar el largo de la barra** con `NotifyBarSizeChanged` (`:180-191`), de modo que multiplica exactamente esta clase de defecto; corregirlo después cuesta más y con más geometría encima. La corrección entra en el contrato de E4 y H-E3-07 se cierra cuando esté implementada y verificada por `qa`.

**Naturaleza**: es un defecto de **geometría** de una decisión de ingeniería (ADR-003 §6.3 y §12 #5), **no un criterio de aceptación de E3**. No afecta a la visibilidad, al colapso ni al resto de los CA de E3, y no reabre la etapa.

## 2. Drivers

1. **Corregir H-E3-07 sin reabrir E3.** Un ciclo completo de despliegue y colapso debe devolver la barra **exactamente** al rectángulo de reposo (§4.3-01, HU02 CA #2/#3).
2. **Que el ciclo sea demostrable con números**, no a ojo: `104,104` dentro, `104,104` fuera. La corrección se juzga por igualdad de rectángulo, no por impresión visual.
3. **RNF-02: cero trabajo periódico.** Sin temporizadores, sin sondeo y sin *hooks* nuevos. La corrección tiene que ser estado puro.
4. **RNF-01 / Q-08.** El estado añadido es **un campo** de un enum anulable. El presupuesto vigente de 46 MB de memoria privada comprometida no se mueve.
5. **Radio de cambio mínimo y reversible.** `GetFloatingBounds`, `GetDockedBounds`, `ClampIntoWorkArea`, `GetNearestEdge`, `IDockingEventSource`, ADR-002 y ADR-004 quedan como están: ya son correctos y ya están probados.
6. **Que E7 no tenga que reescribirlo.** Estado por instancia, de modo que una barra por monitor obtenga su propio ancla sin código nuevo.
7. **(Ingeniería)** **Una sola fuente de verdad para el ancla.** Mientras se derive en un solo punto y se lea en todos los demás, no puede divergir consigo misma.

## 3. Opciones consideradas

### Opción A — Ancla como estado del coordinador, fijada al soltar (ELEGIDA)

- **Descripción**: `DockPlacementCoordinator` guarda `DockEdge? _anchor` por instancia. Se escribe cuando la barra queda flotante (soltar, `Undock`, primer arranque flotante y recolocación por área de trabajo) y se borra al acoplar. `ResolveFloating` **lee** el ancla y deja de derivarla en la ruta normal.
- **Ventajas**: el ancla deja de depender del historial de tamaños; el ciclo se cierra por construcción; un solo campo y un solo ayudante; no toca ni un cálculo; el estado es por instancia, así que E7 lo hereda.
- **Desventajas / riesgos**: hay que decidir **cuándo** se fija (es lo que fijan las reglas D2, D3 y D6) y una excepción documentada cuando el área de trabajo obliga a mover la barra.
- **Coste de adopción**: bajo (un campo, un ayudante, cuatro asignaciones).

### Opción B — Anclar el rectángulo: crecer siempre hacia abajo y a la derecha, sin recentrar

- **Descripción**: fijar la esquina superior izquierda y dejar que la barra crezca desde ahí.
- **Ventajas**: el ciclo también se cerraría, y además sin salto en el despliegue.
- **Desventajas / riesgos**: cambia ADR-003 §6.3 **para todos los casos**, no solo para el flotante; haría que una barra cercana al borde inferior o derecho creciera fuera del área de trabajo y dependiera del `Clamp`, y ningún CA pide «crecer hacia un lado fijo». Corregir un defecto de 24 px mediante un cambio de comportamiento mucho mayor no es proporcionado.
- **Coste de adopción**: medio, con más superficie que revisar.

### Opción C — Persistir el ancla en la configuración

- **Descripción**: añadir `floatingAnchor` a `settings.json`.
- **Ventajas**: sobreviviría al reinicio.
- **Desventajas / riesgos**: la **posición** de la barra flotante no se persiste (§5.2-02 y AMP-01 enumeran orden, Cintas, preferencias multimonitor y, como ampliación, el borde acoplado y la gracia). Un ancla sin posición guardada no significa nada: al arrancar, la barra aparece donde el sistema la coloca y el ancla guardada sería falsa. Además sube el esquema a v3 sin necesidad.
- **Coste de adopción**: medio, y crea deuda.

### Opción D — Conservar el comportamiento y documentarlo como conocido

- **Descripción**: opción 3 de Q-13.
- **Ventajas**: coste cero.
- **Desventajas / riesgos**: exige justificar que la barra «salte» 24 px en X y no vuelva a su sitio al desplegarse y colapsar, con E4 cambiando el largo de la barra encima. **Descartada por el usuario en Q-13**, que eligió la opción 1.

**Decisión: Opción A.**

## 4. Decisión

Se adopta la Opción A. Se concreta en ocho reglas, todas verificables.

### D1 — El ancla es estado del coordinador, por instancia, y no se persiste

`private DockEdge? _anchor;` en `DockPlacementCoordinator`, junto al resto del estado mutable (`:39-41`). **No** entra en `StickyDockConfiguration`, **no** se escribe en disco y **no** se comparte entre instancias: E7 construye un coordinador por barra y cada uno tendrá el suyo, ligado a la geometría de su monitor.

`null` significa **«derívalo»**, no «no hay ancla»: es el valor que tiene al arrancar y tras acoplar, y el único punto que lo interpreta es `ResolveFloating`.

### D2 — Se fija al soltar la barra y al desacoplarla

- **`HandleDragCompleted` → `ReleaseToFloating`** (`:345`, `:361-374`): la barra queda flotante en la posición soltada. El ancla se fija con el rectángulo que la barra conserva, **antes** del `return false` de `:366-369`, que se dispara cuando ya estaba flotante y que hoy corta el camino sin hacer nada.
- **`Undock()`** (`:138-156`): igual, **antes** del retorno temprano de `:148-151` (`previousEdge is null`), con el rectángulo ya capturado en `_lastApplied` (`:146`).

Ambos lo hacen con un único ayudante privado:

```
SetFloatingAnchor(PixelRect bounds)
    => _anchor = DockPlacementCalculator.GetNearestEdge(bounds, _geometry.GetMonitorGeometry().WorkArea);
```

Un solo cálculo, dos llamadas. Es el único sitio, además del arranque, en que el ancla se **deriva**.

### D3 — Se deriva una vez en el primer arranque flotante

Cuando el borde persistido es `null`, `Start` (`:94-111`) llega a `ResolvePlacement` (`:230-241`) → `ResolveFloating`. Allí la derivación es perezosa:

```
_anchor ??= DockPlacementCalculator.GetNearestEdge(current, workArea);
```

**`Start` no cambia.** La derivación ocurre en la primera llamada a `ResolveFloating` y queda congelada desde ese momento.

**Orden obligatorio dentro de `ResolveFloating`**: la derivación va **antes** de la rama de no-operación (`:267-271`). Si se dejara después y el tamaño ya coincidiera, la primera llamada saldría sin fijar el ancla y la siguiente transición la derivaría sobre el rectángulo ya desplegado, es decir, se reintroduciría H-E3-07 exactamente por la puerta de atrás. Es un detalle de una línea y es la condición de que D3 signifique algo.

### D4 — Se borra al acoplar

`DockTo(edge)` (`:118-133`) pone `_anchor = null`: un ancla solo tiene sentido mientras la barra flota. Si más tarde la barra se suelta, D2 la vuelve a fijar con el rectángulo vigente.

`Dispose` (`:194-209`) **no** lo limpia: el estado muere con la instancia y no hay nada que liberar.

### D5 — `ResolveFloating` conserva el ancla y deja de llamar a `GetNearestEdge` en la ruta normal

El cuerpo de `:250-276` pasa a ser: rectángulo actual → derivación perezosa (solo si `_anchor` es `null`) → `GetFloatingBounds(current, _anchor.Value, size)` → `ClampIntoWorkArea` → aplicación idempotente. La llamada de `:259` **desaparece de la ruta normal**: el ancla ya no se consulta al entorno en cada transición, así que `NotifyBarSizeChanged` (`:180-191`) y los eventos de `IDockingEventSource` (`WorkAreaChanged`, `DisplayChanged`, `TaskbarChanged`, `WindowDragCompleted`) no pueden cambiarla. Eso es lo que cierra la segunda causa de §1.1.

Y como corolario, el ciclo queda exacto: con un ancla **fija**, `GetFloatingBounds` conserva el lado anclado y recentra el otro eje, así que crecer y después encoger devuelve el rectángulo idéntico —`(104,104,120,36)` → `(80,104,168,96)` → `(104,104,120,36)`—. El criterio de corrección es por **igualdad**, no por tolerancia.

### D6 — `ClampIntoWorkArea` es el único movimiento restante, y entonces el ancla se re-deriva

`ClampIntoWorkArea` (`DockPlacementCalculator.cs:228-242`) sigue dentro de `ResolveFloating` y es legítimo: si el área de trabajo cambia y la barra ya no cabe, hay que meterla dentro (E2-CA3 / E4-CA3). Cuando el recorte **mueve** el rectángulo, el ancla deja de corresponder a ninguna geometría del escritorio y se re-deriva con el ayudante de D2 sobre el rectángulo ya recortado.

Es la **única excepción documentada** a D5, y es deliberada: preferimos un ancla correcta a un ancla inmutable. El punto se identifica por comparación entre el rectángulo antes y después del `Clamp`, no por un indicador nuevo.

### D7 — No se toca nada más

`GetFloatingBounds`, `GetDockedBounds`, `ClampIntoWorkArea` y `GetNearestEdge` se conservan tal cual. `IDockingEventSource`, `DockPlacementCalculator`, ADR-002 y ADR-004 tampoco cambian. **Ni un temporizador, ni un sondeo, ni un estado persistido**: el reposo de RNF-02 sigue costando cero y el campo añadido no mueve el presupuesto de 46 MB de Q-12.

### D8 — El ancla no depende del estado de interacción

El ancla es ortogonal a `DockState`: no cambia al desplegarse ni al colapsarse. Solo cambia cuando la barra cambia de naturaleza (acoplar, soltar, desacoplar) o cuando el recorte la mueve.

| Situación | `_anchor` | Tamaño efectivo | Quién lo escribe |
| --- | --- | --- | --- |
| Acoplada (cualquier `DockState`) | `null` | acoplado | `DockTo` (o el valor inicial al arrancar acoplada) |
| Flotante, `Tab` | valor | plegado | soltar / `Undock` / primer `ResolveFloating` / recorte |
| Flotante, `Arming` | **el mismo** | plegado | nadie |
| Flotante, `Expanded` | **el mismo** | desplegado | nadie |
| Flotante, `Collapsing` | **el mismo** | desplegado | nadie |

`Tab` y `Arming` comparten el tamaño plegado, y `Expanded` y `Collapsing` el desplegado: por eso un ciclo completo atraviesa dos veces cada tamaño con el mismo ancla y vuelve al punto de partida.

### Traza corregida

| Paso | Antes (medido) | Con ADR-005 |
| --- | --- | --- |
| Reposo | `104,104 120x36` | `104,104 120x36` |
| Despliegue | `80,104 168x96` (ancla derivada, empate → `Top`) | `80,104 168x96` (la misma, ya congelada) |
| Colapso | `80,134 120x36` (ancla re-derivada → `Left`) | **`104,104 120x36`** |

La deriva pasa de 24 px en X y 30 px en Y a **cero**. El desplazamiento de 24 px en X **durante** el despliegue se mantiene y es correcto: es el recentrado de ADR-003 §6.3, que este ADR no cambia.

## 5. Mapa hallazgo → pieza → prueba

Todas las pruebas viven en `tests\StickyDock.Tests\Docking\DockPlacementCoordinatorTests.cs`, sobre los dobles ya existentes (`FakeDockWindowHost`, `FakeWindowGeometrySource`, `FakeDockingEventSource`, `RecordingConfigurationStore`).

| Hallazgo / regla | Pieza | Prueba |
| --- | --- | --- |
| **H-E3-07** — deriva entre estados | `_anchor` congelada + `GetFloatingBounds` leído de ella (D1, D5) | `FloatingCycle_DeployAndCollapse_ReturnsToTheSameRestingRect` |
| **D5** — los eventos del entorno no mueven el ancla | ancla leída, no derivada, en la ruta normal | `NotifyBarSizeChanged_OfAFloatingBar_KeepsTheAnchoredSideFixed` |
| **D3** — derivación única al arrancar flotante | `_anchor ??=` antes de la rama de no-operación | `Start_WithAPersistedFloatingEdge_DerivesTheAnchorOnce_AndKeepsItAcrossTransitions` |
| **D2 / D4** — fijar al soltar, borrar al acoplar | `SetFloatingAnchor` en `ReleaseToFloating` y `Undock`; `_anchor = null` en `DockTo` | `DockTo_ClearsTheAnchor_AndUndockSetsItAgain` |
| **D6** — recorte que mueve la barra | re-derivación tras `ClampIntoWorkArea` | `WorkAreaChange_ThatForcesTheClamp_ReDerivesTheAnchor_AndLeavesTheBarInsideTheArea` |

La primera es la **regresión de H-E3-07** y se afirma por igualdad estricta de rectángulo: parte de `104,104 120x36` flotante, despliega, colapsa y exige `104,104 120x36`. La segunda comprueba que el lado anclado no se mueve al cambiar el tamaño. La tercera comprueba que el ancla se fija una vez y que las transiciones siguientes no la alteran. La cuarta comprueba el ciclo completo acoplar → desacoplar. La quinta comprueba la excepción de D6 sin debilitar E2-CA3.

## 6. Verificación

### 6.1 Compilación y suite

```powershell
dotnet build -c Release --no-incremental
dotnet build -c Debug --no-incremental
dotnet test
```

Criterio: **0 errores y 0 advertencias** en ambos perfiles (`TreatWarningsAsErrors`) y suite en verde, **sin esperas reales** (el reloj de las pruebas es manual). La línea base al cerrar E3 es de **390 pruebas**; no se admite bajar de esa cifra sin justificación escrita, y las cinco pruebas del §5 se suman a ella.

### 6.2 Greps de control

Ejecutados desde la raíz del repositorio, con `Get-ChildItem -Recurse`. **No** se usa `Select-String -Path src\**\*.cs`: `-Path` no expande `**` de forma recursiva en Windows PowerShell 5.1 y daría un falso verde.

```powershell
# 1. El ancla no se persiste: no debe existir ninguna propiedad nueva de configuración.
Get-ChildItem -Path src\StickyDock.Core\Configuration -Recurse -Include *.cs |
    Select-String -Pattern 'DockAnchor|AnchorEdge|FloatingAnchor'

# 2. La ruta normal de ResolveFloating no re-deriva el ancla.
Get-ChildItem -Path src\StickyDock.Core\Docking -Recurse -Include *.cs |
    Select-String -Pattern 'GetNearestEdge' -Context 2,2

# 3. El estado nuevo es local al coordinador.
Get-ChildItem -Path src,tests -Recurse -Include *.cs |
    Select-String -Pattern '\b_anchor\b'
```

Resultado exigido: el primero **sin coincidencias**; en el segundo, `GetNearestEdge` no aparece nunca como argumento directo de `GetFloatingBounds` y toda aparición está dentro de `SetFloatingAnchor` o de la derivación perezosa de D3; en el tercero, `_anchor` solo aparece en `DockPlacementCoordinator.cs` y en las pruebas de §5.

### 6.3 Comprobación funcional en Release

1. **Ciclo flotante exacto**: soltar la barra en `104,104 120x36`, desplegarla, colapsarla y medir el rectángulo final. Debe ser **exactamente `104,104 120x36`**. Es la comprobación que fallaba (antes: `80,134 120x36`).
2. **Sonda de ADR-004 §9.5 intacta**: los 19 puntos siguen en **PASS / 0 FAIL** en los cuatro bordes y en flotante. La corrección no puede alterar el *slide*, el colapso, la visibilidad del panel ni la región interactiva.

## 7. Consecuencias

### Positivas

- **H-E3-07 queda corregido por construcción**, no por ajuste: el ancla deja de depender del historial de tamaños, y el ciclo se cierra por igualdad de rectángulo. Es una aserción, no una impresión visual.
- **E4 puede cambiar el largo de la barra sin reproducir el defecto.** `NotifyBarSizeChanged` y los eventos del entorno ya no consultan el ancla, que es justo el escenario que Q-13 anticipaba.
- **Coste en reposo nulo**: ni temporizadores, ni sondeo, ni estado persistido. RNF-02 sigue en cero y RNF-01 no se mueve: el estado añadido es un campo de un enum anulable.
- **Radio de cambio mínimo**: un campo, un ayudante de una línea y cuatro asignaciones. Los cuatro métodos puros del calculador y sus pruebas siguen intactos.
- **E7 lo hereda sin código**: el ancla es por instancia, así que cada barra por monitor obtiene la suya ligada a su propia geometría.

### Negativas / deuda asumida

- **El ancla se pierde al reiniciar.** Es coherente con que tampoco se persista la posición flotante, pero significa que el ancla de una barra que arranca flotante se re-deriva en cada sesión a partir de donde el sistema la coloque.
- **La separación entre «derivar» y «leer» es una regla de disciplina, no del compilador.** El grep del §6.2 la vigila; si una etapa futura vuelve a llamar a `GetNearestEdge` dentro de `ResolveFloating`, la suite de regresión lo detecta por el comportamiento, no por el tipo.
- **D6 admite un movimiento de la barra que el ancla no predice.** Es deliberado y está acotado al caso en que el área de trabajo cambia y la barra deja de caber.

### Impacto en otras áreas

- **E4 (Cintas)**: `NotifyBarSizeChanged` gana una garantía nueva —el lado anclado no se mueve— y el largo de la barra puede cambiar sin tocar el ancla. La corrección de `DockBarMetrics` y la coherencia de tamaño (D-12) no se ven afectadas.
- **E7 (multimonitor)**: hereda el ancla por instancia; no necesita sincronizarla, porque cada barra tiene la suya.
- **E8 (rendimiento y entrega)**: sin cambios; la medición de reposo no varía y M-09 a M-11 siguen vigentes. La comprobación funcional del §6.3 se añade al informe de entrega.
- **E1, E2, E3**: sin cambios de contrato. La huella plegada sigue siendo `120×36` (E1-CA4 y D-15 no se tocan) y E3-CA1/E3-CA2 se conservan.

## 8. Alternativas descartadas

- **Opción B — anclar el rectángulo y crecer hacia abajo y a la derecha**: cerraría el ciclo sin salto, pero cambia ADR-003 §6.3 para todos los casos, no solo para el flotante, y puede sacar la barra del área de trabajo. Se reevaluaría solo si el usuario pidiera explícitamente que la barra flotante no se recentre al crecer.
- **Opción C — persistir el ancla**: un ancla sin posición persistida es una afirmación falsa al arrancar, y sube el esquema sin necesidad. Se reevaluaría si algún día se persiste también la posición de la barra flotante.
- **Opción D — conservar el comportamiento**: descartada por el usuario en Q-13; el salto de 24 px en X es perceptible y E4 lo multiplicaría.
- **Derivar el ancla después del redimensionado** (calcular primero el tamaño y derivar el ancla sobre el rectángulo resultante): no corrige nada —en el colapso, el rectángulo desplegado sigue dando `Left`— y sigue dependiendo del historial.
- **Congelar el ancla en la vista**: lleva geometría a WPF y contradice §5.2-01 y E0-CA6.
- **Un temporizador o una espera que «estabilice» el rectángulo antes de derivar el ancla**: trabajo periódico contra RNF-02 y no elimina la ambigüedad de fondo.

## 9. Notas y pendientes

### Lo que el Director debe registrar en `docs/plan/ESTADO.md`

Este ADR **no** modifica el tablero ni el plan. Las entradas que corresponde registrar son:

1. **ADR-005 en estado *Propuesta***, pendiente de aprobación del usuario antes de implementar.
2. **H-E3-07 no se cierra con este ADR**, sino cuando la implementación esté verificada: las cinco pruebas del §5, los greps del §6.2, el ciclo exacto `104,104 120x36` y los 19 puntos de la sonda de ADR-004 §9.5 en PASS.
3. **Sustitución parcial de ADR-003** (§6.3 y §12 #5) que hay que registrar con el mismo criterio que una ampliación de alcance, para que ADR-003 no se lea como vigente en esos dos puntos.
4. **Ninguna deuda nueva**: D-12, D-13 y D-18 siguen siendo de E4 y no se tocan aquí.

### Decisiones de ingeniería, sin traza a requerimiento

| # | Decisión | Por qué no es requisito | Dónde se ajusta |
| --- | --- | --- | --- |
| 1 | El ancla se fija con el rectángulo **que la barra conserva al soltarla** | Ni RF01 ni HU01 dicen cómo se elige el ancla de una barra flotante | `SetFloatingAnchor` |
| 2 | El ancla se re-deriva solo cuando el recorte mueve la barra | Ningún RF habla del caso «el área de trabajo cambia y la barra flotante deja de caber» | rama de D6 en `ResolveFloating` |
| 3 | `Dispose` no limpia el ancla | Es orden interno de una clase | no aplica |
| 4 | El ancla **no** se persiste | §5.2-02 y AMP-01 no la enumeran, y la posición flotante tampoco se persiste | `DockPlacementCoordinator` |

### Observación para E4, sin abrir pregunta

El recentrado de 24 px en X **durante** el despliegue se mantiene y es el comportamiento de ADR-003 §6.3 (anclar el lado más cercano, recentrar el otro eje). Este ADR lo deja intacto a propósito: el defecto era la **inconsistencia entre estados**, no el recentrado. Si el usuario percibe ese salto como molesto, es una decisión de ingeniería nueva —equivalente a la Opción B— que exige su propio ADR y no forma parte de la corrección de H-E3-07.

### Preguntas que este ADR no abre

Ninguna pregunta de requerimiento. No hay contradicción nueva entre documentos, ningún RF queda reinterpretado y no se reduce alcance: RF01, RF02 y HU01 se aplican tal como estaban decididos, y Q-13 fija la decisión. Las únicas preguntas son de ingeniería y quedan resueltas en este documento o anotadas arriba como observación.

---

*Fin del ADR-005.*
