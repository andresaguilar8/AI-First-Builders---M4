<!--
Sync Impact Report
- Version change: 1.0.0 → 1.0.1 (PATCH: aclaración)
- Principios modificados:
  - IV. Historial inmutable y proyección de recurrentes: se agregan nombre y descripción a los
    campos que no se alteran en meses anteriores, alineado con RF-17 del PRD (sesión de
    clarificación 2026-10-13).
- Secciones agregadas: ninguna
- Secciones eliminadas: ninguna
- Templates (.specify/templates/*): no se modifican; leen la constitución en runtime
- AGENTS.md: consistente (ya prohíbe modificar retroactivamente meses anteriores)
- TODOs diferidos: ninguno
-->

# Gestor de Gastos Constitution

## Core Principles

### I. Lógica de gastos test-first (NO NEGOCIABLE)

Toda lógica de dominio de gastos, tanto puntuales como recurrentes, DEBE desarrollarse test-first:

- El test DEBE escribirse antes que la implementación y ejecutarse para confirmar que falla;
  recién entonces se escribe el código que lo hace pasar (ciclo Red-Green-Refactor).
- Alcance obligatorio: alta, edición, baja y eliminación de gastos; cambio de tipo
  puntual ↔ recurrente; marcado y desmarcado de pago; proyección de recurrentes por período;
  ediciones con alcance "Solo este mes" / "Este mes y los siguientes"; estados de vencimiento.
- Cada criterio de aceptación (AC-xx) del PRD que toque esta lógica DEBE tener al menos un test
  automatizado que lo referencie por su código.
- Un cambio en esta lógica sin un test que lo cubra NO DEBE mergearse.

**Rationale**: la proyección de recurrentes y la preservación del historial son las reglas más
fáciles de romper sin darse cuenta; los tests escritos primero las fijan como contrato ejecutable.

### II. Sin secretos en el código

- Credenciales, cadenas de conexión, claves de firma de tokens y cualquier otro secreto DEBEN
  leerse de variables de entorno (o de un mecanismo equivalente fuera del repositorio, como
  user-secrets en desarrollo).
- NO DEBE aparecer ningún secreto en código fuente, `appsettings*.json`, `docker-compose.yml`,
  archivos de frontend, tests ni historial de git.
- Los archivos `.env` DEBEN estar en `.gitignore`; el repositorio PUEDE incluir un `.env.example`
  con nombres de variables y valores ficticios.
- Si un secreto llega a commitearse, DEBE rotarse; borrarlo del archivo no alcanza.

**Rationale**: un secreto en el repositorio queda expuesto para siempre en el historial.

### III. Alcance acotado al PRD vigente

- Solo se implementan funcionalidades respaldadas por un RF/RNF de [PRD.md](../../PRD.md).
- Toda spec, plan y tarea DEBE referenciar los códigos RF/RNF/AC que cubre.
- Una funcionalidad no contemplada (por ejemplo multiusuario, modo offline, cierre de sesión,
  pre-generación de meses futuros) NO DEBE implementarse hasta que el PRD se modifique para
  incluirla.
- Ante una ambigüedad del PRD se aclara el PRD; no se resuelve agregando comportamiento por
  cuenta propia.

**Rationale**: el PRD es la única fuente de verdad del alcance; evita trabajo no pedido y
deriva funcional.

### IV. Historial inmutable y proyección de recurrentes

- Editar, dar de baja o cambiar de tipo un gasto recurrente NO DEBE alterar nombre, descripción,
  monto, vencimiento, categoría ni estado de pago de meses anteriores al visualizado (RF-13,
  RF-15, RF-17).
- Excepción explícita: renombrar o eliminar una categoría se refleja en todos los meses,
  incluidos los anteriores (RF-20, RF-21).
- Los recurrentes NO DEBEN pre-generar registros: se proyectan al consultar el período; solo se
  persiste exactamente 1 instancia por mes cuando ese mes tiene información propia
  (RNF-05, RNF-06).

**Rationale**: es el invariante central del dominio; romperlo corrompe datos históricos del
usuario de forma silenciosa.

### V. Fecha actual en America/Argentina/Buenos_Aires

- Toda referencia a "hoy" (alerta y estados de vencimiento, fecha de pago, mes inicial) DEBE
  calcularse en la zona `America/Argentina/Buenos_Aires`, independientemente de la zona del
  servidor o del navegador (RNF-07).
- El reloj DEBE ser inyectable para que los tests fijen fecha y hora (por ejemplo, AC-48).

**Rationale**: un desfasaje de zona horaria cambia el día y altera vencimientos sin error visible.

## Restricciones técnicas y de seguridad

- Stack: backend .NET 10 + ASP.NET Core + Entity Framework Core; frontend React 19 +
  TypeScript 5 con pnpm; base de datos PostgreSQL 16. Cambiar el stack requiere enmendar esta
  constitución.
- Las contraseñas DEBEN persistirse con hash y salt con un costo ≥ PBKDF2 de 100.000 iteraciones
  (RNF-04); nunca en texto plano.
- La sesión DEBE expirar tras 30 días sin actividad (RNF-08).
- Las operaciones listadas en RNF-01 DEBEN responder en < 2 s p95 con hasta 100 gastos por mes y
  10 años de historial.

## Flujo de desarrollo y quality gates

- Cada feature sigue el flujo Spec Kit: `/speckit-specify` → `/speckit-plan` →
  `/speckit-tasks` → `/speckit-implement`. El "Constitution Check" del plan DEBE verificar los
  principios I–V.
- Antes de mergear DEBEN pasar `dotnet test` (backend) y `pnpm test` (frontend).
- La revisión de código DEBE verificar: tests escritos primero para lógica de gastos, ausencia de
  secretos y trazabilidad de cada cambio a un RF/RNF/AC del PRD.
- Los commits siguen Conventional Commits.

## Governance

- Esta constitución prevalece sobre cualquier otra práctica del proyecto. `AGENTS.md` es la guía
  operativa de runtime y DEBE mantenerse consistente con ella.
- Las enmiendas se hacen con `/speckit-constitution`, documentando el cambio en el Sync Impact
  Report y en el mensaje de commit.
- Versionado semántico: MAJOR para eliminar o redefinir principios de forma incompatible; MINOR
  para agregar principios o secciones, o ampliar materialmente una guía; PATCH para
  aclaraciones y redacción.
- Toda complejidad o desvío de un principio DEBE justificarse explícitamente en el plan de la
  feature (sección Complexity Tracking).

**Version**: 1.0.1 | **Ratified**: 2026-10-12 | **Last Amended**: 2026-10-13
