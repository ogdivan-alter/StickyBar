---
description: Avanza a la siguiente etapa: implementa, verifica y actualiza el seguimiento
agent: director-desarrollo
---

Ejecuta el ciclo completo de una etapa: **C (Ejecución delegada) → D (Verificación) → E (Seguimiento) → F (Reporte)**.

1. Determina la siguiente etapa según `docs/plan/PLAN.md` y el estado real del proyecto.
2. Delega al subagente `implementador` la etapa, con su contrato completo.
3. Delega al subagente `qa` la verificación de sus criterios de aceptación.
4. Si la verificación falla, devuelve la etapa a "En curso", registra el hallazgo y corrige delegando de nuevo. No cierres una etapa con criterios incumplidos.
5. Actualiza `docs/plan/ESTADO.md` con el estado y la bitácora.
6. Reporta el resultado al usuario.

$ARGUMENTS
