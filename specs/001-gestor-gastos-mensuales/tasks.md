---

description: "Task list for Gestor de gastos mensuales"
---

# Tasks: Gestor de gastos mensuales

**Input**: Design documents from `/specs/001-gestor-gastos-mensuales/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/api.yaml](contracts/api.yaml),
[contracts/csv-export.md](contracts/csv-export.md), [quickstart.md](quickstart.md)

**Tests**: OBLIGATORIOS. La constitución (principio I, NO NEGOCIABLE) exige test-first para toda
la lógica de gastos: cada test se escribe primero, se ejecuta y **debe fallar** antes de
implementar. Todo test que cubre un criterio de aceptación lleva `[Trait("AC", "AC-xx")]`
(backend) o `AC-xx` en el nombre del `it(...)` (frontend).

**Organization**: tareas agrupadas por historia de usuario del spec (US1 a US7).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia de usuario (US1 a US7)
- Rutas relativas a la raíz del repo. Backend: `backend/src/GestorGastos.Domain/`,
  `backend/src/GestorGastos.Api/`, `backend/tests/...`. Frontend: `frontend/src/`, `frontend/tests/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: crear la estructura de proyectos y la configuración base.

- [X] T001 Crear `docker-compose.yml` en la raíz con el servicio `postgres:16` (puerto 5432, volumen nombrado, variables `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` leídas de `.env`) (infra, sin RF)
- [X] T002 [P] Crear `.env.example` en la raíz con `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `ConnectionStrings__Default`, `SEED_USER_EMAIL`, `SEED_USER_PASSWORD` con valores ficticios, y crear `.gitignore` en la raíz que incluya `.env`, `bin/`, `obj/`, `node_modules/`, `dist/` (principio II) (infra, sin RF)
- [X] T003 Crear la solución `backend/GestorGastos.slnx` con los proyectos `backend/src/GestorGastos.Domain` (classlib net10.0, sin dependencias de infraestructura), `backend/src/GestorGastos.Api` (web net10.0, referencia a Domain), `backend/tests/GestorGastos.Domain.Tests` y `backend/tests/GestorGastos.Api.Tests` (xUnit v3) (infra, sin RF)
- [X] T004 Agregar paquetes NuGet: en Api `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`; en Domain.Tests `Microsoft.Extensions.TimeProvider.Testing`; en Api.Tests `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`, `Microsoft.Extensions.TimeProvider.Testing` (infra, sin RF)
- [X] T005 [P] Crear `backend/Directory.Build.props` con `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true` y `backend/.editorconfig` con estilo C# estándar (infra, sin RF)
- [X] T006 [P] Inicializar el frontend con Vite + React 19 + TypeScript 5 en `frontend/` usando pnpm; agregar `react-router`, `@tanstack/react-query`, `vite-plugin-pwa`, y como devDependencies `vitest`, `@testing-library/react`, `@testing-library/user-event`, `@testing-library/jest-dom`, `jsdom`, `msw` (infra, sin RF)
- [X] T007 [P] Configurar `frontend/vite.config.ts` con proxy `/api` → backend en desarrollo, salida de build a `backend/src/GestorGastos.Api/wwwroot`, y entorno de test `jsdom` con setup `frontend/tests/setup.ts` (jest-dom + servidor MSW); script `test` en `frontend/package.json` = `vitest run` (infra, sin RF)
- [X] T008 [P] Configurar ESLint y Prettier en `frontend/` (`eslint.config.js`, `.prettierrc`) con `tsconfig.json` en modo `strict` (infra, sin RF)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: dominio base, persistencia, reloj, autenticación por cookie y harness de tests que
todas las historias necesitan.

**⚠️ CRITICAL**: ninguna historia puede empezar hasta completar esta fase.

### Tests foundational (escribir primero, deben fallar)

- [X] T009 [P] Tests de `Period` en `backend/tests/GestorGastos.Domain.Tests/Periods/PeriodTests.cs`: parseo/formato `yyyy-MM`, `Next()`/`Previous()` cruzando años (`2027-01` → `2026-12`, AC-51), `LastDay`, comparación
- [X] T010 [P] Tests de `BusinessCalendar` en `backend/tests/GestorGastos.Domain.Tests/Periods/BusinessCalendarTests.cs` con `FakeTimeProvider`: a las 01:00 UTC del 06/10/2026 `Today` = 05/10/2026 y `CurrentPeriod` = `2026-10` (AC-48, RNF-07)
- [X] T011 [P] Tests de `Amount` en `backend/tests/GestorGastos.Domain.Tests/Money/AmountTests.cs`: rechaza 0, -100, 10.999 (más de 2 decimales) y 1000000000000; acepta 1234.56 y 999999999999.99 (AC-05, AC-06, AC-62)
- [X] T012 [P] Tests de `ExpenseValues` en `backend/tests/GestorGastos.Domain.Tests/Expenses/ExpenseValuesTests.cs`: nombre obligatorio "hasta 100 caracteres" recortado (vacío tras recortar = no indicado, AC-03), descripción "hasta 500", vencimiento `≥` primer día del mes del gasto (AC-08)

### Implementación foundational

- [X] T013 [P] Implementar el value object `Period(Year, Month)` en `backend/src/GestorGastos.Domain/Periods/Period.cs` (parseo/formato `yyyy-MM`, `FirstDay` como `DateOnly`, `LastDay`, `Next`, `Previous`, `Contains(DateOnly)`, `IComparable`) (RF-02, RF-34, RF-35)
- [X] T014 [P] Implementar `IBusinessCalendar` y `BusinessCalendar` en `backend/src/GestorGastos.Domain/Periods/BusinessCalendar.cs`: recibe `TimeProvider`, convierte a `America/Argentina/Buenos_Aires` con `TimeZoneInfo.FindSystemTimeZoneById` y expone `Today` (`DateOnly`) y `CurrentPeriod` (RNF-07)
- [X] T015 [P] Implementar `Amount` en `backend/src/GestorGastos.Domain/Money/Amount.cs`: `decimal` "mayor a 0 y ≤ 999999999999.99, con hasta 2 decimales"; `Parse` desde string invariante con punto (`"1234.56"`) y `ToString("0.00", InvariantCulture)` (RF-03)
- [X] T016 Implementar `ExpenseValues` (record: `Name`, `Description?`, `Amount`, `CategoryId?`, `DueDate?`) y su validación contra un `Period` en `backend/src/GestorGastos.Domain/Expenses/ExpenseValues.cs`; errores de dominio en `backend/src/GestorGastos.Domain/Common/DomainValidationException.cs` (diccionario campo → mensajes) (RF-01, RF-05, RF-22)
- [X] T017 Crear las entidades del modelo en `backend/src/GestorGastos.Domain/Expenses/`: `Expense.cs` (abstracta: `Id`, `CreatedAt`), `OneOffExpense.cs` (`Period`, valores, `PaidOn?`), `RecurringExpense.cs` (`StartPeriod`, `EndPeriod?`, colecciones `Versions` e `Instances`), `RecurringVersion.cs` (`EffectiveFrom`, valores con `DueDay` 1–31 o nulo), `RecurringInstance.cs` (`Period`, valores con `DueDate?`, `PaidOn?`); y `backend/src/GestorGastos.Domain/Categories/Category.cs` (`Id`, `Name`, `NormalizedName`) según [data-model.md](data-model.md) (RF-04, RF-06, RF-08, RF-18, RNF-05, RNF-06)
- [X] T018 Crear `AppDbContext` (hereda de `IdentityDbContext<AppUser>`) en `backend/src/GestorGastos.Api/Data/AppDbContext.cs` y `AppUser` en `backend/src/GestorGastos.Api/Auth/AppUser.cs` (RF-36)
- [X] T019 Crear las configuraciones EF en `backend/src/GestorGastos.Api/Data/Configurations/`: TPH en tabla `expenses` con discriminador `kind` (`one_off` | `recurring`); `period`/`start_period`/`end_period`/`effective_from` como `date` con `CHECK (extract(day from …) = 1)`; montos `numeric(14,2)`; `name varchar(100)`, `description varchar(500) null`; `category.name varchar(50)`, `normalized_name varchar(50) UNIQUE`; `UNIQUE (recurring_expense_id, effective_from)`; `UNIQUE (recurring_expense_id, period)` en `recurring_instance`; todas las FK `category_id` con `ON DELETE SET NULL`; FK de versiones e instancias con cascade; índices de [data-model.md](data-model.md); nombres `snake_case` (RF-01, RF-03, RF-18, RF-19, RF-21, RNF-06)
- [X] T020 Generar la migración inicial en `backend/src/GestorGastos.Api/Data/Migrations/` y aplicar migraciones automáticamente al iniciar en `backend/src/GestorGastos.Api/Program.cs` (infra, sin RF)
- [X] T021 Configurar Identity y la cookie en `backend/src/GestorGastos.Api/Auth/AuthSetup.cs` (R-06): `AddIdentityCore<AppUser>` + `SignInManager`, cookie `gg.session` HttpOnly, `Secure`, `SameSite=Strict`, `ExpireTimeSpan = 30 días`, `OnValidatePrincipal` con `ShouldRenew = true` en cada request, `OnRedirectToLogin`/`OnRedirectToAccessDenied` → 401/403 sin cuerpo; el grupo de endpoints `/api` exige usuario autenticado (`RequireAuthorization()`), salvo `POST /api/auth/login` (`AllowAnonymous`); **no** usar `FallbackPolicy` global, para que los archivos estáticos y el `index.html` del frontend (incluida la pantalla de login) se sirvan sin sesión (RF-36, RNF-08)
- [X] T022 Implementar el seed del usuario en `backend/src/GestorGastos.Api/Auth/UserSeeder.cs` (R-07): si no hay usuarios, crearlo con `SEED_USER_EMAIL` y `SEED_USER_PASSWORD` de configuración/variables de entorno; si ya existe, no hacer nada; si faltan las variables y no hay usuario, fallar el arranque con mensaje claro (RF-37, RNF-04)
- [X] T023 Configurar `backend/src/GestorGastos.Api/Program.cs`: registrar `TimeProvider.System`, `IBusinessCalendar`, `AppDbContext` con `ConnectionStrings:Default`, ProblemDetails (RFC 9457) y un filtro que convierte `DomainValidationException` en 400 con `errors` por campo; servir `wwwroot` con fallback a `index.html` fuera de `/api`, ambos sin autenticación (RNF-07)
- [X] T024 Crear el harness de integración en `backend/tests/GestorGastos.Api.Tests/Infrastructure/ApiFactory.cs`: `WebApplicationFactory<Program>` + contenedor PostgreSQL 16 de Testcontainers compartido, `FakeTimeProvider` reemplazable, seed de usuario de prueba, clientes creados con `ClientOptions.BaseAddress = https://localhost` (la cookie es `Secure` y no viaja por `http`), helper `CreateAuthenticatedClientAsync()` que obtiene la cookie **sin** pasar por `POST /api/auth/login` (endpoint de prueba definido en el proyecto de tests y agregado al host desde `ApiFactory` con un `IStartupFilter`, nunca en el código de `GestorGastos.Api`, que llama a `SignInManager.SignInAsync` con el usuario sembrado), para que las historias US2 a US7 no dependan de US1, limpieza de datos entre tests y helper `CountInstancesAsync(expenseId, period)` para AC de conteo de registros; todas las clases de `GestorGastos.Api.Tests` comparten una única `[Collection("Api")]` con `ICollectionFixture<ApiFactory>` y `backend/tests/GestorGastos.Api.Tests/xunit.runner.json` fija `"parallelizeTestCollections": false`, para que ningún test corra en paralelo contra la misma base (RNF-05, RNF-06)
- [X] T025 [P] Crear el cliente HTTP base del frontend en `frontend/src/api/client.ts` (fetch con `credentials: 'same-origin'`, JSON, errores 400 → objeto de errores por campo, 401 → evento de sesión vencida) y los tipos de [contracts/api.yaml](contracts/api.yaml) en `frontend/src/api/types.ts` (RF-36)
- [X] T026 [P] Tests de `frontend/src/lib/amount.ts` en `frontend/tests/lib/amount.test.ts` (escribir primero): `parseArsAmount('1.234,56')` → `'1234.56'`; rechaza `0`, `-100`, `10,999` y `1.000.000.000.000`; acepta `999.999.999.999,99`; `formatArs('1234.56')` → `$ 1.234,56` (AC-05, AC-06, AC-62)
- [X] T027 [P] Implementar `parseArsAmount` y `formatArs` (con `Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' })`) en `frontend/src/lib/amount.ts` y helpers de fechas `yyyy-MM-dd`/`dd/MM/yyyy` y períodos en `frontend/src/lib/dates.ts` (RF-03)
- [X] T028 Configurar `frontend/src/main.tsx` y `frontend/src/App.tsx`: `QueryClientProvider`, router con rutas `/login`, `/` (mes) y `/categorias`, y estilos base mobile-first en `frontend/src/styles.css` (ancho mínimo 360 px, sin scroll horizontal, RNF-02)

