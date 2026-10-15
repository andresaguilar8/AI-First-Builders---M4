# Data Model: Gestor de gastos mensuales

**Feature**: [spec.md](spec.md) · **Decisiones**: [research.md](research.md) (R-01 a R-04, R-08 a R-10)

Convenciones: PostgreSQL 16, nombres en `snake_case`. **Período** = primer día del mes (`date`
con `CHECK (extract(day from x) = 1)`); en el dominio es un value object `Period(Year, Month)`.
Montos `numeric(14,2)`.

## Diagrama

```text
app_user (Identity)

category ─────────────┐ (ON DELETE SET NULL)
   │                  │
   ▼                  ▼
expenses (TPH, kind = 'one_off' | 'recurring')
   ├─ one_off:   period, name, description, amount, category_id, due_date, paid_on
   └─ recurring: start_period, end_period
                    │ 1..N                      │ 0..N
                    ▼                           ▼
            recurring_version            recurring_instance
            (valores vigentes            (snapshot de un mes con
             desde effective_from)        información propia)
```

## Entidades

### AppUser (ASP.NET Core Identity)

| Campo | Tipo | Reglas |
|---|---|---|
| Id, Email, NormalizedEmail, PasswordHash, SecurityStamp, … | tablas `AspNet*` de Identity | `PasswordHash` con hasher v3 (PBKDF2-HMAC-SHA512, 100.000 iteraciones) — RNF-04 |

- Exactamente 1 fila, creada por el seed (R-07). Sin endpoint de registro (RF-37).

### Category — tabla `category`

| Campo | Tipo | Reglas |
|---|---|---|
| id | uuid PK | |
| name | varchar(50) | obligatorio, recortado (R-10) |
| normalized_name | varchar(50) | `UNIQUE`; minúsculas, sin acentos, espacios colapsados; ≠ `sin categoria` (RF-19) |

- Renombrar actualiza `name` y `normalized_name`; se refleja en todos los meses por FK (RF-20).
- Eliminar: `ON DELETE SET NULL` en todas las FK → "Sin categoría" en todos los meses (RF-21).
- "Sin categoría" no es una fila: es la presentación de `category_id IS NULL` (RF-24).

### Expense (abstracta, TPH) — tabla `expenses`

| Campo | Tipo | Reglas |
|---|---|---|
| id | uuid PK | |
| kind | varchar | `one_off` \| `recurring` (discriminador) |
| created_at | timestamptz | auditoría |

#### OneOffExpense (gasto puntual)

| Campo | Tipo | Reglas |
|---|---|---|
| period | date | mes al que pertenece (RF-02, RF-08) |
| name | varchar(100) | obligatorio (RF-01) |
| description | varchar(500) null | |
| amount | numeric(14,2) | `> 0` y `≤ 999999999999.99` (RF-03) |
| category_id | uuid null FK | `ON DELETE SET NULL` |
| due_date | date null | `due_date ≥ period` (RF-05, al crear y al editar) |
| paid_on | date null | null = pendiente; `≤ hoy` al registrarse (RF-30) |

Índice: `(period)` filtrado a `kind = 'one_off'`.

#### RecurringExpense (cabecera de gasto recurrente)

| Campo | Tipo | Reglas |
|---|---|---|
| start_period | date | primer mes en que aparece |
| end_period | date null | último mes visible; null = activo. Baja o paso a puntual en P → `P − 1` (RF-13, RF-14) |

Invariante: `end_period IS NULL OR end_period ≥ start_period`; si una baja dejaría
`end_period < start_period`, el gasto se elimina completo (R-03).

Índice: `(start_period, end_period)` filtrado a `kind = 'recurring'`.

### RecurringVersion — tabla `recurring_version`

Valores vigentes de un recurrente desde un mes en adelante.

