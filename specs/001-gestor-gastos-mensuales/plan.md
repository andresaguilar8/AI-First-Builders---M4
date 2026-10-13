# Implementation Plan: Gestor de gastos mensuales

**Branch**: `001-gestor-gastos-mensuales` | **Date**: 2026-10-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-gestor-gastos-mensuales/spec.md`

## Summary

Aplicación web de un único usuario para registrar gastos mensuales puntuales y recurrentes,
marcarlos como pagados, ver estados de vencimiento, una alerta diaria, un resumen por categoría y
exportar el historial a CSV (PRD-001, RF-01 a RF-39, AC-01 a AC-68).

Enfoque técnico: API REST en ASP.NET Core (.NET 10) con EF Core sobre PostgreSQL 16, y una SPA en
React 19 + TypeScript instalable como PWA, servida desde el mismo origen. La pieza central es el
modelo de recurrentes **versiones + instancias mensuales** ([research.md](research.md) R-01): los
meses sin información propia se proyectan al consultar (RNF-05), cada mes con información propia
tiene exactamente una instancia (RNF-06) y "Este mes y los siguientes" crea versiones desde el mes
visualizado, por lo que los meses anteriores no cambian (RF-17). Las reglas del dominio viven en
un proyecto sin dependencias de infraestructura y se desarrollan test-first.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5 / React 19 (frontend)

**Primary Dependencies**: ASP.NET Core 10 (Minimal APIs), ASP.NET Core Identity (cookie),
EF Core 10 + Npgsql; Vite, React Router, TanStack Query, vite-plugin-pwa

**Storage**: PostgreSQL 16 (Docker Compose en desarrollo)

**Testing**: xUnit + FakeTimeProvider (dominio), xUnit + WebApplicationFactory + Testcontainers
PostgreSQL (API/persistencia), Vitest + Testing Library + MSW (frontend)

**Target Platform**: servidor Linux o Windows con .NET 10; navegadores Chrome/Edge (instalable),
Chrome Android (instalable), Firefox y Safari macOS (navegador) — RNF-03

**Project Type**: aplicación web (backend + frontend)

**Performance Goals**: operaciones de RNF-01 < 2 s p95 medido en el servidor, con 1 usuario,
≤ 100 gastos por mes y 10 años de historial

**Constraints**: fecha actual siempre en America/Argentina/Buenos_Aires (RNF-07); sesión de 30
días sin actividad (RNF-08); 360 px sin scroll horizontal (RNF-02); requiere conexión (sin
offline); secretos solo por variables de entorno

**Scale/Scope**: 1 usuario; ~12.000 gastos-mes en 10 años; 3 vistas (login, mes, categorías)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Cómo lo cumple el plan | Pre | Post |
|---|---|---|---|
| I. Lógica de gastos test-first | Reglas en `GestorGastos.Domain` sin infraestructura, con tests unitarios escritos primero; cada test de un AC lleva `[Trait("AC", "AC-xx")]`; integración con PostgreSQL real para conteo de registros (R-13) | ✅ | ✅ |
| II. Sin secretos en el código | Cadena de conexión y credenciales del seed por variables de entorno / user-secrets; `.env` en `.gitignore`, `.env.example` con valores ficticios (R-07, quickstart) | ✅ | ✅ |
| III. Alcance acotado al PRD | Sin registro, sin logout, sin offline (el service worker no cachea la API), sin pre-generación. Las únicas decisiones no escritas en el PRD son técnicas y quedan marcadas para confirmar: límites de texto (R-10) y orden de filas dentro de un período en el CSV | ✅ | ✅ (con 2 decisiones a confirmar en el PRD) |
| IV. Historial inmutable y proyección | Versiones con `effective_from` + instancias únicas por mes; renombrar/eliminar categoría vía FK `ON DELETE SET NULL` (excepción explícita) — [data-model.md](data-model.md) | ✅ | ✅ |
| V. Fecha en Buenos Aires | `TimeProvider` inyectable + `IBusinessCalendar`; el frontend recibe `today` del backend (R-05) | ✅ | ✅ |
| Restricciones técnicas | Stack de AGENTS.md; hasher de Identity v3 = PBKDF2 100.000 iteraciones; cookie renovada en cada request para expirar a los 30 días exactos de inactividad (R-06) | ✅ | ✅ |
| Quality gates | `dotnet test` y `pnpm test` antes de mergear; commits con Conventional Commits | ✅ | ✅ |

Resultado: **PASA**. No hay violaciones que justificar en Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-gestor-gastos-mensuales/
├── plan.md              # Este archivo
├── research.md          # Fase 0: decisiones técnicas
├── data-model.md        # Fase 1: entidades, reglas y transiciones
├── quickstart.md        # Fase 1: cómo levantar y validar
├── contracts/
│   ├── api.yaml         # Fase 1: contrato REST (OpenAPI 3.1)
│   └── csv-export.md    # Fase 1: contrato del CSV
├── checklists/          # requirements.md, dominio.md
└── tasks.md             # Fase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
docker-compose.yml               # PostgreSQL 16 (lee .env)
.env.example

backend/
├── GestorGastos.slnx
├── src/
│   ├── GestorGastos.Domain/     # sin dependencias de infraestructura
│   │   ├── Periods/             # Period, IBusinessCalendar
│   │   ├── Expenses/            # OneOffExpense, RecurringExpense, versiones, instancias,
│   │   │                        # proyección, edición con alcance, conversión, baja
│   │   ├── Payments/            # marcar/desmarcar, validación de fecha
│   │   ├── DueStatus/           # cálculo de estados (RF-33)
│   │   ├── Categories/          # normalización y unicidad
│   │   └── Export/              # armado de filas del CSV
│   └── GestorGastos.Api/
│       ├── Endpoints/           # auth, periods, expenses, payments, categories, alerts, export
│       ├── Auth/                # Identity, cookie, seed del usuario
│       ├── Data/                # DbContext, configuraciones EF, Migrations/
│       ├── Validation/          # parseo de montos/fechas, ProblemDetails
│       └── wwwroot/             # build del frontend (producción)
└── tests/
    ├── GestorGastos.Domain.Tests/   # unit, test-first, [Trait("AC", ...)]
    └── GestorGastos.Api.Tests/      # integración con Testcontainers PostgreSQL

frontend/
├── package.json · vite.config.ts · tsconfig.json
├── public/                      # íconos y manifest de la PWA
├── src/
│   ├── api/                     # cliente tipado según contracts/api.yaml
│   ├── features/
│   │   ├── auth/                # login
│   │   ├── month/               # vista del mes, navegación, totales, resumen, alerta
│   │   ├── expenses/            # formulario, alcance de edición, cambio de tipo, baja
│   │   ├── payments/            # marcar/desmarcar, diálogo de pago vencido
│   │   ├── categories/          # ABM de categorías
│   │   └── export/              # descarga del CSV
│   └── lib/                     # parseo/formato de montos es-AR, fechas
└── tests/                       # Vitest + Testing Library + MSW
```

**Structure Decision**: aplicación web con `backend/` y `frontend/` como indica AGENTS.md. El
backend se separa en `Domain` (reglas puras, test-first, principio I) y `Api` (HTTP, Identity,
EF Core); no se agrega una capa de infraestructura aparte porque con un solo consumidor no aporta.
El backend sirve el build del frontend en producción (mismo origen, R-12).

**Ajuste necesario en AGENTS.md**: con una solución de varios proyectos, `dotnet run` dentro de
`backend/` no encuentra un proyecto ejecutable. El comando correcto es
`dotnet run --project src/GestorGastos.Api` (`dotnet restore` y `dotnet test` siguen funcionando
desde `backend/` gracias a la solución).

## Complexity Tracking

No aplica: el Constitution Check no tiene violaciones.