**Checkpoint**: la base está lista; las historias pueden empezar.

---

## Phase 3: User Story 1 - Acceder de forma segura a la información (Priority: P1) 🎯 MVP

**Goal**: login con email y contraseña, nada accesible sin sesión, sin registro, sesión de 30 días.

**Independent Test**: con el usuario del seed, login correcto e incorrecto, acceso sin sesión por UI
y por API, expiración por inactividad y ausencia de registro.

### Tests for User Story 1 ⚠️ (escribir primero, deben fallar)

- [X] T029 [P] [US1] Tests de integración en `backend/tests/GestorGastos.Api.Tests/Auth/AuthEndpointsTests.cs`: login correcto → 204 + cookie (AC-52); contraseña incorrecta → 401 sin datos (AC-53); `GET /api/periods/2026-10` y `POST /api/categories` sin cookie → 401 con cuerpo vacío (AC-55); avanzar `FakeTimeProvider` 30 días sin actividad → 401 (AC-56) y 29 días con una request intermedia → sigue válida; `POST /api/auth/register` → 404 y sigue habiendo 1 usuario (AC-57)
- [X] T030 [P] [US1] Test del seed en `backend/tests/GestorGastos.Api.Tests/Auth/UserSeederTests.cs`: crea el usuario una sola vez; el `PasswordHash` no contiene la contraseña y usa el formato v3 de Identity (RNF-04)
- [X] T031 [P] [US1] Tests de frontend en `frontend/tests/features/auth/LoginPage.test.tsx`: credenciales inválidas muestran error y no muestran datos (AC-53); sin sesión, `/` redirige a `/login` (AC-54); no existe enlace ni ruta de registro (AC-57)

