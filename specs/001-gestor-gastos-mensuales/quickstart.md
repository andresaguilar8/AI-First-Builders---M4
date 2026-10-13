# Quickstart: validar el gestor de gastos

**Feature**: [spec.md](spec.md) · **Contratos**: [contracts/api.yaml](contracts/api.yaml),
[contracts/csv-export.md](contracts/csv-export.md) · **Modelo**: [data-model.md](data-model.md)

## Prerrequisitos

- .NET SDK 10, Node.js 22 LTS y pnpm, Docker Desktop.
- Archivo `.env` en la raíz, creado a partir de `.env.example` (nunca se commitea, principio II):

```text
POSTGRES_USER=gastos
POSTGRES_PASSWORD=<elegir>
POSTGRES_DB=gastos
ConnectionStrings__Default=Host=localhost;Port=5432;Database=gastos;Username=gastos;Password=<igual>
SEED_USER_EMAIL=<email del usuario>
SEED_USER_PASSWORD=<contraseña larga y única>
```

## Levantar

```bash
docker compose up -d                                 # PostgreSQL 16

cd backend
dotnet restore
dotnet run --project src/GestorGastos.Api            # aplica migraciones y crea el usuario (seed)

cd ../frontend
pnpm install
pnpm dev                                             # http://localhost:5173 (proxy /api → backend)
```

## Tests automatizados

```bash
cd backend && dotnet test                            # dominio + integración (Testcontainers, requiere Docker)
cd backend && dotnet test --filter "AC=AC-17"        # tests de un criterio de aceptación
cd frontend && pnpm test
```

Resultado esperado: todos en verde; cada AC-01 a AC-68 tiene al menos un test que lo referencia
(SC-006, principio I).

## Validación manual end-to-end

Con la fecha del sistema en octubre de 2026 (o la fecha real; ajustar los meses):

| # | Paso | Resultado esperado | Cubre |
|---|---|---|---|
| 1 | Abrir la app sin sesión | Pantalla de login, sin datos | AC-54 |
| 2 | Login con contraseña incorrecta | Rechazo, sin datos | AC-53 |
| 3 | Login correcto | Vista del mes actual | AC-52, AC-49 |
| 4 | Crear recurrente "Alquiler" $300.000 con vencimiento el día 10 | Aparece como recurrente | AC-01 |
| 5 | Ir al mes siguiente | "Alquiler" aparece con vencimiento el 10 | AC-09 |
| 6 | En el mes siguiente, cambiar a $350.000 con "Este mes y los siguientes"; volver al mes anterior | El mes anterior sigue en $300.000 | AC-16 |
| 7 | Marcar pagado un gasto que vence hoy | Pagado con fecha de hoy, sin recargar | AC-35 |
| 8 | Recargar la app con un pendiente que vence hoy | Alerta visible | AC-40, AC-41 |
| 9 | Crear "Luz, gas" $1.234,56 y exportar CSV | Archivo con BOM y la fila del contrato | AC-59 |
| 10 | Achicar la ventana a 360 px | Todo usable sin scroll horizontal | RNF-02 |
| 11 | En Chrome, instalar la app desde la barra de direcciones | Se instala como PWA | RNF-03 |

## Verificar sesión sin acceso a datos por API

```bash
curl -i http://localhost:5000/api/periods/2026-10    # sin cookie
```

Esperado: `HTTP/1.1 401` con cuerpo vacío (AC-55). No existe `/api/auth/register` (AC-57).
