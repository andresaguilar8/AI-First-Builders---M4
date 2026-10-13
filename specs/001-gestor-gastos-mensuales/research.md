# Research: Gestor de gastos mensuales

**Feature**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md) · **Fecha**: 2026-10-13

El stack está fijado por [AGENTS.md](../../AGENTS.md) y la constitución (.NET 10 + ASP.NET Core +
EF Core, React 19 + TypeScript 5 con pnpm, PostgreSQL 16). Este documento registra las decisiones
técnicas tomadas dentro de ese stack. Ninguna agrega comportamiento fuera del PRD (principio III).

---

## R-01. Modelo de persistencia de recurrentes: versiones + instancias mensuales

- **Decision**: un gasto recurrente se guarda como cabecera (`RecurringExpense`: mes de inicio y
  mes final opcional) más **versiones** (`RecurringVersion`: valores vigentes desde un mes) más
  **instancias mensuales** (`RecurringInstance`: snapshot completo de un mes con información
  propia, incluido el pago). Los meses sin instancia se proyectan al consultar a partir de la
  versión vigente.
- **Rationale**: cumple RNF-05 (0 registros sin información propia) y RNF-06 (exactamente 1 por mes
  con información propia, por índice único `(recurring_expense_id, period)`). "Este mes y los
  siguientes" crea o actualiza una versión con `effective_from = mes visualizado`, de modo que los
  meses anteriores siguen leyendo versiones anteriores y quedan inmutables por construcción
  (RF-17, principio IV). La instancia guarda un snapshot completo (no solo deltas) para que la
  regla "se conserva solo si tiene información propia" sea una comparación directa contra la
  versión vigente.
- **Alternatives considered**:
  - *Pre-generar filas por mes*: prohibido por RNF-05 y la constitución.
  - *Instancias con overrides nulos por campo*: obliga a distinguir "sin override" de "override a
    vacío" (descripción, categoría, vencimiento) con banderas extra; más propenso a errores.
  - *Event sourcing de cambios*: excesivo para un usuario y 12.000 filas en 10 años.

## R-02. Gastos puntuales en entidad propia (herencia TPH)

- **Decision**: `Expense` abstracta con herencia TPH (una tabla `expenses` con discriminador
  `kind`): `OneOffExpense` (mes, valores y pago en la misma fila) y `RecurringExpense` (cabecera).
- **Rationale**: un puntual no necesita versiones ni instancias; guardarlo plano simplifica
  consultas y la eliminación definitiva (RF-16). TPH evita joins y es el default de EF Core.
- **Alternatives considered**: puntual como recurrente de un solo mes (complica todas las reglas);
  tablas separadas sin herencia (duplica consultas del mes).

## R-03. Conversión de tipo y baja

- **Decision**:
  - *Recurrente → puntual* viendo el mes P (RF-13): `end_period = P − 1`; se crea un
    `OneOffExpense` nuevo en P con los valores efectivos y el pago de P; se borran las instancias
    con `period ≥ P`. Si `end_period < start_period`, el recurrente se elimina por completo (no
    tiene meses visibles).
  - *Baja* en P (RF-14/15): igual, sin crear el puntual.
  - *Puntual → recurrente* en P (RF-12): se elimina el puntual y se crea un recurrente con
    `start_period = P` y una versión con sus valores; si estaba pagado o su vencimiento cae fuera
    de P, se crea la instancia de P.
- **Rationale**: aplica literalmente las aclaraciones de la sesión 2026-10-13 y reutiliza la misma
  operación de "cierre" para baja y conversión.
- **Alternatives considered**: cambiar el discriminador en la misma fila (EF Core no lo soporta
  bien y mezcla historial).

## R-04. Edición con alcance "Este mes y los siguientes"

- **Decision**: el backend recibe los valores del formulario y calcula el **diff** contra los
  valores efectivos del mes P. Solo los campos del diff se aplican a: (1) la versión vigente en P
  (creando una versión con `effective_from = P` si no existía), (2) las versiones con
  `effective_from > P`, y (3) las instancias con `period ≥ P`. Luego se normaliza: cada instancia
  sin pago e idéntica a su versión vigente se elimina (RNF-06), y versiones consecutivas idénticas
  se fusionan.