### Implementation for User Story 1

- [X] T032 [US1] Implementar `POST /api/auth/login` y `GET /api/auth/me` en `backend/src/GestorGastos.Api/Endpoints/AuthEndpoints.cs` según [contracts/api.yaml](contracts/api.yaml); sin endpoints de registro ni de logout (RF-36, RF-37)
- [X] T033 [P] [US1] Implementar la página de login en `frontend/src/features/auth/LoginPage.tsx` (email, contraseña, error genérico ante 401) y el guard `frontend/src/features/auth/RequireAuth.tsx` (consulta `/api/auth/me`; 401 → `/login`); al recibir 401 en cualquier request, volver a `/login` (RF-36)
- [X] T034 [US1] Tras el login, navegar a la vista del mes actual (`/`) en `frontend/src/features/auth/LoginPage.tsx` (AC-52)

**Checkpoint**: acceso protegido funcionando y testeado de forma independiente.

---

## Phase 4: User Story 2 - Registrar gastos del mes y navegar entre períodos (Priority: P1)

**Goal**: ver el mes actual, crear gastos puntuales y recurrentes con validaciones, navegar meses.

**Independent Test**: crear gastos puntuales en distintos meses y navegar entre ellos.

### Tests for User Story 2 ⚠️ (escribir primero, deben fallar)

- [X] T035 [P] [US2] Tests de dominio de alta en `backend/tests/GestorGastos.Domain.Tests/Expenses/CreateExpenseTests.cs`: puntual con todos los campos (AC-02); recurrente creado en `2026-10` con versión `effective_from = 2026-10` y `due_day` del vencimiento (AC-01); sin nombre o sin monto → error (AC-03); vencimiento 30/09/2026 en `2026-10` → error (AC-08); vencimiento 05/11/2026 en `2026-10` aceptado (AC-07)
- [X] T036 [P] [US2] Tests de proyección del mes en `backend/tests/GestorGastos.Domain.Tests/Expenses/MonthProjectorTests.cs`: un puntual de un mes no aparece en el siguiente (AC-13); un puntual creado viendo diciembre aparece en diciembre y no en octubre (AC-04); mes sin gastos → lista vacía y totales en 0; y tests de estados en `backend/tests/GestorGastos.Domain.Tests/DueStatus/DueStatusCalculatorTests.cs` con hoy = 05/10/2026: 09/10 → AlDia (AC-43); sin vencimiento → AlDia (AC-44); 08/10 y 06/10 → ProximoAVencer (AC-45); 05/10 → VenceHoy (AC-46); 04/10 → Vencido (AC-47); pagado → Pagado
- [X] T037 [P] [US2] Tests de integración en `backend/tests/GestorGastos.Api.Tests/Expenses/CreateAndListTests.cs`: `POST /api/periods/2026-10/expenses` con monto `"1234.56"` → 201 y aparece en `GET /api/periods/2026-10` (AC-06); monto `"0"`, `"-100"`, `"10.999"`, `"1000000000000"` → 400 (AC-05, AC-62); `"999999999999.99"` → 201 (AC-62); nombre de 101 caracteres → 400; respuesta incluye `today` en Buenos Aires
- [X] T038 [P] [US2] Tests de frontend en `frontend/tests/features/month/MonthPage.test.tsx` y `frontend/tests/features/expenses/ExpenseForm.test.tsx`: carga inicial muestra el mes de `today` (AC-49); anterior desde septiembre → agosto (AC-50) y desde enero 2027 → diciembre 2026 (AC-51); el formulario rechaza alta sin nombre/monto (AC-03) y monto `10,999` (AC-05)