| Campo | Tipo | Reglas |
|---|---|---|
| id | uuid PK | |
| recurring_expense_id | uuid FK → expenses (cascade) | |
| effective_from | date | `UNIQUE (recurring_expense_id, effective_from)`; `≥ start_period` |
| name, description, amount, category_id | igual que OneOffExpense | |
| due_day | smallint null | 1–31; null = sin vencimiento (RF-07) |

- **Versión vigente en M** = la de mayor `effective_from ≤ M`.
- Siempre existe una versión con `effective_from = start_period`.
- Versiones consecutivas idénticas se fusionan (R-04).

### RecurringInstance (registro mensual) — tabla `recurring_instance`

Snapshot completo de un mes con información propia.

| Campo | Tipo | Reglas |
|---|---|---|
| id | uuid PK | |
| recurring_expense_id | uuid FK → expenses (cascade) | |
| period | date | `UNIQUE (recurring_expense_id, period)` → como máximo 1 por mes (RNF-06) |
| name, description, amount, category_id | igual que OneOffExpense | valores efectivos de ese mes |
| due_date | date null | fecha exacta; `≥ period` (RF-05) |
| paid_on | date null | |

**Regla de existencia** (RNF-05/06, R-01): una instancia existe si y solo si
`paid_on IS NOT NULL` **o** algún campo difiere de la proyección de la versión vigente (con
`due_date` proyectado = `min(due_day, último día del mes)`). Toda operación que la deje igual a la
proyección y sin pago la elimina.

## Valores efectivos de un gasto en el mes M (lectura)

| Caso | Fuente |
|---|---|
| Puntual con `period = M` | la fila del puntual |
| Recurrente con `start_period ≤ M ≤ coalesce(end_period, ∞)` y con instancia en M | la instancia |
| Ídem, sin instancia | proyección de la versión vigente en M; pendiente |
| Otro caso | no aparece en M |

## Estado de vencimiento (calculado, no persistido) — RF-33

Para un gasto **pendiente**, con `d = due_date − hoy` (días calendario, hoy en Buenos Aires):

| Condición | Estado |
|---|---|
| sin vencimiento o `d > 3` | `AlDia` |
| `1 ≤ d ≤ 3` | `ProximoAVencer` |
| `d = 0` | `VenceHoy` |
| `d < 0` | `Vencido` |

Un gasto pagado se muestra como `Pagado` con su `paid_on`.

## Transiciones (operaciones sobre el mes visualizado P)

| Operación | Efecto |
|---|---|
| Crear puntual | inserta `OneOffExpense(period = P)` |
| Crear recurrente | inserta cabecera (`start_period = P`) + versión (`effective_from = P`, `due_day` = día del vencimiento); si el vencimiento cae fuera de P, crea la instancia de P con esa fecha |
| Editar puntual | actualiza la fila; conserva `paid_on` (RF-09) |
| Editar recurrente "Solo este mes" | upsert de la instancia de P con los valores nuevos; conserva `paid_on`; normaliza |
| Editar recurrente "Este mes y los siguientes" | diff de campos → versión de P (crea o actualiza), versiones posteriores e instancias `≥ P`; conserva pagos; normaliza y fusiona (R-04) |
| Marcar pagado | puntual: `paid_on`; recurrente: upsert de instancia con `paid_on` (RF-27 a RF-30) |
| Desmarcar | `paid_on = null`; recurrente: normaliza (puede borrar la instancia, RF-31) |
| Baja de recurrente | `end_period = P − 1`; borra instancias `≥ P`; si queda vacío, borra el gasto (RF-14, RF-15) |
| Recurrente → puntual | como baja + inserta `OneOffExpense(period = P)` con los valores efectivos y el pago de P (RF-13) |
| Puntual → recurrente | borra el puntual + crea recurrente desde P con sus valores; instancia de P si estaba pagado o vence fuera de P (RF-12) |
| Eliminar puntual | `DELETE` definitivo (RF-16) |
| Eliminar recurrente | no permitido (se usa la baja) |