- **Rationale**: implementa RF-11 aclarado ("solo los campos cuyo valor cambió") y AC-17/AC-64.
  Aplicar el diff también a versiones posteriores garantiza que "los siguientes" muestren el
  valor nuevo aunque existiera un cambio permanente programado más adelante.
- **Vencimiento**: la versión guarda `due_day` (1–31 o nulo). El vencimiento proyectado en el mes
  M es `min(due_day, último día de M)` (RF-07). Si en el mes de la edición la fecha elegida cae en
  un mes posterior (AC-11), esa fecha exacta se guarda en la instancia de P.
- **Alternatives considered**: aplicar todos los campos del formulario (rechazado en la
  clarificación); que el frontend envíe el diff (el backend es la fuente de verdad y debe validar).

## R-05. Fecha actual y zona horaria

- **Decision**: inyectar `TimeProvider` (.NET) y un servicio `IBusinessCalendar` que convierte
  `GetUtcNow()` a `America/Argentina/Buenos_Aires` con `TimeZoneInfo.FindSystemTimeZoneById`
  (IANA soportado en Windows y Linux desde .NET 6 con ICU) y expone `Today` (`DateOnly`) y
  `CurrentPeriod`. Los tests usan `FakeTimeProvider`
  (`Microsoft.Extensions.TimeProvider.Testing`). El frontend nunca calcula "hoy": lo recibe del
  backend en cada respuesta del mes.
- **Rationale**: principio V y RNF-07; AC-48 se testea fijando 01:00 UTC del 06/10/2026.
- **Alternatives considered**: NodaTime (dependencia extra innecesaria); calcular "hoy" en el
  navegador (depende de la zona del dispositivo).

## R-06. Autenticación y sesión

- **Decision**: ASP.NET Core Identity (`AddIdentityCore<AppUser>` + `SignInManager`) con cookie
  HttpOnly, `Secure`, `SameSite=Strict`, `ExpireTimeSpan = 30 días` y renovación en **cada**
  request autenticado (`OnValidatePrincipal` → `ShouldRenew = true`). `OnRedirectToLogin` y
  `OnRedirectToAccessDenied` devuelven 401/403 sin cuerpo de datos (AC-55). No se expone ningún
  endpoint de registro (RF-37) ni de logout (fuera de alcance).
- **Rationale**: el hasher por defecto de Identity v3 es PBKDF2-HMAC-SHA512 con 100.000
  iteraciones (cumple RNF-04 y la constitución). La renovación en cada request hace que la
  expiración sea exactamente "30 días sin actividad" (RNF-08); con `SlidingExpiration` estándar la
  cookie solo se renueva pasada la mitad del período y podría expirar antes.
- **Alternatives considered**: JWT en localStorage (expuesto a XSS, requiere refresh tokens);
  endpoints `MapIdentityApi` (exponen `/register`, prohibido por RF-37).

## R-07. Carga inicial del usuario (seed)

- **Decision**: al iniciar, si no existe ningún usuario, el backend crea uno con
  `SEED_USER_EMAIL` y `SEED_USER_PASSWORD` leídos de variables de entorno (user-secrets en
  desarrollo). Si ya existe un usuario, no hace nada. Si faltan las variables y no hay usuario, el
  arranque falla con un mensaje claro.
- **Rationale**: principio II (sin secretos en el repo) y RF-37.
- **Alternatives considered**: seed en una migración (dejaría la contraseña en el código).

## R-08. Montos

- **Decision**: `numeric(14,2)` en PostgreSQL y `decimal` en .NET; en la API los montos viajan
  como **string** con punto decimal (`"1234.56"`). El frontend convierte la entrada es-AR
  (`1.234,56`) al formato canónico y valida: > 0, ≤ 999999999999.99, hasta 2 decimales (RF-03).
  El backend revalida.
- **Rationale**: 14 dígitos alcanzan para el tope de RF-03; string evita errores de punto
  flotante en JavaScript.
- **Alternatives considered**: enteros en centavos (cambia el contrato y el CSV); `number` en JSON.

## R-09. Unicidad de categorías

- **Decision**: columna `normalized_name` = nombre recortado, en minúsculas, sin diacríticos
  (`string.Normalize(FormD)` filtrando `NonSpacingMark`) y con espacios internos colapsados, con
  índice único. "sin categoria" está reservado. FK `category_id` con `ON DELETE SET NULL` en
  puntuales, versiones e instancias (RF-21).