### Implementation for User Story 2

- [X] T039 [US2] Implementar en el dominio la creación de gastos en `backend/src/GestorGastos.Domain/Expenses/ExpenseFactory.cs`: puntual en el período visualizado; recurrente con cabecera (`StartPeriod = P`) + versión (`EffectiveFrom = P`, `DueDay` = día del vencimiento) e instancia de P solo si el vencimiento cae fuera de P (R-03) (RF-01, RF-02, RF-04, RF-05)
- [X] T040 [US2] Implementar `MonthProjector` en `backend/src/GestorGastos.Domain/Expenses/MonthProjector.cs`: valores efectivos de cada gasto en un período según la tabla "Valores efectivos" de [data-model.md](data-model.md) (puntual del mes; instancia; o proyección de la versión vigente = mayor `EffectiveFrom ≤ P`, con vencimiento `min(DueDay, último día del mes)`), y `DueStatusCalculator` en `backend/src/GestorGastos.Domain/DueStatus/DueStatusCalculator.cs` con la tabla de [data-model.md](data-model.md) (`d = dueDate − today` en días calendario; pagado → `Pagado`) (RF-06, RF-07, RF-08, RF-33)
- [X] T041 [US2] Implementar la consulta del mes en `backend/src/GestorGastos.Api/Services/MonthQueryService.cs`: 3 consultas indexadas (puntuales de P; recurrentes activos en P con versiones `≤ P`; instancias de P) → `MonthProjector` → DTO `MonthView` con `status` de cada gasto calculado por `DueStatusCalculator` (obligatorio en el contrato) y sin resumen por categoría todavía (`byCategory` vacío) (R-14) (RF-33, RF-34, RNF-01)
- [X] T042 [US2] Implementar `GET /api/periods/{period}` y `POST /api/periods/{period}/expenses` en `backend/src/GestorGastos.Api/Endpoints/PeriodEndpoints.cs` y `backend/src/GestorGastos.Api/Endpoints/ExpenseEndpoints.cs` con DTOs en `backend/src/GestorGastos.Api/Contracts/ExpenseDtos.cs` (montos como string, período `yyyy-MM`) (RF-01, RF-02, RF-35)
- [X] T043 [P] [US2] Implementar la vista del mes en `frontend/src/features/month/MonthPage.tsx`, `MonthNavigator.tsx` (anterior/siguiente, año incluido) y `ExpenseList.tsx` (nombre, monto, categoría o "Sin categoría", vencimiento, tipo puntual/recurrente); período inicial = mes de `today` del backend (RF-34, RF-35)
- [X] T044 [P] [US2] Implementar el formulario de alta en `frontend/src/features/expenses/ExpenseForm.tsx` (nombre, monto es-AR, descripción, categoría opcional, vencimiento, tipo) con validación local y errores 400 por campo; al guardar invalida la query del mes (RF-01, RF-03, RF-04, RF-05)

**Checkpoint**: alta y consulta mensual funcionando; con US1 forman un MVP usable.

---

## Phase 5: User Story 3 - Gastos recurrentes que se proyectan y se editan sin reescribir el pasado (Priority: P1)

**Goal**: proyección automática y edición con "Solo este mes" / "Este mes y los siguientes".

**Independent Test**: crear un recurrente, consultar meses futuros y editar con ambos alcances
verificando meses anteriores, posteriores y la cantidad de registros guardados.

### Tests for User Story 3 ⚠️ (escribir primero, deben fallar)

- [X] T045 [P] [US3] Tests de proyección en `backend/tests/GestorGastos.Domain.Tests/Expenses/RecurringProjectionTests.cs`: aparece el mes siguiente (AC-09); vencimiento 30/01/2027 → 28/02/2027 y 30/03/2027 (AC-10); creado en `2026-10` con vencimiento 05/11/2026 → noviembre 05/11 y diciembre 05/12 (AC-11); sin vencimiento → proyecta sin vencimiento
- [X] T046 [P] [US3] Tests de edición en `backend/tests/GestorGastos.Domain.Tests/Expenses/RecurringEditorTests.cs`: "Solo este mes" $35.000 en septiembre, octubre sigue $30.000 (AC-15); "Este mes y los siguientes" desde septiembre: agosto $25.000, septiembre y siguientes $30.000 (AC-16); cambio de vencimiento al 15 (AC-12); noviembre con $32.000 propio y pagado pasa a $35.000 y sigue pagado (AC-17); solo se propagan los campos cambiados: una categoría propia de noviembre se conserva al cambiar solo el monto (RF-11); nombre/descripción no retroactivos (AC-63); noviembre igualado a la versión y sin pago → instancia eliminada (AC-64); el diff también se aplica a versiones posteriores y se fusionan versiones idénticas (R-04); editar conserva `PaidOn` (RF-09); vencimiento en mes anterior al visualizado → error (RF-05); quitar el vencimiento con "Este mes y los siguientes" → la versión queda con `DueDay` nulo y los meses siguientes se proyectan sin vencimiento (RF-09). Además, en un archivo aparte, `backend/tests/GestorGastos.Domain.Tests/Expenses/OneOffExpenseTests.cs`: editar nombre, descripción, monto, categoría y vencimiento de un puntual refleja los nuevos valores (AC-14), conserva `PaidOn` (RF-09), permite quitar el vencimiento (RF-09) y rechaza un vencimiento anterior al mes (RF-05)
- [X] T047 [P] [US3] Tests de integración en `backend/tests/GestorGastos.Api.Tests/Expenses/RecurringPersistenceTests.cs`: creado en `2026-10` sin cambios → 0 instancias en `2026-11` y `2026-12` (AC-60); AC-64 cuenta 0 instancias en noviembre; `PUT` sin `scope` para un recurrente → 400; `PUT` de un puntual sin `scope` → 200 con los valores nuevos (AC-14)
- [X] T048 [P] [US3] Tests de frontend en `frontend/tests/features/expenses/EditScopeDialog.test.tsx`: editar un recurrente exige elegir "Solo este mes" o "Este mes y los siguientes"; editar un puntual no lo pide (RF-10)

### Implementation for User Story 3

- [X] T049 [US3] Implementar la normalización de instancias en `backend/src/GestorGastos.Domain/Expenses/InstanceNormalizer.cs`: una instancia existe solo si `PaidOn != null` o algún campo difiere de la proyección de la versión vigente; si no, se elimina (RNF-05, RNF-06)
- [X] T050 [US3] Implementar `RecurringEditor` en `backend/src/GestorGastos.Domain/Expenses/RecurringEditor.cs`: `EditThisMonth` (upsert de la instancia de P con los valores nuevos, conserva `PaidOn`, normaliza) y `EditThisAndFollowing` (diff contra los valores efectivos de P → versión de P creada o actualizada, versiones posteriores e instancias `≥ P`; vencimiento: `DueDay` = día de la nueva fecha y fecha exacta en la instancia de P si cae fuera de P; normaliza y fusiona versiones consecutivas idénticas) según R-04 (RF-10, RF-11, RF-17, RNF-06)
- [X] T051 [US3] Implementar la edición de puntuales en `backend/src/GestorGastos.Domain/Expenses/OneOffExpense.cs` (`Update(values)` conserva `PaidOn`, valida vencimiento `≥` período) (RF-05, RF-09)
- [X] T052 [US3] Implementar `PUT /api/periods/{period}/expenses/{expenseId}` en `backend/src/GestorGastos.Api/Endpoints/ExpenseEndpoints.cs` con `scope` obligatorio para recurrentes (`ThisMonth` | `ThisAndFollowing`) y 404 si el gasto no aparece en ese mes; servicio de comandos en `backend/src/GestorGastos.Api/Services/ExpenseCommandService.cs` (RF-09, RF-10)
- [X] T053 [P] [US3] Implementar la edición en el frontend: `frontend/src/features/expenses/ExpenseForm.tsx` en modo edición y `frontend/src/features/expenses/EditScopeDialog.tsx` (pregunta el alcance solo para recurrentes); marcar visualmente los recurrentes en `ExpenseList.tsx` (RF-09, RF-10)

**Checkpoint**: el problema central del PRD resuelto (US1 + US2 + US3 = MVP completo).

---

## Phase 6: User Story 4 - Pagar a tiempo: estados, marcado de pago y alerta del día (Priority: P2)

**Goal**: estados de vencimiento, marcar/desmarcar pagos y alerta de lo que vence hoy.

**Independent Test**: con la fecha fijada, crear gastos con distintos vencimientos y verificar
estados, alerta y marcado/desmarcado.

### Tests for User Story 4 ⚠️ (escribir primero, deben fallar)

- [X] T054 [P] [US4] Tests de integración de estados en `backend/tests/GestorGastos.Api.Tests/Payments/MonthStatusTests.cs` con hoy = 05/10/2026: tras marcar pagado un gasto, `GET /api/periods/2026-10` lo devuelve con `status = Pagado` y `paidOn`; tras desmarcarlo vuelve a su estado de vencimiento (AC-43 a AC-47 ya cubiertos en dominio por T036)
- [X] T055 [P] [US4] Tests de pagos en `backend/tests/GestorGastos.Domain.Tests/Payments/PaymentServiceTests.cs`: no vencido → `PaidOn` = hoy sin fecha (AC-35), incluso para un gasto de un mes futuro (RF-28); vencido con 03/10 → 03/10 (AC-36); vencido sin fecha → hoy (AC-37); 06/10 → error y sigue pendiente (AC-38); vencido con 15/08/2026 → aceptado (AC-66); desmarcar → pendiente sin fecha (AC-39); recurrente pagado en septiembre: octubre sigue pendiente $20.000 (AC-24); desmarcar un recurrente cuyo único dato era el pago → instancia eliminada (AC-68) y con cambio "Solo este mes" → instancia conservada sin pago (RF-31)
- [X] T056 [P] [US4] Tests de alerta en `backend/tests/GestorGastos.Domain.Tests/Alerts/DueTodayAlertTests.cs`: pendiente que vence hoy → cuenta 1 (AC-40); único que vence hoy está pagado → 0 (AC-42); pendiente de septiembre que vence 05/10 → 1 (AC-67); recurrente solo proyectado del mes actual que vence hoy → 1; vencidos no cuentan (RF-32)
- [X] T057 [P] [US4] Tests de integración en `backend/tests/GestorGastos.Api.Tests/Payments/PaymentEndpointsTests.cs`: AC-61 (pagar y luego "Solo este mes" → exactamente 1 instancia), AC-68 (pagar y desmarcar → 0 instancias), AC-48 (01:00 UTC del 06/10 → `VenceHoy`) y `GET /api/alerts/due-today` (AC-40, AC-41)
- [X] T058 [P] [US4] Tests de frontend en `frontend/tests/features/payments/PayButton.test.tsx` y `frontend/tests/features/month/DueTodayAlert.test.tsx`: pagar un no vencido no pide fecha y actualiza sin recargar (AC-35); pagar un vencido pregunta "¿Lo pagaste hoy?" y permite ingresar fecha (AC-36, AC-37); fecha futura rechazada (AC-38); alerta visible al abrir con sesión vigente (AC-41) y oculta si no hay pendientes que venzan hoy (AC-42)

### Implementation for User Story 4