- **Rationale**: RF-19 y AC-26/AC-27; el FK hace que renombrar y eliminar se reflejen en todos los
  meses (RF-20/RF-21) sin código extra.
- **Alternatives considered**: collation ICU no determinística en PostgreSQL (no soporta índices
  únicos con todas las operaciones y complica las migraciones).

## R-10. Límites de texto (decisión técnica a confirmar en el PRD)

- **Decision**: nombre del gasto ≤ 100 caracteres, descripción ≤ 500, nombre de categoría ≤ 50;
  se recortan los espacios al inicio y al final; un nombre vacío tras recortar se trata como
  ausente (AC-03).
- **Rationale**: hacen falta para definir columnas y validaciones; los valores son holgados para el
  uso descrito. Como el PRD no los fija, se registran aquí y conviene incorporarlos al PRD (punto
  CHK009 del checklist de dominio).

## R-11. Frontend

- **Decision**: Vite + React 19 + TypeScript 5; React Router para las 3 vistas (login, mes,
  categorías); TanStack Query para el estado del servidor (actualización sin recargar, AC-35);
  formateo con `Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })`; CSS propio
  mobile-first desde 360 px (RNF-02). PWA con `vite-plugin-pwa`: manifest + service worker que
  **no** cachea datos de la API (offline fuera de alcance, RNF-03).
- **Rationale**: stack mínimo que cubre los requisitos; TanStack Query resuelve invalidación del
  mes y del resumen tras cada mutación.
- **Alternatives considered**: Redux (innecesario), Next.js (SSR innecesario y cambia el stack),
  librería de componentes pesada (no aporta a 3 vistas).

## R-12. Despliegue y CSRF

- **Decision**: en producción el backend sirve el build del frontend desde `wwwroot` (mismo
  origen). En desarrollo, el dev server de Vite hace proxy de `/api` al backend. La cookie
  `SameSite=Strict` en mismo origen mitiga CSRF; además las mutaciones solo aceptan
  `application/json`.
- **Rationale**: un solo origen simplifica cookies, CORS y la instalación como PWA.
- **Alternatives considered**: frontend y API en dominios distintos (requiere CORS con
  credenciales y `SameSite=None`).

## R-13. Estrategia de tests

- **Decision**:
  - *Dominio (backend)*: xUnit con tests unitarios puros sobre `GestorGastos.Domain` (proyección,
    edición con alcance, conversión, baja, pagos, estados), escritos **antes** de la
    implementación (principio I). Cada test que cubre un AC lleva `[Trait("AC", "AC-17")]`.
  - *API + persistencia*: xUnit + `WebApplicationFactory` + Testcontainers (PostgreSQL 16 real)
    para conteo de registros (AC-60, AC-61, AC-64, AC-65, AC-68), autenticación (AC-52 a AC-57),
    categorías y CSV (AC-58, AC-59).
  - *Frontend*: Vitest + Testing Library + MSW para formularios, parseo de montos es-AR, flujo de
    pago vencido y alerta.
  - *Rendimiento*: un test de integración siembra 10 años × 100 gastos y mide p95 de las
    operaciones de RNF-01 (< 2 s, medido en el servidor).
- **Rationale**: cumple el principio I y deja trazabilidad AC → test (`dotnet test --filter AC=AC-17`).
- **Alternatives considered**: base en memoria para tests (no valida índices únicos ni
  `ON DELETE SET NULL`).

## R-14. Rendimiento

- **Decision**: la consulta del mes trae puntuales de P, recurrentes activos en P con su versión
  vigente (`effective_from ≤ P` más reciente) e instancias de P en 3 consultas indexadas; el
  resumen y los totales se calculan en memoria sobre ese resultado y viajan en la misma respuesta.
  La alerta solo necesita: puntuales e instancias con `due_date = hoy` sin pago, y proyecciones del
  mes actual (un vencimiento proyectado siempre cae en su propio mes). La exportación arma en
  memoria ≤ 12.000 filas.
- **Rationale**: con ≤ 100 gastos por mes los volúmenes son triviales para PostgreSQL; RNF-01
  (< 2 s p95) queda con margen amplio.