- [X] T059 [P] [US4] Incluir `paidOn` en `MonthExpense` y verificar en `backend/src/GestorGastos.Api/Services/MonthQueryService.cs` que el estado se recalcula con el pago (`Pagado`) usando `DueStatusCalculator` de T040 (RF-33)
- [X] T060 [US4] Implementar `PaymentService` en `backend/src/GestorGastos.Domain/Payments/PaymentService.cs`: `MarkPaid(effective, today, paidOn?)` (si no está Vencido ignora la fecha y usa hoy; si está Vencido usa `paidOn ?? today` y exige `≤ today`) y `Unmark`; para recurrentes hace upsert de la instancia y normaliza con `InstanceNormalizer` (RF-27 a RF-31, RNF-06)
- [X] T061 [US4] Implementar la regla de la alerta en `backend/src/GestorGastos.Domain/Alerts/DueTodayAlert.cs` (cuenta pendientes de cualquier mes cuyo vencimiento efectivo es hoy; los vencidos y pagados no cuentan; RF-32) y la consulta en `backend/src/GestorGastos.Api/Services/DueTodayAlertService.cs` que la usa (R-14): carga puntuales e instancias con `due_date = hoy` sin pago + recurrentes activos en el período actual con su versión vigente e instancia, y delega el conteo en `DueTodayAlert`
- [X] T062 [US4] Implementar `PUT`/`DELETE /api/periods/{period}/expenses/{expenseId}/payment` en `backend/src/GestorGastos.Api/Endpoints/PaymentEndpoints.cs` y `GET /api/alerts/due-today` en `backend/src/GestorGastos.Api/Endpoints/AlertEndpoints.cs` (RF-27, RF-31, RF-32)
- [X] T063 [P] [US4] Implementar `frontend/src/features/payments/PayButton.tsx` y `frontend/src/features/payments/OverduePaymentDialog.tsx` (solo para Vencido: "¿Lo pagaste hoy?" → sí / fecha real con máximo = hoy) con actualización optimista e invalidación del mes (RF-28, RF-29, RF-30)
- [X] T064 [US4] (después de T053, que también modifica `ExpenseList.tsx`) Implementar la alerta en `frontend/src/features/month/DueTodayAlert.tsx` (consulta `/api/alerts/due-today` en cada apertura o recarga de la app) y los indicadores de estado en `frontend/src/features/month/ExpenseList.tsx` ("Al día", "Próximo a vencer", "Vence hoy", "Vencido", "Pagado" con fecha) (RF-32, RF-33)

**Checkpoint**: estados, pagos y alerta funcionando de forma independiente.

---

## Phase 7: User Story 5 - Corregir el plan: baja, eliminación y cambio de tipo (Priority: P2)

**Goal**: dar de baja recurrentes, eliminar puntuales y convertir entre tipos sin perder historial.

**Independent Test**: sobre gastos existentes, ejecutar baja, eliminación y cambios de tipo
verificando meses anteriores, actual y posteriores y los registros guardados.

### Tests for User Story 5 ⚠️ (escribir primero, deben fallar)

- [X] T065 [P] [US5] Tests de dominio en `backend/tests/GestorGastos.Domain.Tests/Expenses/ExpenseLifecycleTests.cs`: puntual de octubre → recurrente aparece en octubre y noviembre (AC-18) conservando sus valores (RF-12); recurrente desde agosto → puntual en octubre: agosto y septiembre siguen, octubre es un puntual nuevo con los valores y el pago de octubre, noviembre no aparece (AC-19, RF-13); pagado en noviembre y convertido viendo octubre → noviembre sin él (AC-20); baja en septiembre con registros enero–agosto (AC-21); pagado en septiembre y baja en septiembre (AC-22); baja o conversión en el mes de inicio → el recurrente se elimina por completo (R-03)
- [X] T066 [P] [US5] Tests de integración en `backend/tests/GestorGastos.Api.Tests/Expenses/LifecycleEndpointsTests.cs`: eliminar un puntual (AC-23) y un puntual pagado → 204 (RF-16); `DELETE` sobre un recurrente → 409; operar sobre un recurrente en un mes anterior a su inicio o posterior a su baja → 404 (RF-09); baja viendo octubre con pago en noviembre → 0 instancias en octubre y noviembre (AC-65); totales de noviembre sin su monto tras convertir (AC-20)
- [ ] T067 [P] [US5] Tests de frontend en `frontend/tests/features/expenses/ExpenseActions.test.tsx`: un puntual ofrece "Eliminar" y "Hacer recurrente"; un recurrente ofrece "Dar de baja desde este mes" y "Hacer puntual", con confirmación (RF-12, RF-13, RF-14, RF-16)

### Implementation for User Story 5

- [X] T068 [US5] Implementar `ExpenseLifecycle` en `backend/src/GestorGastos.Domain/Expenses/ExpenseLifecycle.cs`: `Discontinue(recurring, P)` (`EndPeriod = P − 1`, borra instancias `≥ P`, elimina el gasto si `EndPeriod < StartPeriod`); `ToOneOff(recurring, P)` (igual + crea `OneOffExpense` en P con valores efectivos y `PaidOn` de P); `ToRecurring(oneOff, P)` (borra el puntual, crea recurrente desde P con sus valores e instancia si estaba pagado o vence fuera de P) (RF-12 a RF-15)
- [X] T069 [US5] Implementar `DELETE /api/periods/{period}/expenses/{expenseId}` (solo puntuales, eliminación definitiva; 409 para recurrentes), `POST …/discontinue` y `POST …/convert` en `backend/src/GestorGastos.Api/Endpoints/ExpenseEndpoints.cs` usando `ExpenseCommandService` (RF-12, RF-13, RF-14, RF-16)
- [ ] T070 [P] [US5] Implementar `frontend/src/features/expenses/ExpenseActions.tsx` (menú por gasto con eliminar, dar de baja y cambiar tipo, con diálogo de confirmación) e invalidar la query del mes tras cada acción (RF-12, RF-13, RF-14, RF-16)

**Checkpoint**: el plan de gastos se puede corregir sin romper el historial.

---

## Phase 8: User Story 6 - Categorías y resumen del mes (Priority: P3)

**Goal**: ABM de categorías y resumen por categoría con total del mes y total pendiente.

**Independent Test**: crear categorías, asociarlas a gastos de un mes y verificar resumen y totales.

### Tests for User Story 6 ⚠️ (escribir primero, deben fallar)

- [ ] T071 [P] [US6] Tests de dominio en `backend/tests/GestorGastos.Domain.Tests/Categories/CategoryNameTests.cs`: normalización (minúsculas, sin acentos, espacios colapsados, recorte); "Educación"/"educacion"/"EDUCACIÓN" colisionan (AC-26); "Sin categoría"/"sin categoria" reservados (AC-27); "Gastos  hogar" (doble espacio) colisiona con "Gastos hogar" (RF-19); nombre obligatorio "hasta 50 caracteres"
- [ ] T072 [P] [US6] Tests del resumen en `backend/tests/GestorGastos.Domain.Tests/Summary/MonthSummaryTests.cs`: Vivienda $200.000 pagado + $100.000 pendiente → $300.000, Servicios $100.000 (AC-32); $50.000 sin categoría → "Sin categoría" (AC-33); pagados $300.000 + pendientes $100.000 → total $400.000 y pendiente $100.000 (AC-34)
- [ ] T073 [P] [US6] Tests de integración en `backend/tests/GestorGastos.Api.Tests/Categories/CategoryEndpointsTests.cs`: crear (AC-25); duplicados y reservados → 409 (AC-26, AC-27); renombrar se ve en agosto y octubre (AC-28); eliminar sin gastos (AC-29); eliminar "Ocio" con gastos en agosto y octubre → "Sin categoría" en ambos, incluidas versiones e instancias (AC-30); asociar categoría a un gasto sin categoría (AC-31)
- [ ] T074 [P] [US6] Tests de frontend en `frontend/tests/features/categories/CategoriesPage.test.tsx` y `frontend/tests/features/month/CategorySummary.test.tsx`: alta, renombrado y eliminación con errores 409 visibles; resumen y totales del mes renderizados con formato es-AR (RF-18 a RF-21, RF-23 a RF-26)

### Implementation for User Story 6

- [ ] T075 [P] [US6] Implementar `CategoryName` (normalización y reserva de "sin categoria") en `backend/src/GestorGastos.Domain/Categories/CategoryName.cs` (RF-18, RF-19)
- [ ] T076 [P] [US6] Implementar `MonthSummary` en `backend/src/GestorGastos.Domain/Summary/MonthSummary.cs` (totales por categoría sumando pagados y pendientes, grupo "Sin categoría", total del mes y total pendiente) y completar `byCategory`, `total` y `pendingTotal` en `MonthQueryService` (RF-23 a RF-26)
- [ ] T077 [US6] Implementar `GET/POST /api/categories` y `PUT/DELETE /api/categories/{categoryId}` en `backend/src/GestorGastos.Api/Endpoints/CategoryEndpoints.cs` (409 ante índice único violado o nombre reservado; el borrado confía en `ON DELETE SET NULL`) (RF-18 a RF-21)
- [ ] T078 [P] [US6] Implementar `frontend/src/features/categories/CategoriesPage.tsx` (listar, crear, renombrar, eliminar) y el selector de categoría en `frontend/src/features/expenses/ExpenseForm.tsx` (RF-18 a RF-22)
- [ ] T079 [P] [US6] Implementar `frontend/src/features/month/Totals.tsx` (total del mes y total pendiente) y `frontend/src/features/month/CategorySummary.tsx` dentro de `MonthPage.tsx` (RF-23 a RF-26)

**Checkpoint**: categorías y resumen funcionando de forma independiente.

---

## Phase 9: User Story 7 - Exportar el historial a CSV (Priority: P4)

**Goal**: descargar el historial completo hasta el mes actual en CSV.

**Independent Test**: con puntuales y recurrentes en varios meses, exportar y validar contenido y
formato según [contracts/csv-export.md](contracts/csv-export.md).

### Tests for User Story 7 ⚠️ (escribir primero, deben fallar)

- [ ] T080 [P] [US7] Tests de dominio en `backend/tests/GestorGastos.Domain.Tests/Export/CsvExportBuilderTests.cs`: hoy 05/10/2026 con un puntual en agosto y un recurrente desde agosto sin instancias → 4 filas (`2026-08` ×2, `2026-09`, `2026-10`) y ninguna de `2026-11` (AC-58); fila exacta `2026-10,"Luz, gas",1234.56,Sin categoría,2026-10-10,Pendiente,` (AC-59); sin gastos → solo encabezado; orden por período, luego vencimiento (vacíos al final), luego nombre (RF-39); comillas internas duplicadas
- [ ] T081 [P] [US7] Test de integración en `backend/tests/GestorGastos.Api.Tests/Export/ExportEndpointTests.cs`: `GET /api/export.csv` devuelve `text/csv; charset=utf-8`, empieza con los bytes `EF BB BF`, usa `\r\n` y tiene `Content-Disposition` de descarga (AC-59)

### Implementation for User Story 7

- [ ] T082 [US7] Implementar `CsvExportBuilder` en `backend/src/GestorGastos.Domain/Export/CsvExportBuilder.cs`: recorre desde el primer período con gastos hasta `CurrentPeriod` usando `MonthProjector`; escapa según RFC 4180; formatos de [contracts/csv-export.md](contracts/csv-export.md) (RF-38, RF-39, RNF-07)
- [ ] T083 [US7] Implementar `GET /api/export.csv` en `backend/src/GestorGastos.Api/Endpoints/ExportEndpoints.cs` (carga todos los gastos con versiones e instancias en memoria, escribe UTF-8 con BOM) (RF-38, RF-39)
- [ ] T084 [P] [US7] Implementar `frontend/src/features/export/ExportButton.tsx` (descarga de `/api/export.csv`) en la vista del mes, con test en `frontend/tests/features/export/ExportButton.test.tsx` (RF-38)

**Checkpoint**: todas las historias funcionan de forma independiente.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: requisitos no funcionales y cierre.

- [ ] T085 Test de rendimiento en `backend/tests/GestorGastos.Api.Tests/Performance/PerformanceTests.cs`: sembrar 10 años × 100 gastos por mes (mezcla de puntuales, recurrentes, versiones e instancias) y verificar p95 < 2 s medido en el servidor para cada operación de RNF-01 (login, alerta, consultar mes, crear/editar/baja/eliminar, cambio de tipo, marcar/desmarcar, categorías, exportar) (SC-003). Marcarlo con `[Trait("Category", "Performance")]` y declararlo explícito (`[Fact(Explicit = true)]` de xUnit v3) para que un `dotnet test` sin filtro no lo ejecute, y correrlo explícitamente antes de cada merge (`dotnet test -- --explicit only`)
- [ ] T086 [P] Configurar la PWA en `frontend/vite.config.ts` con `vite-plugin-pwa`: manifest (nombre, íconos en `frontend/public/`, `display: standalone`, colores) y service worker **sin** cacheo de `/api` (RNF-03; offline fuera de alcance)
- [ ] T087 [P] Revisar todas las vistas a 360 px de ancho sin scroll horizontal ni superposición en `frontend/src/styles.css` y componentes (RNF-02, SC-004)
- [ ] T089 Verificar trazabilidad: script `backend/tests/check-ac-coverage.sh` que lista los AC-01 a AC-68 sin al menos un test que los referencie (traits del backend y nombres de tests del frontend) y falla si falta alguno (SC-006, principio I)
- [ ] T090 Ejecutar `dotnet test` en `backend/` (más la corrida explícita de rendimiento, `dotnet test -- --explicit only`) y `pnpm test` en `frontend/` y dejar todo en verde (quality gate de la constitución) (quality gate, sin RF)
- [ ] T091 Ejecutar la validación manual de [quickstart.md](quickstart.md) completa (pasos 1 a 11 y verificación con curl), verificar cada vista (login, mes con gastos, categorías y los diálogos de edición y pago) a 360 px en DevTools sin scroll horizontal ni superposición (RNF-02, SC-004), verificar que la app se instala en Chrome y Edge de escritorio y en Chrome Android (o su emulación) y que se usa completa desde el navegador en Firefox y Safari de macOS (RNF-03), y corregir lo que falle (quality gate)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup; **bloquea** todas las historias.
- **Historias (Phase 3 a 9)**: dependen de Foundational.
- **Polish (Phase 10)**: depende de las historias que se quieran entregar.

### User Story Dependencies

- **US1 (P1)**: solo Foundational.
- **US2 (P1)**: solo Foundational (los tests de API usan el cliente autenticado del harness, no la UI de login).
- **US3 (P1)**: depende de US2 (alta de recurrentes y `MonthProjector`).
- **US4 (P2)**: depende de US2 (consulta del mes); usa `InstanceNormalizer` de US3 para recurrentes (T049).
- **US5 (P2)**: depende de US2; la conversión a puntual usa valores efectivos de US3.
- **US6 (P3)**: depende de US2 (consulta del mes); la tabla `category` ya existe desde Foundational.
- **US7 (P4)**: depende de US2 y usa `MonthProjector`; los nombres de categoría se muestran como "Sin categoría" hasta tener US6.

### Within Each User Story

- Tests primero y en rojo (principio I) → dominio → servicios → endpoints → frontend.
- Commit al cerrar cada tarea o grupo lógico (Conventional Commits).

### Parallel Opportunities

- Setup: T002, T005, T006, T007, T008 en paralelo tras T001/T003.
- Foundational: los tests T009 a T012 en paralelo; T013, T014, T015 en paralelo; frontend T025 a T027 en paralelo con el backend.
- Dentro de cada historia: todos los tests marcados [P] en paralelo; el frontend [P] en paralelo con el backend una vez definido el contrato.
- Entre historias: tras US2, US4, US5, US6 y US7 pueden avanzar en paralelo (US3 conviene antes que US4 y US5 por `InstanceNormalizer` y los valores efectivos).

---

## Parallel Example: User Story 3

```bash
# Tests primero, en paralelo:
Task: "Tests de proyección en backend/tests/GestorGastos.Domain.Tests/Expenses/RecurringProjectionTests.cs"
Task: "Tests de edición en backend/tests/GestorGastos.Domain.Tests/Expenses/RecurringEditorTests.cs"
Task: "Tests de integración en backend/tests/GestorGastos.Api.Tests/Expenses/RecurringPersistenceTests.cs"
Task: "Tests de frontend en frontend/tests/features/expenses/EditScopeDialog.test.tsx"

# Luego, el frontend en paralelo con el dominio:
Task: "Implementar EditScopeDialog en frontend/src/features/expenses/EditScopeDialog.tsx"
Task: "Implementar RecurringEditor en backend/src/GestorGastos.Domain/Expenses/RecurringEditor.cs"
```

---

## Implementation Strategy

### MVP First (US1 + US2 + US3)

1. Phase 1: Setup.
2. Phase 2: Foundational (bloqueante).
3. Phase 3: US1 → validar acceso protegido.
4. Phase 4: US2 → validar alta y navegación.
5. Phase 5: US3 → validar recurrentes (el problema central del PRD).
6. **STOP y validar**: las tres historias P1 juntas forman el MVP.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 + US2 + US3 → MVP.
3. US4 (pagos y alerta) → objetivo medible del PRD (SC-001).
4. US5 (baja y cambio de tipo).
5. US6 (categorías y resumen).
6. US7 (CSV).
7. Polish: rendimiento, PWA, 360 px, trazabilidad AC → test.

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- Cada test que cubre un AC lo referencia por código; T089 verifica que estén los 68.
- Verificar que los tests fallen antes de implementar (principio I).
- No pre-generar registros de meses futuros ni modificar meses anteriores (AGENTS.md, principio IV).
- Ningún secreto en código, `appsettings*.json`, `docker-compose.yml` ni tests (principio II).
